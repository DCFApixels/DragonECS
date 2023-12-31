#pragma warning disable CS0162 // Обнаружен недостижимый код
using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.RunnersCore;
using System;

namespace DCFApixels.DragonECS
{
    #region Interfaces
    [MetaName(nameof(PreInit))]
    [MetaColor(MetaColor.Orange)]
    [BindWithEcsRunner(typeof(EcsPreInitProcessRunner))]
    public interface IEcsPreInitProcess : IEcsProcess
    {
        void PreInit();
    }
    [MetaName(nameof(Init))]
    [MetaColor(MetaColor.Orange)]
    [BindWithEcsRunner(typeof(EcsInitProcessRunner))]
    public interface IEcsInitProcess : IEcsProcess
    {
        void Init();
    }
    [MetaName(nameof(Run))]
    [MetaColor(MetaColor.Orange)]
    [BindWithEcsRunner(typeof(EcsRunProcessRunner))]
    public interface IEcsRunProcess : IEcsProcess
    {
        void Run();
    }
    [MetaName(nameof(Destroy))]
    [MetaColor(MetaColor.Orange)]
    [BindWithEcsRunner(typeof(EcsDestroyProcessRunner))]
    public interface IEcsDestroyProcess : IEcsProcess
    {
        void Destroy();
    }

    #endregion

    namespace Internal
    {
        [MetaColor(MetaColor.Orange)]
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
            public void PreInit()
            {
#if DEBUG && !DISABLE_DEBUG
                for (int i = 0; i < targets.Length && targets.Length <= _markers.Length; i++)
                {
                    _markers[i].Begin();
                    try
                    {
                        targets[i].PreInit();
                    }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw e;
#endif
                        EcsDebug.PrintError(e);
                    }
                    _markers[i].End();
                }
#else
                foreach (var item in targets)
                {
                    try { item.PreInit(); }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw e;
#endif
                        EcsDebug.PrintError(e);
                    }
                }
#endif
            }
        }
        [MetaColor(MetaColor.Orange)]
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
            public void Init()
            {
#if DEBUG && !DISABLE_DEBUG
                for (int i = 0; i < targets.Length && targets.Length <= _markers.Length; i++)
                {
                    _markers[i].Begin();
                    try
                    {
                        targets[i].Init();
                    }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw e;
#endif
                        EcsDebug.PrintError(e);
                    }
                    _markers[i].End();
                }
#else
                foreach (var item in targets)
                {
                    try { item.Init(); }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw e;
#endif
                        EcsDebug.PrintError(e);
                    }
                }
#endif
            }
        }
        [MetaColor(MetaColor.Orange)]
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
            public void Run()
            {
#if DEBUG && !DISABLE_DEBUG
                for (int i = 0; i < targets.Length && targets.Length <= _markers.Length; i++)
                {
                    _markers[i].Begin();
                    try
                    {
                        targets[i].Run();
                    }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw e;
#endif
                        EcsDebug.PrintError(e);
                    }
                    _markers[i].End();
                }
#else
                foreach (var item in targets)
                {
                    try { item.Run(); }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw e;
#endif
                        EcsDebug.PrintError(e);
                    }
                }
#endif
            }
        }
        [MetaColor(MetaColor.Orange)]
        public sealed class EcsDestroyProcessRunner : EcsRunner<IEcsDestroyProcess>, IEcsDestroyProcess
        {
#if DEBUG && !DISABLE_DEBUG
            private EcsProfilerMarker[] _markers;
            protected override void OnSetup()
            {
                _markers = new EcsProfilerMarker[targets.Length];
                for (int i = 0; i < targets.Length; i++)
                {
                    _markers[i] = new EcsProfilerMarker($"{targets[i].GetType().Name}.{nameof(IEcsDestroyProcess.Destroy)}");
                }
            }
#endif
            void IEcsDestroyProcess.Destroy()
            {
#if DEBUG && !DISABLE_DEBUG
                for (int i = 0; i < targets.Length && targets.Length <= _markers.Length; i++)
                {
                    _markers[i].Begin();
                    try
                    {
                        targets[i].Destroy();
                    }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw e;
#endif
                        EcsDebug.PrintError(e);
                    }
                    _markers[i].End();
                }
#else
                foreach (var item in targets)
                {
                    try { item.Destroy(); }
                    catch (Exception e)
                    {
#if DISABLE_CATH_EXCEPTIONS
                        throw e;
#endif
                        EcsDebug.PrintError(e);
                    }
                }
#endif
            }
        }
    }
}
