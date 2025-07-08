using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.Code;

namespace GhoulWorkAble.HarmonyPatches
{
    //Dangereous to modify this.
    [StaticConstructorOnStartup]
    static class PawnColonistPatch
    {
        static PawnColonistPatch() {
            var harmony = new Harmony(typeof(PawnColonistPatch).FullName);
            harmony.Patch(original: AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsColonist)),
            transpiler: new HarmonyMethod(typeof(PawnColonistPatch), nameof(PawnColonist_Transpiler)));
        }
        static IEnumerable<CodeInstruction> PawnColonist_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            //IL_0037: ldarg.0
            //IL_0038: call instance bool Verse.Pawn::get_IsSubhuman()
            //IL_003D: ldc.i4.0
            //IL_003E: ceq
            //IL_0040: ret
            var found = false;
            var targetMethod = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.IsSubhuman));
            CodeInstruction prevInstruction = null;
            if (targetMethod == null)
            {
                Log.Message("GhoulWorkAble failed to pawn colonist." + Environment.StackTrace);
            }
            foreach (var instruction in instructions)
            {
                if (targetMethod != null
                    && instruction.Calls(targetMethod))
                {
                    // return !(IsSubHuman && !IsColonySubhumanPlayerControlled)
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
                Log.Message("GhoulWorkAble failed to pawn colonist." + Environment.StackTrace);
        }

    }
}
