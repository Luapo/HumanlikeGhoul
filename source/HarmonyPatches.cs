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
using UnityEngine.UIElements;

namespace GhoulWorkAble
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        public static bool changeDef = false;
        public static String GhoulDefName = "Ghoul";
        public static int TickNum = 0;
        private static GhoulWorkAbleSettings Settings= LoadedModManager.GetMod<GhoulWorkAbleMod>().GetSettings<GhoulWorkAbleSettings>();
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
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(MainTabWindow_Assign), nameof(MainTabWindow_Assign.Pawns)),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(MainTabWindow_Work_pawns_PostFix)));
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(Pawn_OutfitTracker), nameof(Pawn_OutfitTracker.CurrentApparelPolicy)),
            transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_Tracker_Transpiler)));
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(Pawn_DrugPolicyTracker), nameof(Pawn_DrugPolicyTracker.CurrentPolicy)),
            transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_Tracker_Transpiler)));
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(Pawn_FoodRestrictionTracker), nameof(Pawn_FoodRestrictionTracker.CurrentFoodPolicy)),
            transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_Tracker_Transpiler)));
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(Pawn_ReadingTracker), nameof(Pawn_ReadingTracker.CurrentPolicy)),
            transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_Tracker_Transpiler)));
            // add aviliable order
            harmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.AddMutantOrders)),
               prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(AddMutantOrders_PreFix)));
            // remove humanorder food option
            harmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.AddHumanlikeOrders)),
            transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(AddHumanOrderFood_Transpiler)));
            // adjust pawn gear 
            harmony.Patch(original: AccessTools.FirstMethod(typeof(EquipmentUtility), CanEquip_Search),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CanEquip_PostFix)));

            harmony.Patch(original: AccessTools.PropertyGetter(typeof(ITab_Pawn_Gear), nameof(ITab_Pawn_Gear.CanControlColonist)),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CanControlColonist_PostFix)));
            // from carvan ghouls
            harmony.Patch(original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.CanEverCarryAnything)),
            transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(CanEverCarryAnything_Transpiler)));

            //adjust psychitRitual  and they will still can't use due to them psychit is 0
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(PsychicRitualCandidatePool), nameof(PsychicRitualCandidatePool.AllCandidatePawns)),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(PsychicRitualCandidatePool_AllCandidatePawns_Postfix)));
            //replace the mutant condition by humanlike,Ludden stuido leave the ghoul condition without making anything but leave them.
            harmony.Patch(original: AccessTools.Method(typeof(PsychicRitualRoleDef), nameof(PsychicRitualRoleDef.ConditionAllowed), [typeof(Condition)]),
            prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(ConditionAllowed_Prefix)));
            //adjust ideo role if exsit 
            if (ModsConfig.ideologyActive)
            {
                harmony.Patch(original: AccessTools.Method(typeof(SocialCardUtility), nameof(SocialCardUtility.DrawPawnRoleSelection)),
                transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(DrawPawnRoleSelection_Transpiler)));
                //mult overload so should use find method,disable mutant reason.
                harmony.Patch(original: AccessTools.FirstMethod(typeof(RitualRoleAssignments), PawnNotAssignableReason_Search),
                transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(PawnNotAssignableReason_Transpiler)));
                // can reuse here,althoung not recomended
                harmony.Patch(original: AccessTools.Method(typeof(RitualRoleIdeoRoleChanger), nameof(RitualRoleIdeoRoleChanger.AppliesToPawn)),
                transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(DrawPawnRoleSelection_Transpiler)));
                //fill the list with ghoul
                harmony.Patch(original: AccessTools.Method(typeof(RitualRoleAssignments), nameof(RitualRoleAssignments.Setup)),
                prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(RitualRoleAssignments_Setup_Prefix)));
                //change the precept(or called bug find) method of role .
                harmony.Patch(original: AccessTools.Method(typeof(Precept_Role), nameof(Precept_Role.ValidatePawn)),
                transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(Precept_Role_ValidatePawn_Transpiler)));
            }
            Verse.Log.Message("GhoulWorkAble is running now.");
        }
        static IEnumerable<CodeInstruction> AddHumanOrderFood_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            /*
			    IL_03ca: ldloc.s 14
			    IL_03cc: ldfld class RimWorld.FloatMenuMakerMap/'<>c__DisplayClass12_0' RimWorld.FloatMenuMakerMap/'<>c__DisplayClass12_2'::'CS$<>8__locals2'
			    IL_03d1: ldfld class Verse.Pawn RimWorld.FloatMenuMakerMap/'<>c__DisplayClass12_0'::pawn
			    IL_03d6: callvirt instance class Verse.RaceProperties Verse.Pawn::get_RaceProps()
			    IL_03db: ldloc.s 14
			    IL_03dd: ldfld class Verse.Thing RimWorld.FloatMenuMakerMap/'<>c__DisplayClass12_2'::t
			    IL_03e2: callvirt instance bool Verse.RaceProperties::CanEverEat(class Verse.Thing)
			    IL_03e7: brfalse IL_0865
             */
            // (pawn.RaceProperites.canevereat&&!IsMutantPlayerControl)
            var found = false;
            Queue<CodeInstruction> prevInstruction = new Queue<CodeInstruction> { };
            var targetMethod = AccessTools.Method(typeof(RaceProperties), nameof(RaceProperties.CanEverEat),
                new Type[] { typeof(Thing) });
            if (targetMethod == null)
            {
                Verse.Log.Message("GhoulWorkAble failed to humanOrder food." + Environment.StackTrace);
            }
            foreach (var instruction in instructions)
            {
                if (targetMethod != null
                    && instruction.Calls(targetMethod))
                {
                    //Verse.Log.Message("found " + instruction);
                    //put the pawn address backup
                    yield return instruction;
                    /*
                        IL_03ca: ldloc.s 14
			            IL_03cc: ldfld class RimWorld.FloatMenuMakerMap/'<>c__DisplayClass12_0' RimWorld.FloatMenuMakerMap/'<>c__DisplayClass12_2'::'CS$<>8__locals2'
			            IL_03d1: ldfld class Verse.Pawn RimWorld.FloatMenuMakerMap/'<>c__DisplayClass12_0'::pawn
                    */
                    for (int i = 0; i < 3; i++)
                    {
                        yield return prevInstruction.Dequeue();
                    }
                    // get is Colony Mutant
                    yield return new CodeInstruction(OpCodes.Callvirt,
                        AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsColonyMutantPlayerControlled)));
                    // out result
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Xor);
                    yield return new CodeInstruction(OpCodes.And);
                    found = true;
                }
                else yield return instruction;
                prevInstruction.Enqueue(instruction);
                if (prevInstruction.Count > 6){
                    prevInstruction.Dequeue();
                }
            }
            if (found is false)
                Verse.Log.Message("GhoulWorkAble failed to humanOrder food." + Environment.StackTrace);
        }
        static bool CanEquip_Search(MethodInfo method)
        {
            return method.IsStatic && method.Name == nameof(EquipmentUtility.CanEquip)&&method.GetParameters().Length>=3;
        }
        static void CanEquip_PostFix(ref bool __result, Pawn pawn,ref string cantReason)
        {
            if (__result && pawn.IsColonyMutantPlayerControlled&&!Settings.allowEquipment)
            {
                cantReason = "HumanLikeGhoul_CantWearCause".Translate();
                __result = false;
            }
        }
        static IEnumerable<CodeInstruction> Precept_Role_ValidatePawn_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            /*
                	IL_0015: ldarg.1
		            IL_0016: callvirt instance bool Verse.Pawn::get_IsFreeNonSlaveColonist()
		            IL_001b: brtrue.s IL_001f
             */
                    // (IsFreeNonSlaveColonist||isMutantPlayerControl)
                    var found = false;
            CodeInstruction prevInstruction = null;
            var targetMethod = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsFreeNonSlaveColonist));
            if (targetMethod == null)
            {
                Verse.Log.Message("GhoulWorkAble failed to reflect ideo method." + Environment.StackTrace);
            }
            foreach (var instruction in instructions)
            {
                if (targetMethod != null
                    && instruction.Calls(targetMethod))
                {
                    //Verse.Log.Message("found " + instruction);
                    //get origin result
                    yield return instruction;
                    //put address
                    yield return prevInstruction;
                    // get is Colony Mutant
                    yield return new CodeInstruction(OpCodes.Callvirt,
                        AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsColonyMutantPlayerControlled)));
                    // out result
                    yield return new CodeInstruction(OpCodes.Or);
                    found = true;
                }
                else yield return instruction;
                prevInstruction = instruction;
            }
            if (found is false)
                Verse.Log.Message("GhoulWorkAble failed to reflect ideo role." + Environment.StackTrace);
        }
        public static void ConditionAllowed_Prefix(ref Condition condition)
        {
            condition = (condition == Condition.Mutant) ?Condition.Humanlike: condition;
        }
        public static void PsychicRitualCandidatePool_AllCandidatePawns_Postfix(ref List<Pawn>__result)
        {
            __result = (__result.Concat(Find.CurrentMap.mapPawns.ColonyMutants)).ToList();
        }
        public static void RitualRoleAssignments_Setup_Prefix(ref List<Pawn> allPawns)
        {
            if (allPawns != null)
            {
                allPawns=(allPawns.Concat(Find.CurrentMap.mapPawns.ColonyMutants)).ToList();
            }
            else
            {
                allPawns = Find.CurrentMap.mapPawns.ColonyMutants.ToList();
            }
        }
        static bool PawnNotAssignableReason_Search(MethodInfo method)
        {
            return method.IsStatic && method.Name == nameof(RitualRoleAssignments.PawnNotAssignableReason);
        }
        static void Pawn_MutantTrackerDef_PreFix(Pawn __instance){
            // only change ghoul
            if (__instance.mutant?.def?.defName!= GhoulDefName) return;
            TickNum += 1;
            if (changeDef&&TickNum<=10000) return;
            TickNum = 0;
            changeDef = true;
            Settings.notifyHediffDefChange();
            Settings.notifyMutantDefChange();
        }
        //need be move to somewhere for auto load

        static IEnumerable<CodeInstruction> DrawPawnRoleSelection_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            /*
                IL_0000: newobj instance void RimWorld.SocialCardUtility/'<>c__DisplayClass42_0'::.ctor()
                IL_0005: stloc.0
                IL_0006: ldloc.0
                IL_0007: ldarg.0
                IL_0008: stfld class Verse.Pawn RimWorld.SocialCardUtility/'<>c__DisplayClass42_0'::pawn
                IL_000d: ldloc.0
                IL_000e: ldfld class Verse.Pawn RimWorld.SocialCardUtility/'<>c__DisplayClass42_0'::pawn
                IL_0013: callvirt instance bool Verse.Pawn::get_IsFreeNonSlaveColonist()
             */
            var found = false;
            CodeInstruction prevInstruction = null;
            var targetMethod = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsFreeNonSlaveColonist));
            if (targetMethod == null){
                Verse.Log.Message("GhoulWorkAble failed to reflect ideo method." + Environment.StackTrace);
            }
            foreach (var instruction in instructions)
            {
                if (targetMethod!=null
                    && instruction.Calls(targetMethod))
                {
                    //Verse.Log.Message("found " + instruction);
                    //get origin result
                    yield return instruction;
                    //put the pawn 
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return prevInstruction;
                    // get is Colony Mutant
                    yield return new CodeInstruction(OpCodes.Callvirt,
                        AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsColonyMutantPlayerControlled)));
                    // out result
                    yield return new CodeInstruction(OpCodes.Or);
                    found = true;
                }
                else yield return instruction;
                prevInstruction = instruction;
            }
            if (found is false)
                Verse.Log.Message("GhoulWorkAble failed to reflect ideo role." + Environment.StackTrace);
        }
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
                Verse.Log.Message("GhoulWorkAble failed to reflect ideo method." + Environment.StackTrace);
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
                        AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsColonyMutantPlayerControlled)));
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
                Verse.Log.Message("GhoulWorkAble failed to reflect ideo role." + Environment.StackTrace);
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
            var targetMethod = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsMutant));
            CodeInstruction prevInstruction = null;
            if (targetMethod == null)
            {
                Verse.Log.Message("GhoulWorkAble failed to reflect ideo method." + Environment.StackTrace);
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
                        AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsColonyMutantPlayerControlled)));
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
                Verse.Log.Message("GhoulWorkAble failed to reflect ideo role." + Environment.StackTrace);
        }
        static void MainTabWindow_Work_pawns_PostFix(ref IEnumerable<Pawn> __result)
        {
            __result = __result.Concat(Find.CurrentMap.mapPawns.ColonyMutants);
        }
        static void CanControlColonist_PostFix(ref bool __result, ITab_Pawn_Gear __instance)
        {
            __result = __result || (__instance.CanControl && __instance.SelPawnForGear.IsColonyMutantPlayerControlled);
        }
        private static void AddMutantOrders_PreFix(Vector3 clickPos, Pawn pawn, List<FloatMenuOption> opts)
        {
            //Verse.Log.Message("ghoulworkable" + GhoulWorkAbleUtil.IsGhoul(pawn)+","+pawn.IsGhoul+","+pawn.IsColonyMutantPlayerControlled);
            if (!GhoulWorkAbleUtil.IsGhoul(pawn)) return;
            if (!pawn.Drafted && (!pawn.RaceProps.IsMechanoid || DebugSettings.allowUndraftedMechOrders) &&pawn.IsMutant){
                FloatMenuMakerMap.AddUndraftedOrders(clickPos, pawn, opts);
            }
            FloatMenuMakerMap.AddHumanlikeOrders(clickPos, pawn, opts);
            /*
            foreach  (FloatMenuOption opt in opts)
            {
                //if opt.
                //opts.Remove(opt);
            }
            */
        }
        private static void CanBeUsedBy_prefix(CompUsable __instance, Pawn p, bool forced, bool ignoreReserveAndReachable)
        {
            //Verse.Log.Message("GhoulWorkAble is running now. add ghoul allow");
            if (p.IsMutant && !__instance.Props.allowedMutants.Contains(p.mutant.Def)&& p.mutant.Def.defName== GhoulDefName)
            {
                //Verse.Log.Message("GhoulWorkAble is running now. add ghoul allow");
                __instance.Props.allowedMutants.Add(p.mutant.Def);
            }
        }
        private static IEnumerable<CodeInstruction> CanEverCarryAnything_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            MethodInfo targetMethod = AccessTools.PropertyGetter(typeof(Pawn), "IsMutant");
            if (targetMethod == null)
            {
                Log.Message("Failed to reflect carry weight." + Environment.StackTrace);
            }
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (targetMethod != null && !found && CodeInstructionExtensions.Calls(instruction, targetMethod))
                {
                    found = true;
                    //replace by false
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                }
            }
            if (!found)
            {
                Log.Message("Failed to reflect carry weight." + Environment.StackTrace);
            }
        }
    }
}
