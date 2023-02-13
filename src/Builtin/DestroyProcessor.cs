namespace DCFApixels.DragonECS
{
    public class DestroyProcessor : IDo<_Run>
    {
        void IDo<_Run>.Do(EcsSession session)
        {

        }
    }

    public struct DestroyedTag { }
}
