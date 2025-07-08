using Verse;

namespace GhoulWorkAble
{
    internal class GhoulWorkAbleUtil
    {
        public static bool IsGhoul(Pawn pawn)
        {
            return pawn.IsGhoul && pawn.IsColonySubhumanPlayerControlled;
            return false;
        }
    }
}
