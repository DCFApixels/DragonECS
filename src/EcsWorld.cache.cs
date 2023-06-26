using DCFApixels.DragonECS.Utils;
using System;
using System.Reflection;

namespace DCFApixels.DragonECS
{
    public partial class EcsWorld
    {
        internal readonly struct PoolCache<T> : IEcsWorldComponent<PoolCache<T>>
            where T : IEcsPoolImplementation, new()
        {
            public readonly T instance;
            public PoolCache(T instance) => this.instance = instance;
            void IEcsWorldComponent<PoolCache<T>>.Init(ref PoolCache<T> component, EcsWorld world)
            {
                component = new PoolCache<T>(world.CreatePool<T>());
            }
            void IEcsWorldComponent<PoolCache<T>>.OnDestroy(ref PoolCache<T> component, EcsWorld world)
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
        internal readonly struct AspectCache<T> : IEcsWorldComponent<AspectCache<T>>
            where T : EcsAspect
        {
            public readonly T instance;
            public AspectCache(T instance) => this.instance = instance;
            void IEcsWorldComponent<AspectCache<T>>.Init(ref AspectCache<T> component, EcsWorld world)
            {
                component = new AspectCache<T>(EcsAspect.Builder.Build<T>(world));
            }
            void IEcsWorldComponent<AspectCache<T>>.OnDestroy(ref AspectCache<T> component, EcsWorld world)
            {
                component = default;
            }
        }
        internal readonly struct ExcecutorCache<T> : IEcsWorldComponent<ExcecutorCache<T>>
            where T : EcsQueryExecutor, new()
        {
            public readonly T instance;
            public ExcecutorCache(T instance) => this.instance = instance;
            void IEcsWorldComponent<ExcecutorCache<T>>.Init(ref ExcecutorCache<T> component, EcsWorld world)
            {
                T instance = new T();
                instance.Initialize(world);
                component = new ExcecutorCache<T>(instance);
            }
            void IEcsWorldComponent<ExcecutorCache<T>>.OnDestroy(ref ExcecutorCache<T> component, EcsWorld world)
            {
                component = default;
            }
        }
    }
}
