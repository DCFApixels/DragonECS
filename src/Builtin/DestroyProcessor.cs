namespace DCFApixels.DragonECS
{
    public class DestroyProcessor : IEcsDo<_Run>
    {
        void IEcsDo<_Run>.Do(EcsSession session)
        {

        }
    }

    public class DestroyedTable : EcsTable
    {
        public readonly EcsPool<tag> isDestroyed;

        public static mem<tag> isDestroyedMem = "isDestroyed";

        public DestroyedTable(ref TableBuilder builder) : base(ref builder)
        {
            isDestroyed = builder.Inc(isDestroyedMem);
        }
    }
}
