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
}