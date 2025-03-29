#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.RunnersCore;
using System;

namespace DCFApixels.DragonECS
{
    [MetaName(nameof(PreInit))]
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.PROCESSES_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "The process to run when EcsPipeline.Init() is called. Before Init")]
    [MetaID("DragonECS_DE26527C92015AFDD4ECF4D81A4C946B")]
    public interface IEcsPreInit : IEcsProcess
    {
        void PreInit();
    }
    [MetaName(nameof(Init))]
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.PROCESSES_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "The process to run when EcsPipeline.Init() is called. After PreInit")]
    [MetaID("DragonECS_CC45527C9201DF82DCAAAEF33072F9EF")]
    public interface IEcsInit : IEcsProcess
    {
        void Init();
    }
    [MetaName(nameof(Run))]
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.PROCESSES_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "The process to run when EcsPipeline.Run() is called.")]
    [MetaID("DragonECS_9654527C9201BE75546322B9BB03C131")]
    public interface IEcsRun : IEcsProcess
    {
        void Run();
    }
    [MetaName(nameof(RunFinally))]
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.PROCESSES_GROUP)]
    public interface IEcsRunFinally : IEcsProcess
    {
        void RunFinally();
    }
    [MetaName(nameof(Destroy))]
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.PROCESSES_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "The process to run when EcsPipeline.Destroy() is called.")]
    [MetaID("DragonECS_4661527C9201EE669C6EB61B19899AE5")]
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
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.PROCESSES_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    [MetaTags(MetaTags.HIDDEN)]
    [MetaID("DragonECS_3273527C9201285BAA0A463F700A50FB")]
    internal sealed class EcsPreInitRunner : EcsRunner<IEcsPreInit>, IEcsPreInit
    {
        private RunHelper _helper;
        protected override void OnSetup()
        {
            _helper = new RunHelper(this);
        }
        public void PreInit()
        {
            _helper.Run(p => p.PreInit());
        }
    }
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.PROCESSES_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    [MetaTags(MetaTags.HIDDEN)]
    [MetaID("DragonECS_ED85527C9201A391AB8EC0B734917859")]
    internal sealed class EcsInitRunner : EcsRunner<IEcsInit>, IEcsInit
    {
        private RunHelper _helper;
        protected override void OnSetup()
        {
            _helper = new RunHelper(this);
        }
        public void Init()
        {
            _helper.Run(p => p.Init());
        }
    }
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.PROCESSES_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    [MetaTags(MetaTags.HIDDEN)]
    [MetaID("DragonECS_2098527C9201F260C840BFD50BC7E0BA")]
    internal sealed class EcsRunRunner : EcsRunner<IEcsRun>, IEcsRun
    {
        private readonly struct Pair
        {
            public readonly IEcsRun run;
            public readonly IEcsRunFinally cleanup;
            public Pair(IEcsRun run)
            {
                this.run = run;
                cleanup = run as IEcsRunFinally;
            }
        }
        private Pair[] _pairs;
#if DEBUG
        private EcsProfilerMarker[] _markers;
#endif
        protected override void OnSetup()
        {
            _pairs = new Pair[Process.Length];
            for (int i = 0; i < Process.Length; i++)
            {
                _pairs[i] = new Pair(Process[i]);
            }
#if DEBUG
            _markers = new EcsProfilerMarker[Process.Length];
            for (int i = 0; i < Process.Length; i++)
            {
                _markers[i] = new EcsProfilerMarker($"{Process[i].GetMeta().Name}.{nameof(Run)}");
            }
#endif
        }
        public void Run()
        {
#if DEBUG
            for (int i = 0, n = _pairs.Length < _markers.Length ? _pairs.Length : _markers.Length; i < n; i++)
            {
                var pair = _pairs[i];
                _markers[i].Begin();
                try
                {
                    pair.run.Run();
                }
                catch (Exception e)
                {
#if DRAGONECS_DISABLE_CATH_EXCEPTIONS
                    throw e;
#else
                    EcsDebug.PrintError(e);
#endif
                }
                finally
                {
                    pair.cleanup?.RunFinally();
                }
                _markers[i].End();
            }
#else
            foreach (var item in Process)
            {
                try { item.Run(); }
                catch (Exception e)
                {
#if DRAGONECS_DISABLE_CATH_EXCEPTIONS
                    throw e;
#else
                    EcsDebug.PrintError(e);
#endif
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
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.PROCESSES_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    [MetaTags(MetaTags.HIDDEN)]
    [MetaID("DragonECS_06A6527C92010430ACEB3DA520F272CC")]
    internal sealed class EcsDestroyRunner : EcsRunner<IEcsDestroy>, IEcsDestroy
    {
        private RunHelper _helper;
        protected override void OnSetup()
        {
            _helper = new RunHelper(this);
        }
        public void Destroy()
        {
            _helper.Run(p => p.Destroy());
        }
    }
}