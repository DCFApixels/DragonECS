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
#if DEBUG && !DISABLE_DEBUG
        private EcsProfilerMarker[] _markers;
#endif
        public void PreInit(EcsPipeline pipeline)
        {
#if DEBUG && !DISABLE_DEBUG
            for (int i = 0; i < targets.Length; i++)
            {
                using (_markers[i].Auto())
                    targets[i].PreInit(pipeline);
            }
#else
            foreach (var item in targets) item.PreInit(pipeline);
#endif
        }

#if DEBUG && !DISABLE_DEBUG
        protected override void OnSetup()
        {
            _markers = new EcsProfilerMarker[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                _markers[i] = new EcsProfilerMarker(EcsDebug.RegisterMark($"EcsRunner.{targets[i].GetType().Name}.{nameof(PreInit)}"));
            }
        }
#endif
    }
    public sealed class EcsInitRunner : EcsRunner<IEcsInitSystem>, IEcsInitSystem
    {
#if DEBUG && !DISABLE_DEBUG
        private EcsProfilerMarker[] _markers;
#endif
        public void Init(EcsPipeline pipeline)
        {
#if DEBUG && !DISABLE_DEBUG
            for (int i = 0; i < targets.Length; i++)
            {
                using (_markers[i].Auto())
                    targets[i].Init(pipeline);
            }
#else
            foreach (var item in targets) item.Init(pipeline);
#endif
        }

#if DEBUG && !DISABLE_DEBUG
        protected override void OnSetup()
        {
            _markers = new EcsProfilerMarker[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                _markers[i] = new EcsProfilerMarker(EcsDebug.RegisterMark($"EcsRunner.{targets[i].GetType().Name}.{nameof(Init)}"));
            }
        }
#endif
    }
    public sealed class EcsRunRunner : EcsRunner<IEcsRunSystem>, IEcsRunSystem
    {
#if DEBUG && !DISABLE_DEBUG
        private EcsProfilerMarker[] _markers;
#endif
        public void Run(EcsPipeline pipeline)
        {
#if DEBUG && !DISABLE_DEBUG
            for (int i = 0; i < targets.Length; i++)
            {
                using (_markers[i].Auto())
                    targets[i].Run(pipeline);

            }
#else
            foreach (var item in targets) item.Run(pipeline);
#endif
        }

#if DEBUG && !DISABLE_DEBUG
        protected override void OnSetup()
        {
            _markers = new EcsProfilerMarker[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                _markers[i] = new EcsProfilerMarker(EcsDebug.RegisterMark($"EcsRunner.{targets[i].GetType().Name}.{nameof(Run)}"));
            }
        }
#endif
    }
    public sealed class EcsDestroyRunner : EcsRunner<IEcsDestroySystem>, IEcsDestroySystem
    {
#if DEBUG && !DISABLE_DEBUG
        private EcsProfilerMarker[] _markers;
#endif
        public void Destroy(EcsPipeline pipeline)
        {
#if DEBUG && !DISABLE_DEBUG
            for (int i = 0; i < targets.Length; i++)
            {
                using (_markers[i].Auto())
                    targets[i].Destroy(pipeline);
            }
#else
            foreach (var item in targets) item.Destroy(pipeline);
#endif
        }

#if DEBUG && !DISABLE_DEBUG
        protected override void OnSetup()
        {
            _markers = new EcsProfilerMarker[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                _markers[i] = new EcsProfilerMarker(EcsDebug.RegisterMark($"EcsRunner.{targets[i].GetType().Name}.{nameof(Destroy)}"));
            }
        }
#endif
    }
}
