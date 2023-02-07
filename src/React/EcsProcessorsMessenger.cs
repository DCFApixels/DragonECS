using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public interface IEcsProcessorsMessenger
    {
        public EcsSession Source { get; }
    }
    public class EcsProcessorsMessenger<TMessage> : IEcsProcessorsMessenger
       where TMessage : IEcsMessage
    {
        private EcsSession _source;
        private IEcsDoMessege<TMessage>[] _targets;

        public EcsSession Source => _source;
        public IReadOnlyList<IEcsDoMessege<TMessage>> Systems => _targets;

        internal EcsProcessorsMessenger(EcsSession source)
        {
            _source = source;
            List<IEcsDoMessege<TMessage>> list = new List<IEcsDoMessege<TMessage>>();

            foreach (var item in _source.AllProcessors)
            {
                if (item is IEcsDoMessege<TMessage> targetItem)
                {
                    list.Add(targetItem);
                }
            }
            _targets = list.ToArray();
        }

        public void Send(in TMessage message)
        {
            foreach (var item in _targets)
            {
                item.Do(_source, message);
            }
        }
    }
}
