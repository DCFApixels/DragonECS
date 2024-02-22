using DCFApixels.DragonECS.DI.Internal;
using DCFApixels.DragonECS.RunnersCore;

namespace DCFApixels.DragonECS
{
    [MetaName(nameof(Inject))]
    [BindWithEcsRunner(typeof(EcsInjectRunner<>))]
    public interface IEcsInject<T> : IEcsSystem
    {
        void Inject(T obj);
    }
    [MetaName("PreInitInject")]
    [BindWithEcsRunner(typeof(EcsInitInjectProcessRunner))]
    public interface IEcsPreInitInjectProcess : IEcsSystem
    {
        void OnPreInitInjectionBefore(EcsPipeline pipeline);
        void OnPreInitInjectionAfter();
    }
}
namespace DCFApixels.DragonECS.DI.Internal
{
    internal class InitInjectController
    {
        private EcsPipeline _source;
        private EcsProcess<InitInjectSystemBase> _injectSystems;
        private int _injectCount;
        public bool IsInjectionEnd
        {
            get { return _injectCount >= _injectSystems.Length; }
        }
        public InitInjectController(EcsPipeline source)
        {
            _injectCount = 0;
            _source = source;
            _injectSystems = _source.GetProcess<InitInjectSystemBase>();
        }
        public bool OnInject()
        {
            _injectCount++;
            return IsInjectionEnd;
        }
        public void Destroy()
        {
            _source = null;
            _injectSystems = EcsProcess<InitInjectSystemBase>.Empty;
        }
    }
    [MetaTags(MetaTags.HIDDEN)]
    [MetaColor(MetaColor.Gray)]
    public sealed class EcsInjectRunner<T> : EcsRunner<IEcsInject<T>>, IEcsInject<T>
    {
        private InjectionGraph _injectionGraph;
        void IEcsInject<T>.Inject(T obj)
        {
            _injectionGraph.Inject(obj);
        }
        protected override void OnSetup()
        {
            _injectionGraph = Pipeline.Config.Get<InjectionGraph>(InjectionGraph.CONFIG_NAME);
        }
    }
    [MetaTags(MetaTags.HIDDEN)]
    [MetaColor(MetaColor.Gray)]
    public sealed class EcsInitInjectProcessRunner : EcsRunner<IEcsPreInitInjectProcess>, IEcsPreInitInjectProcess
    {
        public void OnPreInitInjectionAfter()
        {
            foreach (var item in Process) item.OnPreInitInjectionAfter();
        }
        public void OnPreInitInjectionBefore(EcsPipeline pipeline)
        {
            foreach (var item in Process) item.OnPreInitInjectionBefore(pipeline);
        }
    }
}
