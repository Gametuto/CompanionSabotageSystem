using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions; // Important pour DestroyPartyAction
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;

namespace CompanionSabotageSystem
{
    public class SabotageCampaignBehavior : CampaignBehaviorBase
    {
        // --- SINGLETON ---
        public static SabotageCampaignBehavior Instance { get; private set; }

        public SabotageCampaignBehavior()
        {
            Instance = this;
        }

        private Dictionary<Hero, SpyData> _activeSpies = new Dictionary<Hero, SpyData>();

        // --- EVENTS & SYNC ---
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_activeSpies", ref _activeSpies);
        }

        // --- MENU DU JEU ---
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town", "sabotage_mission", "{=sabotage_opt}Send an Agent (Sabotage)",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    if (Settlement.CurrentSettlement == null || Settlement.CurrentSettlement.IsVillage) return false;

                    // Pas d'espionnage chez soi ou si assiégé
                    if (Settlement.CurrentSettlement.OwnerClan == Clan.PlayerClan || Settlement.CurrentSettlement.IsUnderSiege)
                        return false;

                    return true;
                },
                args => OpenSpySelectionList(), false, 2);
        }

        private void OpenSpySelectionList()
        {
            List<InquiryElement> spies = new List<InquiryElement>();

            foreach (var troop in MobileParty.MainParty.MemberRoster.GetTroopRoster())
            {
                if (troop.Character.IsHero && !troop.Character.IsPlayerCharacter)
                {
                    Hero h = troop.Character.HeroObject;
                    // Condition : Héros vivant, actif et Roguery >= 30
                    if (h.IsAlive && h.HeroState != Hero.CharacterStates.Disabled && h.GetSkillValue(DefaultSkills.Roguery) >= 30)
                    {
                        string info = $"Roguery: {h.GetSkillValue(DefaultSkills.Roguery)} | HP: {h.HitPoints}%";
                        spies.Add(new InquiryElement(h, $"{h.Name} ({info})", new CharacterImageIdentifier(CharacterCode.CreateFrom(h.CharacterObject))));
                    }
                }
            }

            if (spies.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("No capable agents available (Requires Roguery 30+).", Colors.Red));
                return;
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                "Infiltration Mission",
                "Select an agent to send for sabotage.",
                spies,
                true, 1, 1,
                "Deploy Agent", "Cancel",
                list => DeploySpy((Hero)list[0].Identifier),
                list => { },
                "", false
            ));
        }

        // --- DÉPLOIEMENT ---
        private void DeploySpy(Hero spy)
        {
            Settlement target = Settlement.CurrentSettlement;

            // On sort du menu pour revenir à la carte, sinon le jeu n'aime pas spawner des entités
            GameMenu.ExitToLast();

            // On délègue la création physique à SpyManager
            SpyManager.CreateSpyParty(spy, target);
        }

        // --- ENREGISTREMENT (Appelé par SpyManager) ---
        public void RegisterSpyMission(Hero spy, Settlement target, MobileParty spyParty)
        {
            if (!_activeSpies.ContainsKey(spy))
            {
                // CORRECTION : GetDistance avec 5 arguments
                float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(
                    MobileParty.MainParty,
                    target,
                    false,
                    MobileParty.NavigationType.Default,
                    out _
                );

                int travelDays = (int)Math.Ceiling(distance / 50f);

                var data = new SpyData(spy, target, travelDays, SpyState.TravelingToTarget)
                {
                    PartyId = spyParty.StringId
                };

                _activeSpies.Add(spy, data);
            }
        }

        // --- BOUCLE LOGIQUE (DAILY TICK) ---
        private void OnDailyTick()
        {
            List<Hero> toRemove = new List<Hero>();
            List<Hero> activeHeroes = new List<Hero>(_activeSpies.Keys);

            foreach (var spy in activeHeroes)
            {
                if (spy == null || !spy.IsAlive)
                {
                    toRemove.Add(spy);
                    continue;
                }

                var data = _activeSpies[spy];

                switch (data.State)
                {
                    case SpyState.TravelingToTarget:
                        MobileParty physicalParty = Campaign.Current.CampaignObjectManager.Find<MobileParty>(data.PartyId);

                        // CORRECTION : Utilisation de GetPositionAsVec3() pour compatibilité
                        if (physicalParty != null && physicalParty.GetPositionAsVec3().AsVec2.DistanceSquared(data.TargetSettlement.GatePosition.ToVec2()) < 25f)
                        {
                            // ARRIVÉE
                            data.State = SpyState.Infiltrating;
                            data.DaysRemaining = 5;

                            // CORRECTION : Utilisation de DestroyPartyAction
                            DestroyPartyAction.Apply(null, physicalParty);

                            InformationManager.DisplayMessage(new InformationMessage($"{spy.Name} has infiltrated {data.TargetSettlement.Name}. Operation starting.", Colors.Yellow));
                        }
                        else if (physicalParty == null)
                        {
                            // Fallback sécurité
                            data.State = SpyState.Infiltrating;
                            data.DaysRemaining = 5;
                        }
                        break;

                    case SpyState.Infiltrating:
                        data.DaysRemaining--;

                        bool captured = CheckForCapture(spy, data.TargetSettlement);
                        if (captured)
                        {
                            toRemove.Add(spy);
                        }
                        else
                        {
                            PerformSabotage(data);

                            if (data.DaysRemaining <= 0)
                            {
                                StartReturnJourney(spy, data);
                            }
                        }
                        break;

                    case SpyState.ReturningToPlayer:
                        data.DaysRemaining--;

                        // CORRECTION : GetDistance 5 arguments pour le retour
                        float distToPlayer = Campaign.Current.Models.MapDistanceModel.GetDistance(
                            MobileParty.MainParty,
                            data.TargetSettlement,
                            false,
                            MobileParty.NavigationType.Default,
                            out _
                        );

                        if (data.DaysRemaining <= 0 || distToPlayer < 10f)
                        {
                            ReturnSpyFinal(spy, data);
                            toRemove.Add(spy);
                        }
                        break;
                }
            }

            foreach (var h in toRemove) _activeSpies.Remove(h);
        }

        // --- LOGIQUE INTERNE ---
        private bool CheckForCapture(Hero spy, Settlement target)
        {
            float security = target.Town.Security;
            float skill = spy.GetSkillValue(DefaultSkills.Roguery);
            float riskFactor = (security * 1.2f) - skill;
            if (riskFactor < 2) riskFactor = 2;

            if (MBRandom.RandomFloat * 100 < riskFactor)
            {
                if (spy.HeroState == Hero.CharacterStates.Disabled)
                    spy.ChangeState(Hero.CharacterStates.Active);

                ShowSpyResultPopup(spy, "Agent Captured!", $"{spy.Name} has been caught by the guards of {target.Name}!\nThey are now rotting in the dungeon.", "Damn it!");
                TakePrisonerAction.Apply(target.Party, spy);
                return true;
            }
            return false;
        }

        private void PerformSabotage(SpyData data)
        {
            Settlement target = data.TargetSettlement;
            float skillFactor = data.Agent.GetSkillValue(DefaultSkills.Roguery) / 100f;

            // Food Sabotage
            if (target.Town.FoodStocks > 0)
            {
                float dmg = ((target.Town.FoodStocks * 0.10f) + 5f) * (0.8f + skillFactor);
                dmg = Math.Min(dmg, 50f);
                target.Town.FoodStocks -= dmg;
                data.TotalFoodDestroyed += (int)dmg;
            }

            // Loyalty Sabotage
            float loyaltyDmg = (1.0f + ((100f - target.Town.Security) / 20f)) * (0.5f + skillFactor);
            target.Town.Loyalty -= loyaltyDmg;
            data.TotalLoyaltyLost += loyaltyDmg;

            // Security Sabotage
            target.Town.Security -= 1.0f * (0.5f + skillFactor);
        }

        private void StartReturnJourney(Hero spy, SpyData data)
        {
            // CORRECTION : GetDistance 5 arguments
            float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(
                MobileParty.MainParty,
                data.TargetSettlement,
                false,
                MobileParty.NavigationType.Default,
                out _
            );

            int returnDays = (int)Math.Ceiling(distance / 40f);
            if (returnDays < 1) returnDays = 1;

            data.State = SpyState.ReturningToPlayer;
            data.DaysRemaining = returnDays;

            InformationManager.DisplayMessage(new InformationMessage($"Mission complete. {spy.Name} is returning to base ({returnDays} days).", Colors.Green));
            spy.AddSkillXp(DefaultSkills.Roguery, 800);
        }

        private void ReturnSpyFinal(Hero spy, SpyData data)
        {
            if (spy.HeroState != Hero.CharacterStates.Active)
            {
                spy.ChangeState(Hero.CharacterStates.Active);
            }

            TeleportHeroAction.ApplyImmediateTeleportToParty(spy, MobileParty.MainParty);

            if (!MobileParty.MainParty.MemberRoster.Contains(spy.CharacterObject))
            {
                MobileParty.MainParty.MemberRoster.AddToCounts(spy.CharacterObject, 1);
            }

            string statsReport = "";
            if (data.TotalFoodDestroyed > 0) statsReport += $"- Food Supplies Destroyed: {data.TotalFoodDestroyed}\n";
            if (data.TotalLoyaltyLost > 0) statsReport += $"- Loyalty Reduced: {data.TotalLoyaltyLost:F1}\n";
            if (string.IsNullOrEmpty(statsReport)) statsReport = "No significant damage caused.";

            ShowSpyResultPopup(
                spy,
                "Mission Accomplished",
                $"{spy.Name} has returned from the shadows of {data.TargetSettlement.Name}.\n\nMission Report:\n{statsReport}\n(Roguery XP Gained)",
                "Excellent"
            );
        }

        private void ShowSpyResultPopup(Hero spy, string title, string description, string buttonText)
        {
            var spyImage = new CharacterImageIdentifier(CharacterCode.CreateFrom(spy.CharacterObject));

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                title,
                description,
                new List<InquiryElement> { new InquiryElement(spy, spy.Name.ToString(), spyImage) },
                true, 1, 1, buttonText, "",
                list => { }, list => { }, "", false
            ));
        }
    }
}
