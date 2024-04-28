namespace DCFApixels.DragonECS
{
    public class EcsPreFilter
    {

    }

    public class EcsPreFilterManager
    {
        private EcsWorld _world;
        private EcsGroup _dirtyEntities;

        public EcsPreFilterManager(EcsWorld world)
        {
            _world = world;
            _dirtyEntities = EcsGroup.New(_world);
        }

        internal void AddDirty(int entityID)
        {
            _dirtyEntities.Add(entityID);
        }
    }
    public class PoolListener : IEcsPoolEventListener
    {
        public void OnAdd(int entityID)
        {
        }
        public void OnDel(int entityID)
        {
        }
        public void OnGet(int entityID)
        {
        }
    }
}
