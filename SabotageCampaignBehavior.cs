using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using System.Collections.Generic;

namespace CompanionSabotageSystem
{
    public class SabotageCampaignBehavior : CampaignBehaviorBase
    {
        private SpyManager _spyManager = new SpyManager();

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_spyManager", ref _spyManager);
            if (dataStore.IsLoading && _spyManager == null)
            {
                _spyManager = new SpyManager();
            }
        }

        private void OnDailyTick()
        {
            _spyManager.DailyTick();
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            bool CanSendSpy(MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;

                // Vérifications de base
                if (Settlement.CurrentSettlement == null || Settlement.CurrentSettlement.IsVillage) return false;

                // Interdiction : Chez nous, ou si siège en cours
                if (Settlement.CurrentSettlement.OwnerClan == Clan.PlayerClan || Settlement.CurrentSettlement.IsUnderSiege) return false;

                return true;
            }

            void OpenSpyMenu(MenuCallbackArgs args)
            {
                OpenSpySelectionList();
            }
            // ------------------------------------------

            // 1. Menu pour les VILLES ("town")
            starter.AddGameMenuOption("town", "sabotage_mission_town", "{=css_menu_option}Send an Agent",
                CanSendSpy, OpenSpyMenu, false, 2);

            // 2. Menu pour les CHÂTEAUX ("castle")
            starter.AddGameMenuOption("castle", "sabotage_mission_castle", "{=css_menu_option}Send an Agent",
                CanSendSpy, OpenSpyMenu, false, 2);
        }

        private void OpenSpySelectionList()
        {
            List<InquiryElement> spies = new List<InquiryElement>();

            foreach (var troop in MobileParty.MainParty.MemberRoster.GetTroopRoster())
            {
                if (troop.Character.IsHero && !troop.Character.IsPlayerCharacter)
                {
                    Hero h = troop.Character.HeroObject;
                    if (_spyManager.IsHeroBusy(h)) continue;

                    if (h.GetSkillValue(DefaultSkills.Roguery) >= 30 && h.HitPoints > 40)
                    {
                        TextObject info = new TextObject("{=css_agent_info}Roguery: {SKILL} | HP: {HP}%");
                        info.SetTextVariable("SKILL", h.GetSkillValue(DefaultSkills.Roguery));
                        info.SetTextVariable("HP", h.HitPoints);

                        spies.Add(new InquiryElement(h, $"{h.Name} ({info})", new CharacterImageIdentifier(CharacterCode.CreateFrom(h.CharacterObject))));
                    }
                }
            }

            if (spies.Count == 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=css_no_agents}No agents available (Roguery 30+ required).").ToString(), Colors.Red));
                return;
            }

            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                new TextObject("{=css_menu_title}Infiltration Mission").ToString(),
                new TextObject("{=css_menu_desc}Select an agent for the operation.").ToString(),
                spies,
                true,
                1,
                1,
                new TextObject("{=css_deploy_btn}Deploy").ToString(),
                new TextObject("{=css_cancel_btn}Cancel").ToString(),
                list => _spyManager.DeploySpy((Hero)list[0].Identifier, Settlement.CurrentSettlement),
                list => { },
                "",
                false
            ));
        }
    }
}