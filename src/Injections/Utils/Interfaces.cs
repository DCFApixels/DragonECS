#if DISABLE_DEBUG
#undef DEBUG
#endif

namespace DCFApixels.DragonECS
{
    public interface IEcsInjectProcess : IEcsProcess { }
    [MetaName(nameof(Inject))]
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.DI_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "The interface of the dependency injection process.")]
    [MetaID("DragonECS_4C86537C92019AA24383CBF53CBD456C")]
    public interface IEcsInject<T> : IEcsInjectProcess
    {
        void Inject(T obj);
    }
    public interface IMonoWorldInject : IEcsProcess
    {
        EcsWorld World { get; set; }
    }
    [MetaName(nameof(OnInitInjectionComplete))]
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.DI_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "The process interface that signals the completion of injection during pipeline initialization via the EcsPipeline.Init() method.")]
    [MetaID("DragonECS_05C3537C920155AFC044C900E4F17D90")]
    public interface IOnInitInjectionComplete : IEcsProcess
    {
        void OnBeforeInitInjection();
        void OnInitInjectionComplete();
    }
    public interface IInjector
    {
        void Inject<T>(T obj);
        T Extract<T>();
    }
    public interface IInjectionBlock
    {
        void InjectTo(Injector inj);
    }
    public readonly struct InjectionGraph
    {
        private readonly Injector _injector;
        internal InjectionGraph(Injector injector)
        {
            _injector = injector;
        }
        public void AddNode<T>(T obj = default)
        {
            _injector.AddNode<T>();
        }
    }
    public interface IInjectionUnit
    {
        void InitInjectionNode(InjectionGraph graph);
    }
}
