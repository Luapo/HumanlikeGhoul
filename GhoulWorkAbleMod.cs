using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace GhoulWorkAble
{
    public class GhoulWorkAbleMod : Mod
    {
        private readonly GhoulWorkAbleSettings _settings;
        private int index = 0;
        private static List<String> PageString = ["HumanlikeGhoul_Setting_MainPage", "HumanlikeGhoul_Setting_WorkPage", "HumanlikeGhoul_Setting_StatsPage",
        "HumanlikeGhoul_Setting_OtherModPage"];
        public GhoulWorkAbleMod(ModContentPack content) : base(content)
        {
            _settings = GetSettings<GhoulWorkAbleSettings>();
            //_settings.generateModComplier();
            index = 0;
        }
        public void DoSettingsWindowContentsWork(Rect inRect)
        {
            Listing_Standard leftListenr = new Listing_Standard();
            leftListenr.Begin(inRect with { width = inRect.width / 3 });
            leftListenr.Label("HumanlikeGhoul_AllowWorkType".Translate());
            leftListenr.Gap(10f);
            foreach (WorkTags workTag in Enum.GetValues(typeof(WorkTags)))
            {
                // has disable tags
                if (workTag == WorkTags.None) continue;
                if ((_settings.disableWorkTags & workTag) == workTag) continue;
                String name = Enum.GetName(typeof(WorkTags), workTag);
                String defName = "WorkTag" + name;
                if (leftListenr.ButtonText(defName.Translate()))
                {
                    _settings.changeWorkTag(workTag);
                    _settings.notifyAllDefChange();
                }
                leftListenr.Gap(1f);
            }
            leftListenr.End();
            Listing_Standard rightListener = new Listing_Standard();
            rightListener.Begin(inRect with { width = inRect.width / 3, x = inRect.width / 2 });
            rightListener.Label("HumanlikeGhoul_DisAllowWorkType".Translate());
            rightListener.Gap(10f);
            foreach (WorkTags workTag in Enum.GetValues(typeof(WorkTags)))
            {
                if (workTag == WorkTags.None) continue;
                if ((_settings.disableWorkTags & workTag) != workTag) continue;
                String name = Enum.GetName(typeof(WorkTags), workTag);
                String defName = "WorkTag" + name;
                //"HumanlikeGhoul_RemoveDisableWorkTab".Translate() +
                if (rightListener.ButtonText(defName.Translate()))
                {
                    _settings.changeWorkTag(workTag);
                    _settings.notifyAllDefChange();
                }
                rightListener.Gap(1f);
            }
            rightListener.End();
        }
        public void DoSettingsWindowContentsMain(Rect inRect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(inRect);
            listing_Standard.Label("HumanlikeGhoul_ResetWarring".Translate());
            if (listing_Standard.ButtonText("HumanlikeGhoul_ResetNone".Translate()))
            {
                _settings.resetNoneSetting();
            }
            listing_Standard.Gap(5f);
            if (listing_Standard.ButtonText("HumanlikeGhoul_ResetDumbLabor".Translate()))
            {
                _settings.resetDumbLaborSetting();
            }
            listing_Standard.Gap(5f);
            if (listing_Standard.ButtonText("HumanlikeGhoul_ResetDefault".Translate()))
            {
                _settings.resetDefaultSetting();
            }
            listing_Standard.Gap(5f);
            if (listing_Standard.ButtonText("HumanlikeGhoul_ResetAdvance".Translate()))
            {
                _settings.resetAdvanceSetting();
            }
            listing_Standard.Gap(5f);
            listing_Standard.CheckboxLabeled("HuamnlikeGhoul_EquipmentLimit".Translate(), ref _settings.allowEquipment, "HuamnlikeGhoul_EquipmentLimitToolTip".Translate());
            listing_Standard.Gap(5f);
            listing_Standard.CheckboxLabeled("HuamnlikeGhoul_IdeoLimit".Translate(), ref _settings.allowIdeo, "HuamnlikeGhoul_IdeoLimitToolTip".Translate());
            listing_Standard.Gap(5f);
            listing_Standard.CheckboxLabeled("HuamnlikeGhoul_GeneLimit".Translate(), ref _settings.geneLimit, "HuamnlikeGhoul_GeneLimitToolTip".Translate());
            listing_Standard.Gap(5f);
            listing_Standard.CheckboxLabeled("HuamnlikeGhoul_AblityLimit".Translate(), ref _settings.ablityLimit, "HuamnlikeGhoul_AblityLimitToolTip".Translate());
            //listing_Standard.Gap(5f);
            //listing_Standard.CheckboxLabeled("HuamnlikeGhoul_FoodLimit".Translate(), ref _settings.foodLimit, "HuamnlikeGhoul_FoodLimitToolTip".Translate());
            listing_Standard.End();
        }
        public void DoSettingsWindowContentsStats(Rect inRect)
        {
            Widgets.Label(inRect with { y = inRect.y, height = 24 }, "HumanlikeGhoul_StatsPageInfo".Translate());
            Widgets.Label(inRect with { width = inRect.width / 4, y = inRect.y + 30, height = 24 }, "HumanlikeGhoul_PsyRateLable".Translate());
            _settings.PsyRate = Widgets.HorizontalSlider(inRect with { x = inRect.width / 4, width = inRect.width / 4 * 3, y = inRect.y + 30, height = 24 }, _settings.PsyRate, 0f, 1f, true, $"{_settings.PsyRate * 100f}%", "0%", "100%", 0.1f);
            Widgets.Label(inRect with { width = inRect.width / 4, y = inRect.y + 60, height = 24 }, "HumanlikeGhoul_HungryRateLable".Translate());
            _settings.hungryRatePercent = Widgets.HorizontalSlider(inRect with { x = inRect.width / 4, width = inRect.width / 4 * 3, y = inRect.y + 60, height = 24 }, _settings.hungryRatePercent, 0.5f, 4f, true, $"{_settings.hungryRatePercent * 100f}%", "50%", "400%", 0.1f);
            if (Widgets.ButtonText(inRect with { y = inRect.y + 90, height = 36 }, "HumanlikeGhoul_RenewDefButton".Translate()))
            {
                _settings.notifyAllDefChange();
            }
        }
        public void DoSettingsWindowOtherMods(Rect inRect)
        {
            Widgets.Label(inRect with { y = inRect.y, height = 24 }, "HumanlikeGhoul_OtherModsPageInfo".Translate());
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(inRect with { y = inRect.y + 30 });
            listing_Standard.Gap(5f);
            listing_Standard.CheckboxLabeled("HumanlikeGhoul_RPGTabEnable".Translate(), ref _settings.enableRPGTabPatch, "HumanlikeGhoul_RPGTabEnableToolTip".Translate());
            listing_Standard.Gap(5f);
            listing_Standard.CheckboxLabeled("HumanlikeGhoul_WorkTabEnable".Translate(), ref _settings.enableWorkTabPatch, "HumanlikeGhoul_WorkTabEnableToolTip".Translate());
            listing_Standard.End();
        }
        public void DoSelectPage(Rect inRect, int index)
        {
            //can use list
            if (index == 0)
            {
                DoSettingsWindowContentsMain(inRect);
            }
            if (index == 1)
            {
                DoSettingsWindowContentsWork(inRect);
            }
            if (index == 2)
            {
                DoSettingsWindowContentsStats(inRect);
            }
            if (index == 3)
            {
                DoSettingsWindowOtherMods(inRect);
            }
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            int total = PageString.Count;
            float szPer = inRect.width / total;
            for (int i = 0; i < total; i++)
            {
                float szMt = (i == index ? 1.2f : 1f);
                Rect tp = inRect with { width = szPer * szMt, height = 28 * szMt, x = szPer * (i + ((i == index ? 0.4f : 0f))) };
                //if (i != index) Widgets.DrawLineHorizontal(tp.x, tp.y+tp.height, szPer*0.8f);
                if (i == index)
                {
                    Widgets.Label(tp, PageString[i].Translate());
                }
                else
                {
                    if (Widgets.ButtonText(tp, PageString[i].Translate()))
                    {
                        index = i;
                    };
                }
            }
            DoSelectPage(inRect with { y = inRect.y + 30 }, index);
            base.DoSettingsWindowContents(inRect);
        }


        public override string SettingsCategory()
        {
            return "HumanlikeGhoul_Setting".Translate();
        }
    }
}
