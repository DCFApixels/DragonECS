using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.UncheckedCore
{
    public static class UncheckedCoreUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsSpan CreateSpan(short worldID, int[] entitesArray, int startIndex, int length)
        {
            return new EcsSpan(worldID, entitesArray, startIndex, length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsSpan CreateSpan(short worldID, int[] entitesArray, int length)
        {
            return new EcsSpan(worldID, entitesArray, length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsSpan CreateSpan(short worldID, int[] entitesArray)
        {
            return new EcsSpan(worldID, entitesArray);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsSpan CreateEmptySpan(short worldID)
        {
            return new EcsSpan(worldID, Array.Empty<int>());
        }
        public static bool CheckSpanValideDebug(EcsSpan span)
        {
            HashSet<int> set = new HashSet<int>(span.Count);
            foreach (var e in span)
            {
                if (set.Add(e) == false)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
