namespace DCFApixels.DragonECS
{
    public abstract class EcsQueryExecutor
    {
        private EcsWorld _world;
        public EcsWorld World => _world;
        internal void Initialize(EcsWorld world)
        {
            _world = world;
            OnInitialize();
        }
        internal void Destroy() => OnDestroy();
        protected abstract void OnInitialize();
        protected abstract void OnDestroy();
    }
}
