using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static RimWorld.PsychicRitualRoleDef;

namespace GhoulWorkAble
{

    static class GearAdjustPatch
    {
        static void Postfix(ref bool __result, Pawn pawn, bool ignoreGender){
            Verse.Log.Message("try find pawn wear");
            if (__result && pawn.IsColonyMutantPlayerControlled)
            {
                __result = false;
            }
        }
    }
}
