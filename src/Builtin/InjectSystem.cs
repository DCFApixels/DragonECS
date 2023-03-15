namespace DCFApixels.DragonECS
{
    public interface IEcsInject<T> : IEcsSystem
    {
        public void Inject(T obj);
    }
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

    public class InjectSystem<T> : IEcsPreInitSystem
    {
        private T _injectedData;

        public InjectSystem(T injectedData)
        {
            _injectedData = injectedData;
        }

        public void PreInit(EcsSession session)
        {
            var injector = session.GetRunner<IEcsInject<T>>();
            injector.Inject(_injectedData);
        }
    }

    public static class InjectSystemExstensions
    {
        public static EcsSession Inject<T>(this EcsSession self, T data)
        {
            self.Add(new InjectSystem<T>(data));
            return self;
        }

        public static EcsSession Inject<A, B>(this EcsSession self, A dataA, B dataB)
        {
            self.Inject(dataA).Inject(dataB);
            return self;
        }

        public static EcsSession Inject<A, B, C, D>(this EcsSession self, A dataA, B dataB, C dataC, D dataD)
        {
            self.Inject(dataA).Inject(dataB).Inject(dataC).Inject(dataD);
            return self;
        }

        public static EcsSession Inject<A, B, C, D, E>(this EcsSession self,
            A dataA, B dataB, C dataC, D dataD, E dataE)
        {
            self.Inject(dataA).Inject(dataB).Inject(dataC).Inject(dataD).Inject(dataE);
            return self;
        }
    }
}
