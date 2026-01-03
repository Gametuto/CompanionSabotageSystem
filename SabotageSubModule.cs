using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using HarmonyLib; // Ajout Harmony

namespace CompanionSabotageSystem
{
    public class SabotageSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            // Initialisation de Harmony
            new Harmony("com.gametuto.companionsabotagesystem").PatchAll();
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            if (game.GameType is Campaign)
            {
                CampaignGameStarter campaignStarter = (CampaignGameStarter)gameStarterObject;
                campaignStarter.AddBehavior(new SabotageCampaignBehavior());
            }
        }
    }
}
