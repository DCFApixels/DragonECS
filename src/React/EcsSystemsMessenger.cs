using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public interface IEcsSystemsMessenger
    {
        public EcsSession Source { get; }
    }
    public class EcsSystemsMessenger<TMessage> : IEcsSystemsMessenger
       where TMessage : IEcsMessage
    {
        private EcsSession _source;
        private IEcsDoMessege<TMessage>[] _systems;

        public EcsSession Source => _source;
        public IReadOnlyList<IEcsDoMessege<TMessage>> Systems => _systems;

        internal EcsSystemsMessenger(EcsSession source)
        {
            _source = source;
            List<IEcsDoMessege<TMessage>> list = new List<IEcsDoMessege<TMessage>>();

            foreach (var item in _source.AllSystems)
            {
                if (item is IEcsDoMessege<TMessage> targetItem)
                {
                    list.Add(targetItem);
                }
            }
            _systems = list.ToArray();
        }

        public void Send(in TMessage message)
        {
            foreach (var item in _systems)
            {
                item.Do(_source, message);
            }
        }
    }
}
