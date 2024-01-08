namespace DCFApixels.DragonECS
{
    public abstract partial class EcsWorld
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
        internal readonly struct AspectCache<T> : IEcsWorldComponent<AspectCache<T>>
            where T : EcsAspect
        {
            public readonly T instance;
            public AspectCache(T instance) => this.instance = instance;
            void IEcsWorldComponent<AspectCache<T>>.Init(ref AspectCache<T> component, EcsWorld world)
            {
                component = new AspectCache<T>(EcsAspect.Builder.New<T>(world));
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
