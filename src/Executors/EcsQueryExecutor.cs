using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public abstract class EcsQueryExecutor
    {
        private EcsWorld _source;
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.id; }
        }
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source; }
        }
        public abstract long Version { get; }
        internal void Initialize(EcsWorld world)
        {
            _source = world;
            OnInitialize();
        }
        internal void Destroy()
        {
            OnDestroy();
            _source = null;
        }
        protected abstract void OnInitialize();
        protected abstract void OnDestroy();
    }

    public readonly struct PoolVersionsChecker
    {
        private readonly EcsMask _mask;
        private readonly long[] _versions;

        public PoolVersionsChecker(EcsMask mask) : this()
        {
            _mask = mask;
            _versions = new long[mask._inc.Length + mask._exc.Length];
        }

        public bool NextEquals()
        {
            var slots = _mask.World._poolSlots;
            bool result = true;
            int index = 0;
            foreach (var i in _mask._inc)
            {
                if (slots[i].version != _versions[index++])
                {
                    result = false;
                }
            }
            foreach (var i in _mask._exc)
            {
                if (slots[i].version != _versions[index++])
                {
                    result = false;
                }
            }
            return result;
        }
    }
}