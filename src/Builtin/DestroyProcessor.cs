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
        private EcsIncTag _destroyedTag;
    }
}
