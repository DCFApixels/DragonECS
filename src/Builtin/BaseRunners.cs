﻿using DCFApixels.DragonECS.RunnersCore;

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

    namespace Internal
    {
        [DebugColor(DebugColor.Orange)]
        public sealed class EcsPreInitRunner : EcsRunner<IEcsPreInitSystem>, IEcsPreInitSystem
        {
#if DEBUG && !DISABLE_DEBUG
            private EcsProfilerMarker[] _markers;
#endif
            public void PreInit(EcsPipeline pipeline)
            {
#if DEBUG && !DISABLE_DEBUG
                for (int i = 0; i < Targets.Length && Targets.Length <= _markers.Length; i++)
                {
                    using (_markers[i].Auto())
                        Targets[i].PreInit(pipeline);
                }
#else
            foreach (var item in targets) item.PreInit(pipeline);
#endif
            }

#if DEBUG && !DISABLE_DEBUG
            protected override void OnSetup()
            {
                _markers = new EcsProfilerMarker[Targets.Length];
                for (int i = 0; i < Targets.Length; i++)
                {
                    _markers[i] = new EcsProfilerMarker(EcsDebug.RegisterMark($"EcsRunner.{Targets[i].GetType().Name}.{nameof(PreInit)}"));
                }
            }
#endif
        }
        [DebugColor(DebugColor.Orange)]
        public sealed class EcsInitRunner : EcsRunner<IEcsInitSystem>, IEcsInitSystem
        {
#if DEBUG && !DISABLE_DEBUG
            private EcsProfilerMarker[] _markers;
#endif
            public void Init(EcsPipeline pipeline)
            {
#if DEBUG && !DISABLE_DEBUG
                for (int i = 0; i < Targets.Length && Targets.Length <= _markers.Length; i++)
                {
                    using (_markers[i].Auto())
                        Targets[i].Init(pipeline);
                }
#else
            foreach (var item in targets) item.Init(pipeline);
#endif
            }

#if DEBUG && !DISABLE_DEBUG
            protected override void OnSetup()
            {
                _markers = new EcsProfilerMarker[Targets.Length];
                for (int i = 0; i < Targets.Length; i++)
                {
                    _markers[i] = new EcsProfilerMarker(EcsDebug.RegisterMark($"EcsRunner.{Targets[i].GetType().Name}.{nameof(Init)}"));
                }
            }
#endif
        }
        [DebugColor(DebugColor.Orange)]
        public sealed class EcsRunRunner : EcsRunner<IEcsRunSystem>, IEcsRunSystem
        {
#if DEBUG && !DISABLE_DEBUG
            private EcsProfilerMarker[] _markers;
#endif
            public void Run(EcsPipeline pipeline)
            {
#if DEBUG && !DISABLE_DEBUG
                for (int i = 0; i < Targets.Length && Targets.Length <= _markers.Length; i++)
                {
                    using (_markers[i].Auto())
                        Targets[i].Run(pipeline);

                }
#else
            foreach (var item in targets) item.Run(pipeline);
#endif
            }

#if DEBUG && !DISABLE_DEBUG
            protected override void OnSetup()
            {
                _markers = new EcsProfilerMarker[Targets.Length];
                for (int i = 0; i < Targets.Length; i++)
                {
                    _markers[i] = new EcsProfilerMarker(EcsDebug.RegisterMark($"EcsRunner.{Targets[i].GetType().Name}.{nameof(Run)}"));
                }
            }
#endif
        }
        [DebugColor(DebugColor.Orange)]
        public sealed class EcsDestroyRunner : EcsRunner<IEcsDestroySystem>, IEcsDestroySystem
        {
#if DEBUG && !DISABLE_DEBUG
            private EcsProfilerMarker[] _markers;
#endif
            public void Destroy(EcsPipeline pipeline)
            {
#if DEBUG && !DISABLE_DEBUG
                for (int i = 0; i < Targets.Length && Targets.Length <= _markers.Length; i++)
                {
                    using (_markers[i].Auto())
                        Targets[i].Destroy(pipeline);
                }
#else
            foreach (var item in targets) item.Destroy(pipeline);
#endif
            }

#if DEBUG && !DISABLE_DEBUG
            protected override void OnSetup()
            {
                _markers = new EcsProfilerMarker[Targets.Length];
                for (int i = 0; i < Targets.Length; i++)
                {
                    _markers[i] = new EcsProfilerMarker(EcsDebug.RegisterMark($"EcsRunner.{Targets[i].GetType().Name}.{nameof(Destroy)}"));
                }
            }
#endif
        }
    }
}
