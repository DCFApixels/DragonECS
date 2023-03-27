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
#if DEBUG
        private int[] _targetIds;
#endif
        public void PreInit(EcsSystems systems)
        {
#if DEBUG
            for (int i = 0; i < targets.Length; i++)
            {
                int id = _targetIds[i];
                EcsDebug.ProfileMarkBegin(id);
                targets[i].PreInit(systems);
                EcsDebug.ProfileMarkEnd(id);
            }
#else
            foreach (var item in targets) item.PreInit(systems);
#endif
        }

#if DEBUG
        protected override void OnSetup()
        {
            _targetIds = new int[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                _targetIds[i] = EcsDebug.RegisterMark($"EcsRunner.{targets[i].GetType().Name}.{nameof(PreInit)}");
            }
        }
#endif
    }
    public sealed class EcsInitRunner : EcsRunner<IEcsInitSystem>, IEcsInitSystem
    {
#if DEBUG
        private int[] _targetIds;
#endif
        public void Init(EcsSystems systems)
        {
#if DEBUG
            for (int i = 0; i < targets.Length; i++)
            {
                int id = _targetIds[i];
                EcsDebug.ProfileMarkBegin(id);
                targets[i].Init(systems);
                EcsDebug.ProfileMarkEnd(id);
            }
#else
            foreach (var item in targets) item.Init(systems);
#endif
        }

#if DEBUG
        protected override void OnSetup()
        {
            _targetIds = new int[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                _targetIds[i] = EcsDebug.RegisterMark($"EcsRunner.{targets[i].GetType().Name}.{nameof(Init)}");
            }
        }
#endif
    }
    public sealed class EcsRunRunner : EcsRunner<IEcsRunSystem>, IEcsRunSystem
    {
#if DEBUG
        private int[] _targetIds;
#endif
        public void Run(EcsSystems systems)
        {
#if DEBUG
            for (int i = 0; i < targets.Length; i++)
            {
                int id = _targetIds[i];
                EcsDebug.ProfileMarkBegin(id);
                targets[i].Run(systems);
                EcsDebug.ProfileMarkEnd(id);
            }
#else
            foreach (var item in targets) item.Run(systems);
#endif
        }

#if DEBUG
        protected override void OnSetup()
        {
            _targetIds = new int[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                _targetIds[i] = EcsDebug.RegisterMark($"EcsRunner.{targets[i].GetType().Name}.{nameof(Run)}");
            }
        }
#endif
    }
    public sealed class EcsDestroyRunner : EcsRunner<IEcsDestroySystem>, IEcsDestroySystem
    {
#if DEBUG
        private int[] _targetIds;
#endif
        public void Destroy(EcsSystems systems)
        {
#if DEBUG
            for (int i = 0; i < targets.Length; i++)
            {
                int id = _targetIds[i];
                EcsDebug.ProfileMarkBegin(id);
                targets[i].Destroy(systems);
                EcsDebug.ProfileMarkEnd(id);
            }
#else
            foreach (var item in targets) item.Destroy(systems);
#endif
        }

#if DEBUG
        protected override void OnSetup()
        {
            _targetIds = new int[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                _targetIds[i] = EcsDebug.RegisterMark($"EcsRunner.{targets[i].GetType().Name}.{nameof(Destroy)}");
            }
        }
#endif
    }
}
