using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public interface IEcsInject<T>
    {
        public void Inject(T obj);
    }
    public class InjectProcessor<T> : IDo<_PreInit>
    {

        private T _injectedData;

        public InjectProcessor(T injectedData)
        {
            _injectedData = injectedData;
        }

        void IDo<_PreInit>.Do(EcsSession session)
        {
            var messenger = session.GetMessenger<_OnInject<T>>();
            messenger.Send(new _OnInject<T>(_injectedData));
            messenger.Destroy();
        }
    }

    public static class InjectProcessorExstensions
    {
        public static EcsSession Inject<T>(this EcsSession self, T data)
        {
            self.Add(new InjectProcessor<T>(data));
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
