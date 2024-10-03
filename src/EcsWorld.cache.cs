using DCFApixels.DragonECS.PoolsCore;

namespace DCFApixels.DragonECS
{
    public partial class EcsWorld
    {
        internal readonly struct PoolCache<T> : IEcsWorldComponent<PoolCache<T>>
            where T : IEcsPoolImplementation, new()
        {
            public readonly T Instance;
            public PoolCache(T instance) { Instance = instance; }
            void IEcsWorldComponent<PoolCache<T>>.Init(ref PoolCache<T> component, EcsWorld world)
            {
                component = new PoolCache<T>(world.CreatePool<T>());
            }
            void IEcsWorldComponent<PoolCache<T>>.OnDestroy(ref PoolCache<T> component, EcsWorld world)
            {
                component = default;
            }
        }
        internal readonly struct AspectCache<T> : IEcsWorldComponent<AspectCache<T>>
            where T : EcsAspect, new()
        {
            public readonly T Instance;
            public AspectCache(T instance) { Instance = instance; }
            void IEcsWorldComponent<AspectCache<T>>.Init(ref AspectCache<T> component, EcsWorld world)
            {
                component = new AspectCache<T>(EcsAspect.Builder.New<T>(world));
            }
            void IEcsWorldComponent<AspectCache<T>>.OnDestroy(ref AspectCache<T> component, EcsWorld world)
            {
                component = default;
            }
        }
        internal readonly struct QueryCache<TExecutor, TAspcet> : IEcsWorldComponent<QueryCache<TExecutor, TAspcet>>
            where TExecutor : EcsQueryExecutor, new()
            where TAspcet : EcsAspect, new()
        {
            public readonly TExecutor Executor;
            public readonly TAspcet Aspcet;
            public QueryCache(TExecutor executor, TAspcet aspcet)
            {
                Executor = executor;
                Aspcet = aspcet;
            }
            void IEcsWorldComponent<QueryCache<TExecutor, TAspcet>>.Init(ref QueryCache<TExecutor, TAspcet> component, EcsWorld world)
            {
                TExecutor instance = new TExecutor();
                TAspcet aspect = world.GetAspect<TAspcet>();
                instance.Initialize(world, aspect.Mask);
                component = new QueryCache<TExecutor, TAspcet>(instance, aspect);
            }
            void IEcsWorldComponent<QueryCache<TExecutor, TAspcet>>.OnDestroy(ref QueryCache<TExecutor, TAspcet> component, EcsWorld world)
            {
                component = default;
            }
        }
    }
}
