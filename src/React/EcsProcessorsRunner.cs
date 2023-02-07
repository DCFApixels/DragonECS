using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public interface IEcsProcessorsRunner
    {
        public EcsSession Source { get; }
        public void Run();
    }
    public class EcsProcessorsRunner<TDoTag> : IEcsProcessorsRunner
        where TDoTag : IEcsDoTag
    {
        private EcsSession _source;
        private IEcsDo<TDoTag>[] _targets;

        public EcsSession Source => _source;
        public IReadOnlyList<IEcsDo<TDoTag>> Systems => _targets;

        internal EcsProcessorsRunner(EcsSession source)
        {
            _source = source;
            List<IEcsDo<TDoTag>> list = new List<IEcsDo<TDoTag>>();

            foreach (var item in _source.AllProcessors)
            {
                if (item is IEcsDo<TDoTag> targetItem)
                {
                    list.Add(targetItem);
                }
            }
            _targets = list.ToArray();
        }

        public void Run()
        {
            foreach (var item in _targets)
            {
                item.Do(_source);
            }
        }
    }
}
