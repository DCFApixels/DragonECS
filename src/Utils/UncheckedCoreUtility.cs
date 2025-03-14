#if DISABLE_DEBUG
#undef DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.UncheckedCore
{
    public static class UncheckedCoreUtility
    {
        #region CreateEntLong
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static entlong CreateEntLong(int entityID, short gen, short worldID)
        {
            return new entlong(entityID, gen, worldID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static entlong CreateEntLong(long entityGenWorld)
        {
            return new entlong(entityGenWorld);
        }
        #endregion

        #region CreateSpan
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsSpan CreateSpan(short worldID, ReadOnlySpan<int> entitesArray)
        {
            return new EcsSpan(worldID, entitesArray);
        }
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
            HashSet<int> set = new HashSet<int>();
            foreach (var e in span)
            {
                if (set.Add(e) == false)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region EcsGroup
        public static EcsGroup GetSourceInstance(EcsReadonlyGroup group)
        {
            return group.GetSource_Internal();
        }
        #endregion
    }

    public readonly struct EntitiesMatrix
    {
        private readonly EcsWorld _world;
        public EntitiesMatrix(EcsWorld world)
        {
            _world = world;
        }
        public int PoolsCount
        {
            get { return _world.PoolsCount; }
        }
        public int EntitesCount
        {
            get { return _world.Capacity; }
        }
        public int GetEntityComponentsCount(int entityID)
        {
            return _world.GetComponentsCount(entityID);
        }
        public int GetEntityGen(int entityID)
        {
            return _world.GetGen(entityID);
        }
        public bool IsEntityUsed(int entityID)
        {
            return _world.IsUsed(entityID);
        }
        public bool this[int entityID, int poolID]
        {
            get
            {
                int entityStartChunkIndex = entityID << _world._entityComponentMaskLengthBitShift;
                var chunkInfo = EcsMaskChunck.FromID(poolID);
                return (_world._entityComponentMasks[entityStartChunkIndex + chunkInfo.chunkIndex] & chunkInfo.mask) != 0;
            }
        }
    }
}