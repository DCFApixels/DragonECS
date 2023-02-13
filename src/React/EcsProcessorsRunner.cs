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
        private readonly EcsSession _source;
        private readonly IDo<TDoTag>[] _targets;

        public EcsSession Source => _source;
        public IReadOnlyList<IDo<TDoTag>> Targets => _targets;

        internal EcsProcessorsRunner(EcsSession source)
        {
            _source = source;
            List<IDo<TDoTag>> list = new List<IDo<TDoTag>>();

            foreach (var item in _source.AllProcessors)
            {
                if (item is IDo<TDoTag> targetItem)
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

        public void Destroy() => _source.OnRunnerDetroyed(this);
    }
}
