using DCFApixels.DragonECS.RunnersCore;

namespace DCFApixels.DragonECS
{
    public interface IEcsComponentAdd : IEcsSystem
    {
        public void OnComponentAdd<T>(int entityID);
    }
    public interface IEcsComponentWrite : IEcsSystem
    {
        public void OnComponentWrite<T>(int entityID);
    }
    public interface IEcsComponentDel : IEcsSystem
    {
        public void OnComponentDel<T>(int entityID);
    }
    public interface IEcsComponentLifecycle : IEcsComponentAdd, IEcsComponentWrite, IEcsComponentDel { }
    public sealed class EcsEntityAddComponentRunner : EcsRunner<IEcsComponentAdd>, IEcsComponentAdd
    {
        public void OnComponentAdd<T>(int entityID)
        {
            foreach (var item in targets) item.OnComponentAdd<T>(entityID);
        }
    }
    public sealed class EcsEntityChangeComponentRunner : EcsRunner<IEcsComponentWrite>, IEcsComponentWrite
    {
        public void OnComponentWrite<T>(int entityID)
        {
            foreach (var item in targets) item.OnComponentWrite<T>(entityID);
        }
    }
    public sealed class EcsEntityDelComponentRunner : EcsRunner<IEcsComponentDel>, IEcsComponentDel
    {
        public void OnComponentDel<T>(int entityID)
        {
            foreach (var item in targets) item.OnComponentDel<T>(entityID);
        }
    }


    public interface IEcsEntityCreate : IEcsSystem
    {
        public void OnEntityCreate(EcsEntity entity);
    }
    public interface IEcsEntityDestroy : IEcsSystem
    {
        public void OnEntityDestroy(EcsEntity entity);
    }
    public interface IEcsEntityLifecycle : IEcsEntityCreate, IEcsEntityDestroy { }
    public sealed class EcsEntityCreateRunner : EcsRunner<IEcsEntityCreate>, IEcsEntityCreate
    {
        public void OnEntityCreate(EcsEntity entity)
        {
            foreach (var item in targets) item.OnEntityCreate(entity);
        }
    }
    public sealed class EcsEntityDestroyRunner : EcsRunner<IEcsEntityDestroy>, IEcsEntityDestroy
    {
        public void OnEntityDestroy(EcsEntity entity)
        {
            foreach (var item in targets) item.OnEntityDestroy(entity);
        }
    }


    public interface IEcsWorldCreate : IEcsSystem
    {
        public void OnWorldCreate(EcsWorld world);
    }
    public interface IEcsWorldDestroy : IEcsSystem
    {
        public void OnWorldDestroy(EcsWorld world);
    }
    public interface IEcsWorldLifecycle : IEcsWorldCreate, IEcsWorldDestroy { }
    public sealed class EcsWorldCreateRunner : EcsRunner<IEcsWorldCreate>, IEcsWorldCreate
    {
        public void OnWorldCreate(EcsWorld world)
        {
            foreach (var item in targets) item.OnWorldCreate(world);
        }
    }
    public sealed class EcsWorldDestryRunner : EcsRunner<IEcsWorldDestroy>, IEcsWorldDestroy
    {
        public void OnWorldDestroy(EcsWorld world)
        {
            foreach (var item in targets) item.OnWorldDestroy(world);
        }
    }
}
