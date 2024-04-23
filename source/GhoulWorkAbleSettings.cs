using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;

namespace GhoulWorkAble
{
    internal class GhoulWorkAbleSettings : ModSettings
    {
        // 0:none change ,origin ghoul
        //1:can do easy work and use meal weapon and wear armor,smarter ghoul
        //2:can do most work human can do,the advance choice;
        public static string GhoulDefName="Ghoul";
        public static List<float> PsyRatePreset = new List<float> {0f,0f,0.1f };
        // useless stat,mainly for test
        public static float defaultMaxHungryPercent = 2f;
        public static List<bool> AllowEquipmentPreset = new List<bool> { false, true, true };
        public static List<bool> AllowIdeoPreset = new List<bool> { false, false, true };
        public static List<bool> AblityLimitPreset = new List<bool> { true, true, false };
        public static List<bool> GeneLimitPreset = new List<bool> { true, true, false };
        public static List<bool> FoodLimitPreset = new List<bool> { true, true, true };
        //WorkTags.AllWork &~WorkTags.Violent is origin code
        public static List<WorkTags> DisableWorkTagsPreset = new List<WorkTags> { WorkTags.AllWork&~WorkTags.Violent,
            WorkTags.Intellectual| WorkTags.Caring| WorkTags.Social|WorkTags.Cooking|WorkTags.Shooting|WorkTags.Artistic
            |WorkTags.Crafting|WorkTags.Firefighting,
            WorkTags.None };
        public WorkTags disableWorkTags = DisableWorkTagsPreset[1];
        public float PsyRate = PsyRatePreset[1];
        public bool allowIdeo = AllowIdeoPreset[1];
        public bool allowEquipment = AllowEquipmentPreset[1];
        public bool ablityLimit = AblityLimitPreset[1];
        public bool geneLimit = GeneLimitPreset[1];
        public bool foodLimit = FoodLimitPreset[1];
        // test,no use now
        public Dictionary<string, string> defDic = new Dictionary<string, string> {
            {nameof(defaultMaxHungryPercent) ,"MaxNutrition"},
            {nameof(PsyRate) ,"PsychicSensitivity"}
        };
        public float hungryRatePercent = defaultMaxHungryPercent;
        public override void ExposeData()
        {

            base.ExposeData();
            Scribe_Values.Look(ref disableWorkTags, nameof(disableWorkTags), DisableWorkTagsPreset[1]);
            // can't parse list into values ,so need to get locally
            //Scribe_Values.Look(ref enabledWorkTypes, nameof(enabledWorkTypes), getEnableWorkTypes(DisableWorkTagsPreset[1]));
            Scribe_Values.Look(ref hungryRatePercent, nameof(hungryRatePercent), defaultMaxHungryPercent);
            Scribe_Values.Look(ref allowIdeo, nameof(allowIdeo), AllowIdeoPreset[1]);
            Scribe_Values.Look(ref allowEquipment, nameof(allowEquipment), AllowEquipmentPreset[1]);
            Scribe_Values.Look(ref ablityLimit, nameof(ablityLimit), AblityLimitPreset[1]);
            Scribe_Values.Look(ref PsyRate, nameof(PsyRate), PsyRatePreset[1]);
            Scribe_Values.Look(ref geneLimit, nameof(geneLimit), GeneLimitPreset[1]);
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
                foreach(StatModifier statModifier in stage.statFactors)
                {
                    if (statModifier.stat.defName == "MaxNutrition") statModifier.value = hungryRatePercent;
                    if (statModifier.stat.defName == "PsychicSensitivity") statModifier.value = PsyRate;
                }
            }
        }
        public static List<Verse.WorkTypeDef> getEnableWorkTypes(WorkTags workTag)
        {
            List<Verse.WorkTypeDef> result = new List<WorkTypeDef>();
            List<Verse.WorkTypeDef> WorkTypelist = DefDatabase<WorkTypeDef>.AllDefsListForReading;
            foreach (Verse.WorkTypeDef work in WorkTypelist)
            {
                if ((workTag & work.workTags) == 0)
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
            foodLimit = FoodLimitPreset[index];
            PsyRate = PsyRatePreset[index];
            hungryRatePercent = defaultMaxHungryPercent;
            notifyHediffDefChange();
        }
        public void changeWorkTag(WorkTags workTag)
        {
            disableWorkTags ^= workTag;
        }
        public void resetDefaultSetting()
        {
            resetSettingIndex(1);
        }
        public void resetNoneSetting()
        {
            resetSettingIndex(0);
        }
        public void resetAdvanceSetting()
        {
            resetSettingIndex(2);
        }
    }
}
