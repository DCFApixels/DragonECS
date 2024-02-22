using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.RunnersCore;

namespace DCFApixels.DragonECS
{
    [MetaName("PreInitInject")]
    [BindWithEcsRunner(typeof(EcsInitInjectProcessRunner))]
    public interface IEcsPreInitInjectProcess : IEcsProcess
    {
        void OnPreInitInjectionBefore(EcsPipeline pipeline);
        void OnPreInitInjectionAfter();
    }
}
namespace DCFApixels.DragonECS.Internal
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
    public sealed class EcsInitInjectProcessRunner : EcsRunner<IEcsPreInitInjectProcess>, IEcsPreInitInjectProcess
    {
        public void OnPreInitInjectionAfter()
        {
            foreach (var item in Process)
            {
                item.OnPreInitInjectionAfter();
            }
        }
        public void OnPreInitInjectionBefore(EcsPipeline pipeline)
        {
            foreach (var item in Process)
            {
                item.OnPreInitInjectionBefore(pipeline);
            }
        }
    }
}
