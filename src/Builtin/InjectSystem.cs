namespace DCFApixels.DragonECS
{
    public interface IEcsInject<T> : IEcsSystem
    {
        public void Inject(T obj);
    }
    [DebugColor(DebugColor.Gray)]
    public sealed class InjectRunner<T> : EcsRunner<IEcsInject<T>>, IEcsInject<T>
    {
        void IEcsInject<T>.Inject(T obj)
        {
            foreach (var item in targets)
            {
                item.Inject(obj);
            }
        }
    }

    [DebugColor(DebugColor.Gray)]
    public class InjectSystem<T> : IEcsPreInitSystem
    {
        private T _injectedData;

        public InjectSystem(T injectedData)
        {
            _injectedData = injectedData;
        }

        public void PreInit(EcsSystems systems)
        {
            var injector = systems.GetRunner<IEcsInject<T>>();
            injector.Inject(_injectedData);
        }
    }

    public static class InjectSystemExstensions
    {
        public static EcsSystems.Builder Inject<T>(this EcsSystems.Builder self, T data)
        {
            self.Add(new InjectSystem<T>(data));
            return self;
        }
        public static EcsSystems.Builder Inject<A, B>(this EcsSystems.Builder self, A a, B b)
        {
            self.Inject(a).Inject(b);
            return self;
        }
        public static EcsSystems.Builder Inject<A, B, C, D>(this EcsSystems.Builder self, A a, B b, C c, D d)
        {
            self.Inject(a).Inject(b).Inject(c).Inject(d);
            return self;
        }
        public static EcsSystems.Builder Inject<A, B, C, D, E>(this EcsSystems.Builder self, A a, B b, C c, D d, E e)
        {
            self.Inject(a).Inject(b).Inject(c).Inject(d).Inject(e);
            return self;
        }
        public static EcsSystems.Builder Inject<A, B, C, D, E, F>(this EcsSystems.Builder self, A a, B b, C c, D d, E e, F f)
        {
            self.Inject(a).Inject(b).Inject(c).Inject(d).Inject(e).Inject(f);
            return self;
        }
    }
}
