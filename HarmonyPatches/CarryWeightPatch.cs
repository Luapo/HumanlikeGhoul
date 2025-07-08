using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static RimWorld.PsychicRitualRoleDef;

namespace GhoulWorkAble.HarmonyPatches
{
    [StaticConstructorOnStartup]
    static class CarryWeightPatch
    {
        static CarryWeightPatch()
        {
            var harmony = new Harmony(typeof(CarryWeightPatch).FullName);
            harmony.Patch(original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.CanEverCarryAnything)),
            transpiler: new HarmonyMethod(typeof(CarryWeightPatch), nameof(CanEverCarryAnything_Transpiler)));
        }
        private static IEnumerable<CodeInstruction> CanEverCarryAnything_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            MethodInfo targetMethod = AccessTools.PropertyGetter(typeof(Pawn), "IsSubhuman");
            if (targetMethod == null)
            {
                Log.Message("Failed to reflect carry weight." + Environment.StackTrace);
            }
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (targetMethod != null && !found && instruction.Calls(targetMethod))
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
