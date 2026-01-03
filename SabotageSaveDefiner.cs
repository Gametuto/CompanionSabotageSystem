using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace CompanionSabotageSystem
{
    public class SabotageSaveDefiner : SaveableTypeDefiner
    {
        // On utilise un ID unique assez grand pour éviter les conflits avec d'autres mods
        public SabotageSaveDefiner() : base(854_120_333) { }

        protected override void DefineClassTypes()
        {
            // On déclare notre classe custom
            AddClassDefinition(typeof(SpyData), 1);
            AddClassDefinition(typeof(SpyPartyComponent), 3); // <--- AJOUTE ÇA (ID différent de SpyData)
        }

        protected override void DefineEnumTypes()
        {
            // On déclare notre Enum
            AddEnumDefinition(typeof(SpyState), 2);
        }

        protected override void DefineContainerDefinitions()
        {
            // On déclare le Dictionnaire Hero -> SpyData
            ConstructContainerDefinition(typeof(Dictionary<Hero, SpyData>));
        }
    }
}
