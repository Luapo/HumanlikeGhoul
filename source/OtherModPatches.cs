using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using System.Reflection.Emit;
using Verse.AI.Group;
using static RimWorld.PsychicRitualRoleDef;

namespace GhoulWorkAble
{
    [StaticConstructorOnStartup]
    static class OtherModPatches
    {
        private static GhoulWorkAbleSettings Settings= LoadedModManager.GetMod<GhoulWorkAbleMod>().GetSettings<GhoulWorkAbleSettings>();
        public static bool IsRPGTabEnabled()
        {
            return ModsConfig.ActiveModsInLoadOrder.Any(m => m.Name == "RPG Style Inventory" || m.Name == "RPG Style Inventory Revamped");
        }
        static OtherModPatches()
        {
            var harmony = new Harmony("Luapo.ghoulWorkAble.otherModPatch");
            if (IsRPGTabEnabled())
            {
                String packageName = "Sandy_Detailed_RPG_Inventory";
                String typeName = "Sandy_Detailed_RPG_GearTab";
                String priorityName = "CanControlColonist";
                String targetType = packageName + "." + typeName;
                String targetProiority=targetType+"." + priorityName;
                Type Sandy_Detailed_RPG_GearTab = GenTypes.GetTypeInAnyAssembly(targetType);
                var method = AccessTools.PropertyGetter(Sandy_Detailed_RPG_GearTab, priorityName);
                harmony.Patch(original:method,
                postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CanControlColonist_PostFix)));
            }
        }
        static void CanControlColonist_PostFix(ref bool __result, ITab_Pawn_Gear __instance)
        {
            __result = __result || (__instance.CanControl && __instance.SelPawnForGear.IsColonyMutantPlayerControlled);
        }
    }
}
