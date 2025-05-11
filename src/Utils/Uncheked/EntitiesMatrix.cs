#if DISABLE_DEBUG
#undef DEBUG
#endif

namespace DCFApixels.DragonECS.UncheckedCore
{
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