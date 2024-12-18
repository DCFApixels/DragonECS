using DCFApixels.DragonECS.Core;
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
                component = new PoolCache<T>(world.FindOrAutoCreatePool<T>());
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

        internal readonly struct WhereQueryCache<TExecutor, TAspcet> : IEcsWorldComponent<WhereQueryCache<TExecutor, TAspcet>>
            where TExecutor : MaskQueryExecutor, new()
            where TAspcet : EcsAspect, new()
        {
            public readonly TExecutor Executor;
            public readonly TAspcet Aspcet;
            public WhereQueryCache(TExecutor executor, TAspcet aspcet)
            {
                Executor = executor;
                Aspcet = aspcet;
            }
            void IEcsWorldComponent<WhereQueryCache<TExecutor, TAspcet>>.Init(ref WhereQueryCache<TExecutor, TAspcet> component, EcsWorld world)
            {
                TAspcet aspect = world.GetAspect<TAspcet>();
                TExecutor instance = world.GetExecutorForMask<TExecutor>(aspect.Mask);
                instance.Initialize(world, aspect.Mask);
                component = new WhereQueryCache<TExecutor, TAspcet>(instance, aspect);
            }
            void IEcsWorldComponent<WhereQueryCache<TExecutor, TAspcet>>.OnDestroy(ref WhereQueryCache<TExecutor, TAspcet> component, EcsWorld world)
            {
                component = default;
            }
        }
    }
}
