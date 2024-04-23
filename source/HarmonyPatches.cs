using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;
using Verse.AI;
using static HarmonyLib.Code;
using System.Linq.Expressions;
using System.Reflection.Emit;
using UnityEngine.XR;
using System.Net;
using NAudio.CoreAudioApi;
using Verse.AI.Group;
using static RimWorld.PsychicRitualRoleDef;

namespace GhoulWorkAble
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        public static bool changeDef = false;
        public static String GhoulDefName = "Ghoul";
        private static GhoulWorkAbleSettings Settings= LoadedModManager.GetMod<GhoulWorkAbleMod>().GetSettings<GhoulWorkAbleSettings>();
        static HarmonyPatches()
        {
            var harmony = new Harmony("Luapo.ghoulWorkAble");
            //change ghoul def
            // may cause peformance problem.
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(Pawn_MutantTracker), nameof(Pawn_MutantTracker.Def)),
               postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(Pawn_MutantTrackerDef_PostFix)));
            //AI work
            // not recommender reuse
            harmony.Patch(original: AccessTools.Method(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.CanPawnTakeOpportunisticJob)),
                transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(PawnNotAssignableReason_Transpiler)));
            // adjust worktable 
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(MainTabWindow_Work), nameof(MainTabWindow_Work.Pawns)),
              postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(MainTabWindow_Work_pawns_PostFix)));
            // add aviliable order
            harmony.Patch(original: AccessTools.Method(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.AddMutantOrders)),
               prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(AddMutantOrders_PreFix)));
            // adjust pawn gear 
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(ITab_Pawn_Gear), nameof(ITab_Pawn_Gear.CanControlColonist)),
            postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(CanControlColonist_PostFix)));
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
        static void Pawn_MutantTrackerDef_PostFix(ref MutantDef __result)
        {
            // only change ghoul
            if (__result.defName != GhoulDefName) return;
            //work
            if (!changeDef)
            {
                changeDef = true;
                Settings.notifyHediffDefChange();
            }
            __result.disabledWorkTags = Settings.disableWorkTags;
            __result.enabledWorkTypes = GhoulWorkAbleSettings.getEnableWorkTypes(Settings.disableWorkTags);
            //DefGenerator
            //__result.enabledWorkTypes = Settings.enabledWorkTypes;
            // ideo and etc 
            if (!Settings.geneLimit){
                __result.disablesGenes.Clear();
            }
            if (!Settings.geneLimit)
            {
                //__result.drugWhitelist=DefMap<Thing>.
            }
            if (!Settings.ablityLimit)
            {
                __result.abilityWhitelist = DefDatabase<AbilityDef>.defsList;
            }
            __result.removeIdeo = !Settings.allowIdeo;
            //equiments
            //__result.disablesGenes = Settings.geneLimit;
            //can wear equipment
            __result.canWearApparel = Settings.allowEquipment;
            // available work type
        }
        //need be rewrite

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
    }
}
