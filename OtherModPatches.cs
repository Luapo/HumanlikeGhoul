using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace GhoulWorkAble
{
    [StaticConstructorOnStartup]
    static class OtherModPatches
    {
        private static GhoulWorkAbleSettings Settings = LoadedModManager.GetMod<GhoulWorkAbleMod>().GetSettings<GhoulWorkAbleSettings>();
        private static Type WorkTabType = null;
        public static bool IsRPGTabEnabled()
        {
            return ModsConfig.ActiveModsInLoadOrder.Any(m => m.Name == "RPG Style Inventory" || m.Name == "RPG Style Inventory Revamped");
        }
        public static bool IsWorkTabEnable()
        {
            return ModsConfig.ActiveModsInLoadOrder.Any(m => m.Name == "Work Tab 1.5 Temp" || m.Name == "Work Tab");
        }
        static OtherModPatches()
        {
            var harmony = new Harmony("Luapo.ghoulWorkAble.otherModPatch");
            if (IsWorkTabEnable())
            {
                String packageName = "WorkTab";
                String typeName = "MainTabWindow_WorkTab";
                String fieldName = "Pawns";
                String type = packageName + "." + typeName;
                String field = type + "." + fieldName;
                Type tableType = GenTypes.GetTypeInAnyAssembly(type);
                WorkTabType = tableType;
                Verse.Log.Message(tableType);
                harmony.Patch(original: AccessTools.PropertyGetter(typeof(MainTabWindow_PawnTable), nameof(MainTabWindow_PawnTable.Pawns)),
                postfix: new HarmonyMethod(typeof(OtherModPatches), nameof(PawnList_PostFix)));
            }
            if (IsRPGTabEnabled())
            {
                String packageName = "Sandy_Detailed_RPG_Inventory";
                String typeName = "Sandy_Detailed_RPG_GearTab";
                String priorityName = "CanControlColonist";
                String targetType = packageName + "." + typeName;
                String targetProiority = targetType + "." + priorityName;
                Type Sandy_Detailed_RPG_GearTab = GenTypes.GetTypeInAnyAssembly(targetType);
                var method = AccessTools.PropertyGetter(Sandy_Detailed_RPG_GearTab, priorityName);
                harmony.Patch(original: method,
                postfix: new HarmonyMethod(typeof(OtherModPatches), nameof(CanControlColonist_PostFix)));
            }
        }
        static void CanControlColonist_PostFix(ref bool __result, ITab_Pawn_Gear __instance)
        {
            __result = __result || (__instance.CanControl && __instance.SelPawnForGear.IsColonySubhumanPlayerControlled);
        }
        public static void PawnList_PostFix(ref MainTabWindow_PawnTable __instance, ref IEnumerable<Pawn> __result)
        {
            if (__instance.GetType() == WorkTabType)
            {
                __result = __result.Concat(Find.CurrentMap.mapPawns.ColonySubhumansControllable);
            }
        }
    }
}
