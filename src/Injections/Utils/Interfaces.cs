﻿namespace DCFApixels.DragonECS
{
    [MetaName(nameof(Inject))]
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.DI_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "The interface of the dependency injection process.")]
    public interface IEcsInject<T> : IEcsProcess
    {
        void Inject(T obj);
    }
    [MetaName(nameof(OnInitInjectionComplete))]
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.DI_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "The process interface that signals the completion of injection during pipeline initialization via the EcsPipeline.Init() method.")]
    public interface IOnInitInjectionComplete : IEcsProcess
    {
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
