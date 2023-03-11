using System.Collections.Generic;
using System;

namespace DCFApixels.DragonECS
{
    public interface IEcsProcessorsMessenger
    {
        public EcsSession Source { get; }
    }
    public interface IEcsProcessorsMessenger<TMessage> : IEcsProcessorsMessenger where TMessage : IEcsMessage { }
    public class EcsProcessorsMessenger<TMessage> : IEcsProcessorsMessenger<TMessage>, IDisposable
       where TMessage : IEcsMessage
    {
        private readonly EcsSession _source;
        private readonly IReceive<TMessage>[] _targets;

        public EcsSession Source => _source;
        public IReadOnlyList<IReceive<TMessage>> Targets => _targets;

        internal EcsProcessorsMessenger(EcsSession source)
        {
            _source = source;
            List<IReceive<TMessage>> list = new List<IReceive<TMessage>>();

            foreach (var item in _source.AllProcessors)
            {
                if (item is IReceive<TMessage> targetItem)
                {
                    list.Add(targetItem);
                }
            }
            _targets = list.ToArray();
        }

        public void Send(TMessage message)
        {
            Send(in message);
        }
        public void Send(in TMessage message)
        {
            foreach (var item in _targets)
            {
                item.Do(_source, in message);
            }
        }

        public void Destroy() => _source.OnMessengerDetroyed(this);
        void IDisposable.Dispose() => Destroy();
    }
}
