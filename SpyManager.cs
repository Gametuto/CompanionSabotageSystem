using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace CompanionSabotageSystem
{
    public static class SpyManager
    {
        public static void CreateSpyParty(Hero spy, Settlement target)
        {
            // 1. Sortir le héro du groupe
            spy.PartyBelongedTo?.MemberRoster.AddToCounts(spy.CharacterObject, -1);

            // 2. Créer le composant
            var spyComponent = new SpyPartyComponent(spy, target);
            string partyId = $"spy_party_{spy.StringId}_{MBRandom.RandomInt(10000)}";

            // 3. Création du MobileParty (Version 2 arguments)
            MobileParty spyParty = MobileParty.CreateParty(partyId, spyComponent);

            // 4. Initialisation Manuelle (ce qu'on faisait dans le délégué avant)
            spyParty.ActualClan = Clan.PlayerClan;
            spyParty.MemberRoster.AddToCounts(spy.CharacterObject, 1);

            // Positionnement
            // On récupère la position 3D
            Vec3 position3D = MobileParty.MainParty.GetPositionAsVec3();

            // On crée le CampaignVec2 avec 'true' pour dire qu'on est sur la terre ferme
            spyParty.InitializeMobilePartyAtPosition(new CampaignVec2(position3D.AsVec2, true));

            // IA et Configuration
            spyParty.SetPartyUsedByQuest(true);
            spyParty.IgnoreByOtherPartiesTill(CampaignTime.Now + CampaignTime.Hours(2));
            spyParty.Ai.SetDoNotMakeNewDecisions(true); 
            spyParty.Party.SetVisualAsDirty();

            // 5. Ordre de Mouvement (Correction de l'argument manquant)
            spyParty.SetMoveGoToSettlement(target, MobileParty.NavigationType.Default, false);

            // 6. Enregistrer
            SabotageCampaignBehavior.Instance.RegisterSpyMission(spy, target, spyParty);

            InformationManager.DisplayMessage(new InformationMessage($"{spy.Name} is leaving for {target.Name}.", Colors.Gray));
        }
    }
}
