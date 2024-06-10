#pragma warning disable CS0162 // Обнаружен недостижимый код
using DCFApixels.DragonECS.RunnersCore;
using System;

namespace DCFApixels.DragonECS
{
    [MetaName(nameof(PreInit))]
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.FRAMEWORK_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    public interface IEcsPreInit : IEcsProcess
    {
        void PreInit();
    }
    [MetaName(nameof(Init))]
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.FRAMEWORK_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    public interface IEcsInit : IEcsProcess
    {
        void Init();
    }
    [MetaName(nameof(Run))]
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.FRAMEWORK_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    public interface IEcsRun : IEcsProcess
    {
        void Run();
    }
    [MetaName(nameof(Destroy))]
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.FRAMEWORK_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    public interface IEcsDestroy : IEcsProcess
    {
        void Destroy();
    }
}

namespace DCFApixels.DragonECS.Internal
{
#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.FRAMEWORK_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    internal sealed class EcsPreInitRunner : EcsRunner<IEcsPreInit>, IEcsPreInit
    {
#if DEBUG && !DISABLE_DEBUG
        private EcsProfilerMarker[] _markers;
        protected override void OnSetup()
        {
            _markers = new EcsProfilerMarker[Process.Length];
            for (int i = 0; i < Process.Length; i++)
            {
                _markers[i] = new EcsProfilerMarker($"{Process[i].GetMeta().Name}.{nameof(PreInit)}");
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
                try 
                { 
                    item.PreInit(); 
                }
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
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.FRAMEWORK_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    internal sealed class EcsInitRunner : EcsRunner<IEcsInit>, IEcsInit
    {
#if DEBUG && !DISABLE_DEBUG
        private EcsProfilerMarker[] _markers;
        protected override void OnSetup()
        {
            _markers = new EcsProfilerMarker[Process.Length];
            for (int i = 0; i < Process.Length; i++)
            {
                _markers[i] = new EcsProfilerMarker($"{Process[i].GetMeta().Name}.{nameof(Init)}");
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
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.FRAMEWORK_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    internal sealed class EcsRunRunner : EcsRunner<IEcsRun>, IEcsRun
    {
#if DEBUG && !DISABLE_DEBUG
        private EcsProfilerMarker[] _markers;
        protected override void OnSetup()
        {
            _markers = new EcsProfilerMarker[Process.Length];
            for (int i = 0; i < Process.Length; i++)
            {
                _markers[i] = new EcsProfilerMarker($"{Process[i].GetMeta().Name}.{nameof(Run)}");
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
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.FRAMEWORK_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    internal sealed class EcsDestroyRunner : EcsRunner<IEcsDestroy>, IEcsDestroy
    {
#if DEBUG && !DISABLE_DEBUG
        private EcsProfilerMarker[] _markers;
        protected override void OnSetup()
        {
            _markers = new EcsProfilerMarker[Process.Length];
            for (int i = 0; i < Process.Length; i++)
            {
                _markers[i] = new EcsProfilerMarker($"{Process[i].GetMeta().Name}.{nameof(IEcsDestroy.Destroy)}");
            }
        }
#endif
        public void Destroy()
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
