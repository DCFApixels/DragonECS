using DCFApixels.DragonECS.Utils;
using System;

namespace DCFApixels.DragonECS
{
    public partial class EcsWorld
    {
        internal readonly struct PoolCache<TPool> : IEcsWorldComponent<PoolCache<TPool>>
            where TPool : IEcsPoolImplementation, new()
        {
            public readonly TPool instance;
            public PoolCache(TPool instance) => this.instance = instance;
            void IEcsWorldComponent<PoolCache<TPool>>.Init(ref PoolCache<TPool> component, EcsWorld world)
            {
                component = new PoolCache<TPool>(world.CreatePool<TPool>());
            }
            void IEcsWorldComponent<PoolCache<TPool>>.OnDestroy(ref PoolCache<TPool> component, EcsWorld world)
            {
                component = default;
            }
        }
        private TPool CreatePool<TPool>() where TPool : IEcsPoolImplementation, new()
        {
            int index = WorldMetaStorage.GetPoolID<TPool>(_worldTypeID);
            if (index >= _pools.Length)
            {
                int oldCapacity = _pools.Length;
                Array.Resize(ref _pools, _pools.Length << 1);
                ArrayUtility.Fill(_pools, _nullPool, oldCapacity, oldCapacity - _pools.Length);
            }
            if (_pools[index] == _nullPool)
            {
                var pool = new TPool();
                _pools[index] = pool;
                pool.OnInit(this, index);
            }
            return (TPool)_pools[index];
        }
    }
}
