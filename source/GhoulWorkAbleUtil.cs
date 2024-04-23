using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace GhoulWorkAble
{
    internal class GhoulWorkAbleUtil
    {
        public static bool IsGhoul(Pawn pawn)
        {
            return pawn.IsGhoul && pawn.IsColonyMutantPlayerControlled;
            return false;
        }
    }
}
