namespace DCFApixels.DragonECS
{
    public abstract class EcsQueryExecutor
    {
        internal void Destroy() => OnDestroy();
        protected abstract void OnDestroy();
    }
}
