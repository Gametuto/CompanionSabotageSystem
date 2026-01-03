using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace CompanionSabotageSystem
{
    public class SabotageSettings : AttributeGlobalSettings<SabotageSettings>
    {
        public override string Id => "CompanionSabotageSystem";
        public override string DisplayName => "Companion Sabotage System";
        public override string FolderName => "CompanionSabotage";
        public override string FormatType => "json2";

        [SettingPropertyFloatingInteger("Difficulty Factor", 0.5f, 2.0f, "0.0", Order = 1, RequireRestart = false, HintText = "Multiplies the risk of capture. 1.0 is default.")]
        public float DifficultyFactor { get; set; } = 1.0f;

        [SettingPropertyFloatingInteger("XP Gain Multiplier", 0.5f, 5.0f, "0.0", Order = 2, RequireRestart = false, HintText = "Multiplies the Roguery XP gained after mission.")]
        public float XPGainMultiplier { get; set; } = 1.0f;

        [SettingPropertyBool("Show Popups", Order = 3, RequireRestart = false, HintText = "If disabled, results will be shown as simple text messages.")]
        public bool ShowPopups { get; set; } = true;
    }
}