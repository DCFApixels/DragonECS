#if DISABLE_DEBUG
#undef DEBUG
#endif
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
            where T : new()
        {
            public readonly T Instance;
            public readonly EcsMask Mask;
            public AspectCache(T instance, EcsMask mask)
            {
                Instance = instance;
                Mask = mask;
            }
            void IEcsWorldComponent<AspectCache<T>>.Init(ref AspectCache<T> component, EcsWorld world)
            {
#if DEBUG
                AllowedInWorldsAttribute.CheckAllows<T>(world);
#endif
                var result = EcsAspect.Builder.New<T>(world);
                component = new AspectCache<T>(result.aspect, result.mask);
            }
            void IEcsWorldComponent<AspectCache<T>>.OnDestroy(ref AspectCache<T> component, EcsWorld world)
            {
                component = default;
            }
        }

        internal readonly struct WhereQueryCache<TExecutor, TAspcet> : IEcsWorldComponent<WhereQueryCache<TExecutor, TAspcet>>
            where TExecutor : MaskQueryExecutor, new()
            where TAspcet : new()
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
                TAspcet aspect = world.GetAspect<TAspcet>(out EcsMask mask);
                TExecutor instance = world.GetExecutorForMask<TExecutor>(mask);
                instance.Initialize(world, mask);
                component = new WhereQueryCache<TExecutor, TAspcet>(instance, aspect);
            }
            void IEcsWorldComponent<WhereQueryCache<TExecutor, TAspcet>>.OnDestroy(ref WhereQueryCache<TExecutor, TAspcet> component, EcsWorld world)
            {
                component = default;
            }
        }
    }
}
