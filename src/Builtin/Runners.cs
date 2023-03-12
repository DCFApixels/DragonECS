namespace DCFApixels.DragonECS
{
    public interface IEcsPreInitSystem : IEcsSystem
    {
        public void PreInit(EcsSession session);
    }
    public interface IEcsInitSystem : IEcsSystem
    {
        public void Init(EcsSession session);
    }
    public interface IEcsRunSystem : IEcsSystem
    {
        public void Run(EcsSession session);
    }
    public interface IEcsDestroySystem : IEcsSystem
    {
        public void Destroy(EcsSession session);
    }

    public interface IEcsSimpleCycleSystem :
        IEcsInitSystem,
        IEcsRunSystem,
        IEcsDestroySystem
    { }

    public sealed class EcsPreInitRunner : EcsRunner<IEcsPreInitSystem>, IEcsPreInitSystem
    {
        void IEcsPreInitSystem.PreInit(EcsSession session)
        {
            foreach (var item in targets) item.PreInit(session);
        }
    }
    public sealed class EcsInitRunner : EcsRunner<IEcsInitSystem>, IEcsInitSystem
    {
        void IEcsInitSystem.Init(EcsSession session)
        {
            foreach (var item in targets) item.Init(session);
        }
    }
    public sealed class EcsRunRunner : EcsRunner<IEcsRunSystem>, IEcsRunSystem
    {
        void IEcsRunSystem.Run(EcsSession session)
        {
            foreach (var item in targets) item.Run(session);
        }
    }
    public sealed class EcsDestroyRunner : EcsRunner<IEcsDestroySystem>, IEcsDestroySystem
    {
        void IEcsDestroySystem.Destroy(EcsSession session)
        {
            foreach (var item in targets) item.Destroy(session);
        }
    }
}
