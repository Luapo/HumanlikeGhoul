using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using static RimWorld.PsychicRitualRoleDef;

namespace GhoulWorkAble.HarmonyPatches
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        public static bool changeDef = false;
        public static string GhoulDefName = "Ghoul";
        public static int TickNum = 0;
        private static GhoulWorkAbleSettings Settings = LoadedModManager.GetMod<GhoulWorkAbleMod>().GetSettings<GhoulWorkAbleSettings>();
        static HarmonyPatches()
        {
            var harmony = new Harmony("Luapo.ghoulWorkAble");
            // need to move to other 
            //change ghoul def
            // may cause peformance problem.
            harmony.Patch(original: AccessTools.Method(typeof(Pawn), nameof(Pawn.GetDisabledWorkTypes)),
               prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_MutantTrackerDef_PreFix)));
            //AI work
            // not recommender reuse
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.CanPawnTakeOpportunisticJob)),
                transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(PawnNotAssignableReason_Transpiler)));
            // adjust pawntable 
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(MainTabWindow_Work), nameof(MainTabWindow_Work.Pawns)),
              postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(MainTabWindow_Work_pawns_PostFix)));
            //harmony.Patch(original: AccessTools.PropertyGetter(typeof(MainTabWindow_Assign), nameof(MainTabWindow_Assign.Pawns)),
            //postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(MainTabWindow_Work_pawns_PostFix)));
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(Pawn_OutfitTracker), nameof(Pawn_OutfitTracker.CurrentApparelPolicy)),
            transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_Tracker_Transpiler)));
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(Pawn_DrugPolicyTracker), nameof(Pawn_DrugPolicyTracker.CurrentPolicy)),
            transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_Tracker_Transpiler)));
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(Pawn_FoodRestrictionTracker), nameof(Pawn_FoodRestrictionTracker.CurrentFoodPolicy)),
            transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_Tracker_Transpiler)));
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(Pawn_ReadingTracker), nameof(Pawn_ReadingTracker.CurrentPolicy)),
            transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_Tracker_Transpiler)));
            // add aviliable order
            // adjust pawn gear 
            harmony.Patch(original: AccessTools.FirstMethod(typeof(EquipmentUtility), CanEquip_Search),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CanEquip_PostFix)));

            harmony.Patch(original: AccessTools.PropertyGetter(typeof(ITab_Pawn_Gear), nameof(ITab_Pawn_Gear.CanControlColonist)),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CanControlColonist_PostFix)));

            //adjust psychitRitual  and they will still can't use due to them psychit is 0
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(PsychicRitualCandidatePool), nameof(PsychicRitualCandidatePool.AllCandidatePawns)),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(PsychicRitualCandidatePool_AllCandidatePawns_Postfix)));
            //replace the mutant condition by humanlike,Ludden stuido leave the ghoul condition without making anything but leave them.
            harmony.Patch(original: AccessTools.Method(typeof(PsychicRitualRoleDef), nameof(PsychicRitualRoleDef.ConditionAllowed), [typeof(Condition)]),
            prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(ConditionAllowed_Prefix)));
            //adjust ideo role if exsit 
            Log.Message("GhoulWorkAble is running now.");
        }
        static bool CanEquip_Search(MethodInfo method)
        {
            return method.IsStatic && method.Name == nameof(EquipmentUtility.CanEquip) && method.GetParameters().Length >= 3;
        }
        static void CanEquip_PostFix(ref bool __result, Pawn pawn, ref string cantReason)
        {
            if (__result && pawn.IsColonySubhumanPlayerControlled && !Settings.allowEquipment)
            {
                cantReason = "HumanLikeGhoul_CantWearCause".Translate();
                __result = false;
            }
        }
        //need rework
        public static void ConditionAllowed_Prefix(ref Condition condition)
        {
            condition = condition;
        }
        public static void PsychicRitualCandidatePool_AllCandidatePawns_Postfix(ref List<Pawn> __result)
        {
            __result = __result.Concat(Find.CurrentMap.mapPawns.ColonySubhumansControllable).ToList();
        }
        public static void RitualRoleAssignments_Setup_Prefix(ref List<Pawn> allPawns)
        {
            if (allPawns != null)
            {
                allPawns = allPawns.Concat(Find.CurrentMap.mapPawns.ColonySubhumansControllable).ToList();
            }
            else
            {
                allPawns = Find.CurrentMap.mapPawns.ColonySubhumansControllable.ToList();
            }
        }
        static bool PawnNotAssignableReason_Search(MethodInfo method)
        {
            return method.IsStatic && method.Name == nameof(RitualRoleAssignments.PawnNotAssignableReason);
        }
        static void Pawn_MutantTrackerDef_PreFix(Pawn __instance)
        {
            // only change ghoul
            if (__instance.mutant?.def?.defName != GhoulDefName) return;
            TickNum += 1;
            if (changeDef && TickNum <= 10000) return;
            TickNum = 0;
            changeDef = true;
            Settings.notifyHediffDefChange();
            Settings.notifyMutantDefChange();
            Settings.notifyFloatMenuChange();
            Settings.notifyThinkTreeDefChange();
        }
        //need be move to somewhere for auto load
        static IEnumerable<CodeInstruction> Pawn_Tracker_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            /*
		        IL_0000: ldarg.0
		        IL_0001: ldfld class Verse.Pawn RimWorld.Pawn_OutfitTracker::pawn
		        IL_0006: callvirt instance bool Verse.Pawn::get_IsMutant()
		        IL_000b: brfalse.s IL_000f
             */
            var found = false;
            var targetMethod = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsMutant));
            Stack<CodeInstruction> prevInstruction = new Stack<CodeInstruction> { };
            if (targetMethod == null)
            {
                Log.Message("GhoulWorkAble failed to reflect ideo method." + Environment.StackTrace);
            }
            foreach (var instruction in instructions)
            {
                if (targetMethod != null
                    && instruction.Calls(targetMethod))
                {
                    // if (is mutant & ! is player ColonyMutantPlayerControlled)
                    //Verse.Log.Message("found " + instruction);
                    CodeInstruction tp = prevInstruction.Pop();
                    yield return instruction;
                    yield return prevInstruction.Pop();
                    yield return tp;
                    yield return new CodeInstruction(OpCodes.Callvirt,
                        AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsColonySubhumanPlayerControlled)));
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Xor);
                    yield return new CodeInstruction(OpCodes.And);
                    // out result
                    found = true;
                }
                else yield return instruction;
                prevInstruction.Push(instruction);
                if (prevInstruction.Count >= 3)
                {
                    prevInstruction.Pop();
                }
            }
            if (found is false)
                Log.Message("GhoulWorkAble failed to reflect ideo role." + Environment.StackTrace);
        }
        static IEnumerable<CodeInstruction> PawnNotAssignableReason_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            /*
                IL_0134: ldarg.0
		        IL_0135: callvirt instance bool Verse.Pawn::get_IsMutant()
		        IL_013a: brfalse.s IL_014c

		        IL_013c: ldstr "MessageRitualCannotBeMutant"
		        IL_0141: call valuetype Verse.TaggedString Verse.Translator::Translate(string)
		        IL_0146: call string Verse.TaggedString::op_Implicit(valuetype Verse.TaggedString)
		        IL_014b: ret
             */
            var found = false;
            var targetMethod = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsSubhuman));
            CodeInstruction prevInstruction = null;
            if (targetMethod == null)
            {
                Log.Message("GhoulWorkAble failed to reflect ideo method." + Environment.StackTrace);
            }
            foreach (var instruction in instructions)
            {
                if (targetMethod != null
                    && instruction.Calls(targetMethod))
                {
                    // if (is mutant & ! is player ColonyMutantPlayerControlled)
                    //Verse.Log.Message("found " + instruction);
                    yield return instruction;
                    yield return prevInstruction;
                    yield return new CodeInstruction(OpCodes.Callvirt,
                        AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsColonySubhumanPlayerControlled)));
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Xor);
                    yield return new CodeInstruction(OpCodes.And);
                    // out result
                    found = true;
                }
                else yield return instruction;
                prevInstruction = instruction;
            }
            if (found is false)
                Log.Message("GhoulWorkAble failed to reflect ideo role." + Environment.StackTrace);
        }
        public static void MainTabWindow_Work_pawns_PostFix(ref IEnumerable<Pawn> __result)
        {
            __result = __result.Concat(Find.CurrentMap.mapPawns.ColonySubhumansControllable);
        }
        public static void CanControlColonist_PostFix(ref bool __result, ITab_Pawn_Gear __instance)
        {
            __result = __result || __instance.CanControl && __instance.SelPawnForGear.IsColonySubhumanPlayerControlled;
        }
        private static void CanBeUsedBy_prefix(CompUsable __instance, Pawn p, bool forced, bool ignoreReserveAndReachable)
        {
            //Verse.Log.Message("GhoulWorkAble is running now. add ghoul allow");
            if (p.IsMutant && !__instance.Props.allowedMutants.Contains(p.mutant.Def) && p.mutant.Def.defName == GhoulDefName)
            {
                //Verse.Log.Message("GhoulWorkAble is running now. add ghoul allow");
                __instance.Props.allowedMutants.Add(p.mutant.Def);
            }
        }

    }
}
