using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public interface IEcsSystemsRunner
    {
        public EcsSession Source { get; }
        public void Run();
    }
    public class EcsSystemsRunner<TDoTag> : IEcsSystemsRunner
        where TDoTag : IEcsDoTag
    {
        private EcsSession _source;
        private IEcsDo<TDoTag>[] _systems;

        public EcsSession Source => _source;
        public IReadOnlyList<IEcsDo<TDoTag>> Systems => _systems;

        internal EcsSystemsRunner(EcsSession source)
        {
            _source = source;
            List<IEcsDo<TDoTag>> list = new List<IEcsDo<TDoTag>>();

            foreach (var item in _source.AllSystems)
            {
                if (item is IEcsDo<TDoTag> targetItem)
                {
                    list.Add(targetItem);
                }
            }
            _systems = list.ToArray();
        }

        public void Run()
        {
            foreach (var item in _systems)
            {
                item.Do(_source);
            }
        }
    }
}
