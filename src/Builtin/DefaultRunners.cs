namespace DCFApixels.DragonECS
{
    public interface IEcsPreInitSystem : IEcsSystem
    {
        public void PreInit(EcsSystems systems);
    }
    public interface IEcsInitSystem : IEcsSystem
    {
        public void Init(EcsSystems systems);
    }
    public interface IEcsRunSystem : IEcsSystem
    {
        public void Run(EcsSystems systems);
    }
    public interface IEcsDestroySystem : IEcsSystem
    {
        public void Destroy(EcsSystems systems);
    }

    public interface IEcsBaseSystem : IEcsInitSystem, IEcsRunSystem, IEcsDestroySystem { }

    public sealed class EcsPreInitRunner : EcsRunner<IEcsPreInitSystem>, IEcsPreInitSystem
    {
        void IEcsPreInitSystem.PreInit(EcsSystems systems)
        {
            foreach (var item in targets) item.PreInit(systems);
        }
    }
    public sealed class EcsInitRunner : EcsRunner<IEcsInitSystem>, IEcsInitSystem
    {
        void IEcsInitSystem.Init(EcsSystems systems)
        {
            foreach (var item in targets) item.Init(systems);
        }
    }
    public sealed class EcsRunRunner : EcsRunner<IEcsRunSystem>, IEcsRunSystem
    {
        void IEcsRunSystem.Run(EcsSystems systems)
        {
            foreach (var item in targets) item.Run(systems);
        }
    }
    public sealed class EcsDestroyRunner : EcsRunner<IEcsDestroySystem>, IEcsDestroySystem
    {
        void IEcsDestroySystem.Destroy(EcsSystems systems)
        {
            foreach (var item in targets) item.Destroy(systems);
        }
    }
}
