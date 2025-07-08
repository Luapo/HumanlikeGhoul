using RimWorld;
using System.Collections.Generic;
using Verse;

namespace GhoulWorkAble
{
    internal class GhoulWorkAbleSettings : ModSettings
    {
        // 0:none change ,origin ghoul
        //1:can do easy work and use meal weapon and wear armor,smarter ghoul
        //2:can do most work human can do,the advance choice;
        public static string GhoulDefName = "Ghoul";
        public static string HumanDefName = "Human";
        public static List<float> PsyRatePreset = new List<float> { 0f, 0f, 0f, 0.1f };
        // useless stat,mainly for test
        public static float defaultMaxHungryPercent = 2f;
        public static List<bool> AllowEquipmentPreset = new List<bool> { false, false, true, true };
        public static List<bool> AllowIdeoPreset = new List<bool> { false, false, true, true };
        public static List<bool> AblityLimitPreset = new List<bool> { true, true, false, false };
        public static List<bool> GeneLimitPreset = new List<bool> { true, true, true, false };
        // no use after 1.6
        //public static List<bool> FoodLimitPreset = new List<bool> { true, true, true, false };
        //WorkTags.AllWork &~WorkTags.Violent is origin code
        public static List<WorkTags> DisableWorkTagsPreset = new List<WorkTags> { WorkTags.Shooting | WorkTags.AllWork,
            WorkTags.Shooting |WorkTags.Intellectual| WorkTags.Caring| WorkTags.Social|WorkTags.Artistic|WorkTags.Animals,
            WorkTags.Shooting |WorkTags.Intellectual| WorkTags.Caring| WorkTags.Social|WorkTags.Artistic|WorkTags.Animals,
            WorkTags.None };
        public WorkTags disableWorkTags = DisableWorkTagsPreset[2];
        public float PsyRate = PsyRatePreset[2];
        public bool allowIdeo = AllowIdeoPreset[2];
        public bool allowEquipment = AllowEquipmentPreset[2];
        public bool ablityLimit = AblityLimitPreset[2];
        public bool geneLimit = GeneLimitPreset[2];
        //public bool foodLimit = FoodLimitPreset[2];
        // test,no use now
        public Dictionary<string, string> defDic = new Dictionary<string, string> {
            {nameof(defaultMaxHungryPercent) ,"MaxNutrition"},
            {nameof(PsyRate) ,"PsychicSensitivity"}
        };
        public float hungryRatePercent = defaultMaxHungryPercent;
        public bool enableRPGTabPatch = true;
        public bool enableWorkTabPatch = true;
        public override void ExposeData()
        {

            base.ExposeData();
            Scribe_Values.Look(ref disableWorkTags, nameof(disableWorkTags), DisableWorkTagsPreset[2]);
            //Scribe_Values.Look(ref foodLimit, nameof(foodLimit), FoodLimitPreset[2]);
            // can't parse list into values ,so need to get locally
            Scribe_Values.Look(ref hungryRatePercent, nameof(hungryRatePercent), defaultMaxHungryPercent);
            Scribe_Values.Look(ref allowIdeo, nameof(allowIdeo), AllowIdeoPreset[2]);
            Scribe_Values.Look(ref allowEquipment, nameof(allowEquipment), AllowEquipmentPreset[2]);
            Scribe_Values.Look(ref ablityLimit, nameof(ablityLimit), AblityLimitPreset[2]);
            Scribe_Values.Look(ref PsyRate, nameof(PsyRate), PsyRatePreset[2]);
            Scribe_Values.Look(ref geneLimit, nameof(geneLimit), GeneLimitPreset[2]);
            Scribe_Values.Look(ref enableRPGTabPatch, nameof(enableRPGTabPatch), true);
            Scribe_Values.Look(ref enableWorkTabPatch, nameof(enableWorkTabPatch), true);
        }
        public void notifyAllDefChange()
        {
            notifyMutantDefChange();
            notifyHediffDefChange();
        }
        public void generateModComplier()
        {
            enableRPGTabPatch = OtherModPatches.IsRPGTabEnabled();
        }
        public void notifyThinkTreeDefChange()
        {
            string humanDefName = "Humanlike";
            var ghoulDef = DefDatabase<ThinkTreeDef>.GetNamed(GhoulDefName);
            var humanDef = DefDatabase<ThinkTreeDef>.GetNamed(humanDefName);
            ghoulDef = humanDef;
        }
        public void notifyHediffDefChange()
        {
            var def = DefDatabase<HediffDef>.GetNamed(GhoulDefName);
            if (def == null)
            {
                Verse.Log.Message("Ghoul work fail to change def");
                return;
            }
            foreach (HediffStage stage in def.stages)
            {
                foreach (StatModifier statModifier in stage.statFactors)
                {
                    if (statModifier.stat.defName == "MaxNutrition") statModifier.value = hungryRatePercent;
                    if (statModifier.stat.defName == "PsychicSensitivity") statModifier.value = PsyRate;
                }
            }
        }
        public void notifyFloatMenuChange()
        {
            var def = DefDatabase<MutantDef>.GetNamed(GhoulDefName);
            if (def == null)
            {
                Verse.Log.Message("Ghoul work fail to change def");
                return;
            }
            def.whitelistedFloatMenuProviders = null;
        }
        public void notifyMutantDefChange()
        {
            var def = DefDatabase<MutantDef>.GetNamed(GhoulDefName);
            if (def == null)
            {
                Verse.Log.Message("Ghoul work fail to change def");
                return;
            }
            //work
            def.workDisables = disableWorkTags;
            def.cachedDisabledWorkTypes = getEnableWorkTypes(disableWorkTags);
            /*
            if (!foodLimit)
            {
                def.foodType = DefDatabase<ThingDef>.GetNamed(HumanDefName).race.foodType;
            }*/
            // ideo and etc 
            if (!geneLimit)
            {
                def.disablesGenes.Clear();
            }
            if (!geneLimit)
            {
                //__result.drugWhitelist=DefMap<Thing>.
            }
            if (!ablityLimit)
            {
                def.abilityWhitelist = DefDatabase<AbilityDef>.AllDefsListForReading;
            }
            def.disablesIdeo = !allowIdeo;
            //equiments
            //__result.disablesGenes = Settings.geneLimit;
            //can wear equipment
            def.disableApparel = !allowEquipment;
            // available work type
        }
        public static List<Verse.WorkTypeDef> getEnableWorkTypes(WorkTags workTag)
        {
            List<Verse.WorkTypeDef> result = new List<WorkTypeDef>();
            List<Verse.WorkTypeDef> WorkTypelist = DefDatabase<WorkTypeDef>.AllDefsListForReading;
            foreach (Verse.WorkTypeDef work in WorkTypelist)
            {
                if ((workTag & work.workTags) != 0)
                {
                    result.Add(work);
                }
            }
            return result;
        }
        public void resetSettingIndex(int index)
        {
            disableWorkTags = DisableWorkTagsPreset[index];
            //enabledWorkTypes = getEnableWorkTypes(disableWorkTags);
            allowIdeo = AllowIdeoPreset[index];
            allowEquipment = AllowEquipmentPreset[index];
            ablityLimit = AblityLimitPreset[index];
            geneLimit = GeneLimitPreset[index];
            //foodLimit = FoodLimitPreset[index];
            PsyRate = PsyRatePreset[index];
            hungryRatePercent = defaultMaxHungryPercent;
            notifyAllDefChange();
        }
        public void changeWorkTag(WorkTags workTag)
        {
            disableWorkTags ^= workTag;
        }
        public void resetDefaultSetting()
        {
            resetSettingIndex(2);
        }
        public void resetNoneSetting()
        {
            resetSettingIndex(0);
        }
        public void resetDumbLaborSetting()
        {
            resetSettingIndex(1);
        }
        public void resetAdvanceSetting()
        {
            resetSettingIndex(3);
        }
    }
}
