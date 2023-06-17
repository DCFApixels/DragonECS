#pragma warning disable CS0162 // Обнаружен недостижимый код
using DCFApixels.DragonECS.RunnersCore;

namespace DCFApixels.DragonECS
{
    #region Interfaces
    public interface IEcsPreInitProcess : IEcsProcess
    {
        void PreInit(EcsPipeline pipeline);
    }
    public interface IEcsInitProcess : IEcsProcess
    {
        void Init(EcsPipeline pipeline);
    }
    public interface IEcsRunProcess : IEcsProcess
    {
        void Run(EcsPipeline pipeline);
    }
    public interface IEcsDestroyProcess : IEcsProcess
    {
        void Destroy(EcsPipeline pipeline);
    }

    #endregion

    namespace Internal
    {
        [DebugColor(DebugColor.Orange)]
        public sealed class EcsPreInitProcessRunner : EcsRunner<IEcsPreInitProcess>, IEcsPreInitProcess
        {
#if DEBUG && !DISABLE_DEBUG
            private EcsProfilerMarker[] _markers;
            protected override void OnSetup()
            {
                _markers = new EcsProfilerMarker[targets.Length];
                for (int i = 0; i < targets.Length; i++)
                {
                    _markers[i] = new EcsProfilerMarker($"{targets[i].GetType().Name}.{nameof(PreInit)}");
                }
            }
#endif
            public void PreInit(EcsPipeline pipeline)
            {
#if DEBUG && !DISABLE_DEBUG
                for (int i = 0; i < targets.Length && targets.Length <= _markers.Length; i++)
                {
                    try
                    {
                        _markers[i].Begin();
                        targets[i].PreInit(pipeline);
                        _markers[i].End();
                    }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw;
#endif
                        EcsDebug.PrintError(e.Message);
                    }
                }
#else
                foreach (var item in targets)
                {
                    try { item.PreInit(pipeline); }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw;
#endif
                        EcsDebug.PrintError(e.Message);
                    }
                }
#endif
            }
        }
        [DebugColor(DebugColor.Orange)]
        public sealed class EcsInitProcessRunner : EcsRunner<IEcsInitProcess>, IEcsInitProcess
        {
#if DEBUG && !DISABLE_DEBUG
            private EcsProfilerMarker[] _markers;
            protected override void OnSetup()
            {
                _markers = new EcsProfilerMarker[targets.Length];
                for (int i = 0; i < targets.Length; i++)
                {
                    _markers[i] = new EcsProfilerMarker($"{targets[i].GetType().Name}.{nameof(Init)}");
                }
            }
#endif
            public void Init(EcsPipeline pipeline)
            {
#if DEBUG && !DISABLE_DEBUG
                for (int i = 0; i < targets.Length && targets.Length <= _markers.Length; i++)
                {
                    try
                    {
                        _markers[i].Begin();
                        targets[i].Init(pipeline);
                        _markers[i].End();
                    }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw;
#endif
                        EcsDebug.PrintError(e.Message);
                    }
                }
#else
                foreach (var item in targets)
                {
                    try { item.Init(pipeline); }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw;
#endif
                        EcsDebug.PrintError(e.Message);
                    }
                }
#endif
            }
        }
        [DebugColor(DebugColor.Orange)]
        public sealed class EcsRunProcessRunner : EcsRunner<IEcsRunProcess>, IEcsRunProcess
        {
#if DEBUG && !DISABLE_DEBUG
            private EcsProfilerMarker[] _markers;
            protected override void OnSetup()
            {
                _markers = new EcsProfilerMarker[targets.Length];
                for (int i = 0; i < targets.Length; i++)
                {
                    _markers[i] = new EcsProfilerMarker($"{targets[i].GetType().Name}.{nameof(Run)}");
                }
            }
#endif
            public void Run(EcsPipeline pipeline)
            {
#if DEBUG && !DISABLE_DEBUG
                for (int i = 0; i < targets.Length && targets.Length <= _markers.Length; i++)
                {
                    try
                    {
                        _markers[i].Begin();
                        targets[i].Run(pipeline);
                        _markers[i].End();
                    }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw;
#endif
                        EcsDebug.PrintError(e.Message);
                    }
                }
#else
                foreach (var item in targets)
                {
                    try { item.Run(pipeline); }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw;
#endif
                        EcsDebug.PrintError(e.Message);
                    }
                }
#endif
            }
        }
        [DebugColor(DebugColor.Orange)]
        public sealed class EcsDestroyProcessRunner : EcsRunner<IEcsDestroyProcess>, IEcsDestroyProcess
        {
#if DEBUG && !DISABLE_DEBUG
            private EcsProfilerMarker[] _markers;
            protected override void OnSetup()
            {
                _markers = new EcsProfilerMarker[targets.Length];
                for (int i = 0; i < targets.Length; i++)
                {
                    _markers[i] = new EcsProfilerMarker($"{targets[i].GetType().Name}.{nameof(Destroy)}");
                }
            }
#endif
            public void Destroy(EcsPipeline pipeline)
            {
#if DEBUG && !DISABLE_DEBUG
                for (int i = 0; i < targets.Length && targets.Length <= _markers.Length; i++)
                {
                    try
                    {
                        _markers[i].Begin();
                        targets[i].Destroy(pipeline);
                        _markers[i].End();
                    }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw;
#endif
                        EcsDebug.PrintError(e.Message);
                    }
                }
#else
                foreach (var item in targets)
                {
                    try { item.Destroy(pipeline); }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw;
#endif
                        EcsDebug.PrintError(e.Message);
                    }
                }
#endif
            }
        }
    }
}
