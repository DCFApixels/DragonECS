namespace DCFApixels.DragonECS
{
    public interface IEcsPreInitSystem : IEcsSystem
    {
        public void PreInit(EcsPipeline pipeline);
    }
    public interface IEcsInitSystem : IEcsSystem
    {
        public void Init(EcsPipeline pipeline);
    }
    public interface IEcsRunSystem : IEcsSystem
    {
        public void Run(EcsPipeline pipeline);
    }
    public interface IEcsDestroySystem : IEcsSystem
    {
        public void Destroy(EcsPipeline pipeline);
    }

    public interface IEcsBaseSystem : IEcsInitSystem, IEcsRunSystem, IEcsDestroySystem { }

    public sealed class EcsPreInitRunner : EcsRunner<IEcsPreInitSystem>, IEcsPreInitSystem
    {
#if DEBUG
        private int[] _targetIds;
#endif
        public void PreInit(EcsPipeline pipeline)
        {
#if DEBUG
            for (int i = 0; i < targets.Length; i++)
            {
                int id = _targetIds[i];
                EcsDebug.ProfileMarkBegin(id);
                targets[i].PreInit(pipeline);
                EcsDebug.ProfileMarkEnd(id);
            }
#else
            foreach (var item in targets) item.PreInit(pipeline);
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
        public void Init(EcsPipeline pipeline)
        {
#if DEBUG
            for (int i = 0; i < targets.Length; i++)
            {
                int id = _targetIds[i];
                EcsDebug.ProfileMarkBegin(id);
                targets[i].Init(pipeline);
                EcsDebug.ProfileMarkEnd(id);
            }
#else
            foreach (var item in targets) item.Init(pipeline);
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
        public void Run(EcsPipeline pipeline)
        {
#if DEBUG
            for (int i = 0; i < targets.Length; i++)
            {
                int id = _targetIds[i];
                EcsDebug.ProfileMarkBegin(id);
                targets[i].Run(pipeline);
                EcsDebug.ProfileMarkEnd(id);
            }
#else
            foreach (var item in targets) item.Run(pipeline);
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
        public void Destroy(EcsPipeline pipeline)
        {
#if DEBUG
            for (int i = 0; i < targets.Length; i++)
            {
                int id = _targetIds[i];
                EcsDebug.ProfileMarkBegin(id);
                targets[i].Destroy(pipeline);
                EcsDebug.ProfileMarkEnd(id);
            }
#else
            foreach (var item in targets) item.Destroy(pipeline);
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
