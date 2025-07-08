using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace GhoulWorkAble.HarmonyPatches
{

    [StaticConstructorOnStartup]
    internal class IdeologyPatches
    {
        private static GhoulWorkAbleSettings Settings = LoadedModManager.GetMod<GhoulWorkAbleMod>().GetSettings<GhoulWorkAbleSettings>();
        static IdeologyPatches()
        {
            var harmony = new Harmony(typeof(IdeologyPatches).FullName);
            if (ModsConfig.ideologyActive)
            {
                harmony.Patch(original: AccessTools.Method(typeof(SocialCardUtility), nameof(SocialCardUtility.DrawPawnRoleSelection)),
                transpiler: new HarmonyMethod(typeof(IdeologyPatches), nameof(DrawPawnRoleSelection_Transpiler)));
                //mult overload so should use find method,disable mutant reason.
                harmony.Patch(original: AccessTools.FirstMethod(typeof(RitualRoleAssignments), PawnNotAssignableReason_Search),
                transpiler: new HarmonyMethod(typeof(IdeologyPatches), nameof(PawnNotAssignableReason_Transpiler)));
                // can reuse here,althoung not recomended
                harmony.Patch(original: AccessTools.Method(typeof(RitualRoleIdeoRoleChanger), nameof(RitualRoleIdeoRoleChanger.AppliesToPawn)),
                transpiler: new HarmonyMethod(typeof(IdeologyPatches), nameof(DrawPawnRoleSelection_Transpiler)));
                //fill the list with ghoul
                harmony.Patch(original: AccessTools.Method(typeof(RitualRoleAssignments), nameof(RitualRoleAssignments.Setup)),
                prefix: new HarmonyMethod(typeof(IdeologyPatches), nameof(RitualRoleAssignments_Setup_Prefix)));
                //change the precept(or called bug find) method of role .
                harmony.Patch(original: AccessTools.Method(typeof(Precept_Role), nameof(Precept_Role.ValidatePawn)),
                transpiler: new HarmonyMethod(typeof(IdeologyPatches), nameof(Precept_Role_ValidatePawn_Transpiler)));
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
                Log.Message("GhoulWorkAble failed to reflect ideo method." + Environment.StackTrace);
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
                        AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsColonySubhumanPlayerControlled)));
                    // out result
                    yield return new CodeInstruction(OpCodes.Or);
                    found = true;
                }
                else yield return instruction;
                prevInstruction = instruction;
            }
            if (found is false)
                Log.Message("GhoulWorkAble failed to reflect ideo role." + Environment.StackTrace);
        }
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
            if (targetMethod == null)
            {
                Log.Message("GhoulWorkAble failed to reflect ideo method." + Environment.StackTrace);
            }
            foreach (var instruction in instructions)
            {
                if (targetMethod != null
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
                        AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsColonySubhumanPlayerControlled)));
                    // out result
                    yield return new CodeInstruction(OpCodes.Or);
                    found = true;
                }
                else yield return instruction;
                prevInstruction = instruction;
            }
            if (found is false)
                Log.Message("GhoulWorkAble failed to reflect ideo role." + Environment.StackTrace);
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
    }
}
