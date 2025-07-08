using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace GhoulWorkAble.HarmonyPatches
{
    [StaticConstructorOnStartup]
    static class CaravanPatches
    {
        static CaravanPatches()
        {
            //var harmony = new Harmony(typeof(CarryWeightPatch).FullName);
            //harmony.Patch(original: AccessTools.Method(typeof(MassUtility), nameof(MassUtility.CanEverCarryAnything)),
            //transpiler: new HarmonyMethod(typeof(CarryWeightPatch), nameof(CanEverCarryAnything_Transpiler)));
        }
        static bool CanEquip_Search(MethodInfo method)
        {
            return method.IsStatic && method.Name == nameof(EquipmentUtility.CanEquip) && method.GetParameters().Length >= 3;
        }
    }
}
