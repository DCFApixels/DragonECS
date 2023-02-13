using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public class EcsProcessorsGMessenger<TMessage> : IEcsProcessorsMessenger<TMessage>, IDisposable
       where TMessage : IEcsMessage
    {
        private readonly EcsSession _source;
        private readonly IEcsGReceive<TMessage>[] _targets;

        public EcsSession Source => _source;
        public IReadOnlyList<IEcsGReceive<TMessage>> Targets => _targets;

        internal EcsProcessorsGMessenger(EcsSession source)
        {
            _source = source;
            List<IEcsGReceive<TMessage>> list = new List<IEcsGReceive<TMessage>>();

            foreach (var item in _source.AllProcessors)
            {
                if (item is IEcsGReceive<TMessage> targetItem)
                {
                    list.Add(targetItem);
                }
            }
            _targets = list.ToArray();
        }

        public void Send<T>(in TMessage message, in T obj)
        {
            foreach (var item in _targets)
            {
                item.Do(_source, in message, in obj);
            }
        }

        public void Destroy() => _source.OnMessengerDetroyed(this);
        void IDisposable.Dispose() => Destroy();
    }
}
