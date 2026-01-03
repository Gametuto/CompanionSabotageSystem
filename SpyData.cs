using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace CompanionSabotageSystem
{
    public enum SpyState
    {
        TravelingToTarget = 0,
        Infiltrating = 1,
        ReturningToPlayer = 2
    }

    public class SpyData
    {
        [SaveableField(1)]
        public Settlement TargetSettlement;

        [SaveableField(2)]
        public int DaysRemaining;

        [SaveableField(3)]
        public SpyState State; // Maintenant il reconnait SpyState

        [SaveableField(6)]
        public Hero Agent;

        [SaveableField(4)]
        public int TotalFoodDestroyed = 0;

        [SaveableField(5)]
        public float TotalLoyaltyLost = 0f;

        [SaveableField(7)]
        public string PartyId;

        public SpyData() { }

        public SpyData(Hero agent, Settlement target, int days, SpyState state)
        {
            Agent = agent;
            TargetSettlement = target;
            DaysRemaining = days;
            State = state;
        }
    }
}
