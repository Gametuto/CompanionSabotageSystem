using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;
using TaleWorlds.Localization;

namespace CompanionSabotageSystem
{
    public class SpyManager
    {
        [SaveableField(1)]
        private readonly Dictionary<Hero, SpyData> _activeSpies = new Dictionary<Hero, SpyData>();

        public void DeploySpy(Hero spy, Settlement target)
        {
            float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty, target, false, MobileParty.NavigationType.Default, out _);

            int divisor = SabotageSettings.Instance != null ? SabotageSettings.Instance.TravelSpeedDivisor : 50;
            int travelDays = (distance < 1f) ? 0 : (int)Math.Ceiling(distance / (float)divisor);

            MobileParty.MainParty.MemberRoster.AddToCounts(spy.CharacterObject, -1);
            spy.ChangeState(Hero.CharacterStates.Disabled);

            if (!_activeSpies.ContainsKey(spy))
            {
                SpyState initialState = (travelDays == 0) ? SpyState.Infiltrating : SpyState.TravelingToTarget;
                int initialDuration = (travelDays == 0) ? 5 : travelDays;

                _activeSpies.Add(spy, new SpyData(spy, target, initialDuration, initialState));

                if (travelDays > 0)
                {
                    TextObject msg = new TextObject("{=css_depart_travel}{AGENT} departs for {TARGET} ({DAYS} days).");
                    msg.SetTextVariable("AGENT", spy.Name);
                    msg.SetTextVariable("TARGET", target.Name);
                    msg.SetTextVariable("DAYS", travelDays);
                    InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Gray));
                }
                else
                {
                    TextObject msg = new TextObject("{=css_depart_instant}{AGENT} slips into {TARGET}...");
                    msg.SetTextVariable("AGENT", spy.Name);
                    msg.SetTextVariable("TARGET", target.Name);
                    InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Gray));
                }
            }
        }

        public void DailyTick()
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
                if (!_activeSpies.ContainsKey(spy)) continue;

                var data = _activeSpies[spy];

                // 1. SÉCURITÉ PRISONNIER : S'il est prisonnier, on arrête de le gérer immédiatement.
                if (spy.IsPrisoner)
                {
                    toRemove.Add(spy);
                    continue;
                }

                // 2. Watchdog : Force l'état Disabled si le jeu vanilla l'a réactivé par erreur
                if (data.State != SpyState.ReturningToPlayer && spy.HeroState != Hero.CharacterStates.Disabled)
                {
                    spy.ChangeState(Hero.CharacterStates.Disabled);
                }

                // 3. SÉCURITÉ SIÈGE & FACTION : On annule si la situation change
                if (data.State != SpyState.ReturningToPlayer)
                {
                    if (data.TargetSettlement.IsUnderSiege || data.TargetSettlement.MapFaction == MobileParty.MainParty.MapFaction)
                    {
                        TextObject msg = new TextObject("{=css_mission_abort}Mission aborted: {TARGET} is unstable. {AGENT} is returning.");
                        msg.SetTextVariable("TARGET", data.TargetSettlement.Name);
                        msg.SetTextVariable("AGENT", spy.Name);
                        InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Red));

                        StartReturnJourney(spy, data);
                        continue;
                    }
                }

                data.DaysRemaining--;

                switch (data.State)
                {
                    case SpyState.TravelingToTarget:
                        if (data.DaysRemaining <= 0)
                        {
                            data.State = SpyState.Infiltrating;
                            data.DaysRemaining = 5;

                            TextObject msg = new TextObject("{=css_arrived}{AGENT} arrived at {TARGET}. Sabotage begins.");
                            msg.SetTextVariable("AGENT", spy.Name);
                            msg.SetTextVariable("TARGET", data.TargetSettlement.Name);
                            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Yellow));
                        }
                        break;

                    case SpyState.Infiltrating:
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
                        float distToPlayer = Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty, data.TargetSettlement, false, MobileParty.NavigationType.Default, out _);
                        if (data.DaysRemaining <= 0 || distToPlayer < 5f)
                        {
                            ReturnSpyFinal(spy, data);
                            toRemove.Add(spy);
                        }
                        break;
                }
            }

            foreach (var h in toRemove)
            {
                if (_activeSpies.ContainsKey(h)) _activeSpies.Remove(h);
            }
        }

        private bool CheckForCapture(Hero spy, Settlement target)
        {
            float security = target.Town.Security;
            float skill = spy.GetSkillValue(DefaultSkills.Roguery);
            float multiplier = SabotageSettings.Instance != null ? SabotageSettings.Instance.CaptureChanceMultiplier : 1.0f;

            float riskFactor = ((security * 1.2f) - skill) * multiplier;
            if (riskFactor < 2) riskFactor = 2;

            if (MBRandom.RandomFloat * 100 < riskFactor)
            {
                if (spy.HeroState == Hero.CharacterStates.Disabled)
                    spy.ChangeState(Hero.CharacterStates.Active);

                PartyBase jailer = target.Party ?? target.Town?.GarrisonParty?.Party;

                if (jailer != null)
                {
                    // Action Vanilla : Elle gère la capture ET l'affichage du message.
                    TakePrisonerAction.Apply(jailer, spy);
                    return true;
                }
                else
                {
                    InformationManager.DisplayMessage(new InformationMessage("Capture failed (No Garrison), agent returning wounded.", Colors.Red));
                    return false;
                }
            }
            return false;
        }

        private void PerformSabotage(SpyData data)
        {
            Settlement target = data.TargetSettlement;
            float skillFactor = data.Agent.GetSkillValue(DefaultSkills.Roguery) / 100f;
            int baseFoodDamage = SabotageSettings.Instance != null ? SabotageSettings.Instance.FoodSabotageBase : 20;

            if (target.Town.FoodStocks > 0)
            {
                float damage = (baseFoodDamage + (target.Town.FoodStocks * 0.05f)) * (1f + skillFactor);
                target.Town.FoodStocks = Math.Max(0, target.Town.FoodStocks - damage);
                data.TotalFoodDestroyed += (int)damage;
            }

            target.Town.Loyalty -= 1f + skillFactor;
            target.Town.Security -= 1f + skillFactor;
            data.TotalLoyaltyLost += 1f + skillFactor;
        }

        private void StartReturnJourney(Hero spy, SpyData data)
        {
            float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty, data.TargetSettlement, false, MobileParty.NavigationType.Default, out _);

            if (distance < 1.0f || MobileParty.MainParty.CurrentSettlement == data.TargetSettlement)
            {
                ReturnSpyFinal(spy, data);
                if (_activeSpies.ContainsKey(spy)) _activeSpies.Remove(spy);
                return;
            }

            int divisor = SabotageSettings.Instance != null ? SabotageSettings.Instance.TravelSpeedDivisor : 50;
            int returnDays = (int)Math.Ceiling(distance / (float)divisor);
            if (returnDays < 1) returnDays = 1;

            data.State = SpyState.ReturningToPlayer;
            data.DaysRemaining = returnDays;

            if (spy.HeroState != Hero.CharacterStates.Disabled)
                spy.ChangeState(Hero.CharacterStates.Disabled);

            TextObject msg = new TextObject("{=css_mission_done}{AGENT} mission done. Returning ({DAYS} days).");
            msg.SetTextVariable("AGENT", spy.Name);
            msg.SetTextVariable("DAYS", returnDays);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Green));

            int xp = SabotageSettings.Instance != null ? SabotageSettings.Instance.XpGain : 800;
            spy.AddSkillXp(DefaultSkills.Roguery, xp);
        }

        private void ReturnSpyFinal(Hero spy, SpyData data)
        {
            if (spy.HeroState != Hero.CharacterStates.Active)
                spy.ChangeState(Hero.CharacterStates.Active);

            TeleportHeroAction.ApplyImmediateTeleportToParty(spy, MobileParty.MainParty);

            if (!MobileParty.MainParty.MemberRoster.Contains(spy.CharacterObject))
            {
                MobileParty.MainParty.MemberRoster.AddToCounts(spy.CharacterObject, 1);
            }

            if (data.State != SpyState.ReturningToPlayer)
            {
                int xp = SabotageSettings.Instance != null ? SabotageSettings.Instance.XpGain : 800;
                spy.AddSkillXp(DefaultSkills.Roguery, xp);
            }

            TextObject msg = new TextObject("{=css_back_in_party}{AGENT} is back in the party.");
            msg.SetTextVariable("AGENT", spy.Name);
            InformationManager.DisplayMessage(new InformationMessage(msg.ToString(), Colors.Green));
        }

        public bool IsHeroBusy(Hero hero)
        {
            return _activeSpies.ContainsKey(hero);
        }
    }
}