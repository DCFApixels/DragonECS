namespace DCFApixels.DragonECS
{
    public class FitersProcessor : IEcsGReceive<_OnComponentAdded>, IEcsGReceive<_OnComponentRemoved>
    {
        void IEcsGReceive<_OnComponentAdded>.Do<T>(EcsSession session, in _OnComponentAdded message, in T obj)
        {

        }

        void IEcsGReceive<_OnComponentRemoved>.Do<T>(EcsSession session, in _OnComponentRemoved message, in T obj)
        {

        }
    }
}
