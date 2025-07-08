using Verse;

namespace GhoulWorkAble
{

    static class GearAdjustPatch
    {
        static void Postfix(ref bool __result, Pawn pawn, bool ignoreGender)
        {
            Verse.Log.Message("try find pawn wear");
            if (__result && pawn.IsColonySubhumanPlayerControlled)
            {
                __result = false;
            }
        }
    }
}
