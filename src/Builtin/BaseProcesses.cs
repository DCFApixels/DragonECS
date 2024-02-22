#pragma warning disable CS0162 // Обнаружен недостижимый код
using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.RunnersCore;
using System;

namespace DCFApixels.DragonECS
{
    [MetaName(nameof(PreInit))]
    [MetaColor(MetaColor.Orange)]
    [BindWithEcsRunner(typeof(EcsPreInitRunner))]
    public interface IEcsPreInit : IEcsSystem
    {
        void PreInit();
    }
    [MetaName(nameof(Init))]
    [MetaColor(MetaColor.Orange)]
    [BindWithEcsRunner(typeof(EcsInitRunner))]
    public interface IEcsInit : IEcsSystem
    {
        void Init();
    }
    [MetaName(nameof(Run))]
    [MetaColor(MetaColor.Orange)]
    [BindWithEcsRunner(typeof(EcsRunRunner))]
    public interface IEcsRun : IEcsSystem
    {
        void Run();
    }
    [MetaName(nameof(Destroy))]
    [MetaColor(MetaColor.Orange)]
    [BindWithEcsRunner(typeof(EcsDestroyRunner))]
    public interface IEcsDestroy : IEcsSystem
    {
        void Destroy();
    }
}

namespace DCFApixels.DragonECS.Internal
{
    [MetaColor(MetaColor.Orange)]
    public sealed class EcsPreInitRunner : EcsRunner<IEcsPreInit>, IEcsPreInit
    {
#if DEBUG && !DISABLE_DEBUG
        private EcsProfilerMarker[] _markers;
        protected override void OnSetup()
        {
            _markers = new EcsProfilerMarker[Process.Length];
            for (int i = 0; i < Process.Length; i++)
            {
                _markers[i] = new EcsProfilerMarker($"{Process[i].GetType().Name}.{nameof(PreInit)}");
            }
        }
#endif
        public void PreInit()
        {
#if DEBUG && !DISABLE_DEBUG
            for (int i = 0, n = Process.Length < _markers.Length ? Process.Length : _markers.Length; i < n; i++)
            {
                _markers[i].Begin();
                try
                {
                    Process[i].PreInit();
                }
                catch (Exception e)
                {
#if DISABLE_CATH_EXCEPTIONS
                    throw;
#endif
                    EcsDebug.PrintError(e);
                }
                _markers[i].End();
            }
#else
            foreach (var item in Process)
            {
                try { item.PreInit(); }
                catch (Exception e)
                {
#if DISABLE_CATH_EXCEPTIONS
                    throw;
#endif
                    EcsDebug.PrintError(e);
                }
            }
#endif
        }
    }
    [MetaColor(MetaColor.Orange)]
    public sealed class EcsInitRunner : EcsRunner<IEcsInit>, IEcsInit
    {
#if DEBUG && !DISABLE_DEBUG
        private EcsProfilerMarker[] _markers;
        protected override void OnSetup()
        {
            _markers = new EcsProfilerMarker[Process.Length];
            for (int i = 0; i < Process.Length; i++)
            {
                _markers[i] = new EcsProfilerMarker($"{Process[i].GetType().Name}.{nameof(Init)}");
            }
        }
#endif
        public void Init()
        {
#if DEBUG && !DISABLE_DEBUG
            for (int i = 0, n = Process.Length < _markers.Length ? Process.Length : _markers.Length; i < n; i++)
            {
                _markers[i].Begin();
                try
                {
                    Process[i].Init();
                }
                catch (Exception e)
                {
#if DISABLE_CATH_EXCEPTIONS
                    throw;
#endif
                    EcsDebug.PrintError(e);
                }
                _markers[i].End();
            }
#else
            foreach (var item in Process)
            {
                try { item.Init(); }
                catch (Exception e)
                {
#if DISABLE_CATH_EXCEPTIONS
                    throw;
#endif
                    EcsDebug.PrintError(e);
                }
            }
#endif
        }
    }
    [MetaColor(MetaColor.Orange)]
    public sealed class EcsRunRunner : EcsRunner<IEcsRun>, IEcsRun
    {
#if DEBUG && !DISABLE_DEBUG
        private EcsProfilerMarker[] _markers;
        protected override void OnSetup()
        {
            _markers = new EcsProfilerMarker[Process.Length];
            for (int i = 0; i < Process.Length; i++)
            {
                _markers[i] = new EcsProfilerMarker($"{Process[i].GetType().Name}.{nameof(Run)}");
            }
        }
#endif
        public void Run()
        {
#if DEBUG && !DISABLE_DEBUG
            for (int i = 0, n = Process.Length < _markers.Length ? Process.Length : _markers.Length; i < n; i++)
            {
                _markers[i].Begin();
                try
                {
                    Process[i].Run();
                }
                catch (Exception e)
                {
#if DISABLE_CATH_EXCEPTIONS
                    throw;
#endif
                    EcsDebug.PrintError(e);
                }
                _markers[i].End();
            }
#else
            foreach (var item in Process)
            {
                try { item.Run(); }
                catch (Exception e)
                {
#if DISABLE_CATH_EXCEPTIONS
                    throw;
#endif
                    EcsDebug.PrintError(e);
                }
            }
#endif
        }
    }
    [MetaColor(MetaColor.Orange)]
    public sealed class EcsDestroyRunner : EcsRunner<IEcsDestroy>, IEcsDestroy
    {
#if DEBUG && !DISABLE_DEBUG
        private EcsProfilerMarker[] _markers;
        protected override void OnSetup()
        {
            _markers = new EcsProfilerMarker[Process.Length];
            for (int i = 0; i < Process.Length; i++)
            {
                _markers[i] = new EcsProfilerMarker($"{Process[i].GetType().Name}.{nameof(IEcsDestroy.Destroy)}");
            }
        }
#endif
        void IEcsDestroy.Destroy()
        {
#if DEBUG && !DISABLE_DEBUG
            for (int i = 0, n = Process.Length < _markers.Length ? Process.Length : _markers.Length; i < n; i++)
            {
                _markers[i].Begin();
                try
                {
                    Process[i].Destroy();
                }
                catch (Exception e)
                {
#if DISABLE_CATH_EXCEPTIONS
                    throw;
#endif
                    EcsDebug.PrintError(e);
                }
                _markers[i].End();
            }
#else
            foreach (var item in Process)
            {
                try { item.Destroy(); }
                catch (Exception e)
                {
#if DISABLE_CATH_EXCEPTIONS
                    throw;
#endif
                    EcsDebug.PrintError(e);
                }
            }
#endif
        }
    }
}
