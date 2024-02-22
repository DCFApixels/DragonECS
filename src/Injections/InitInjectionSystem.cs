using System.Collections.Generic;

namespace DCFApixels.DragonECS.Internal
{
    public abstract class InitInjectSystemBase : IEcsProcess { }

    [MetaTags(MetaTags.HIDDEN)]
    [MetaColor(MetaColor.Gray)]
    public class InitInjectionSystem<T> : InitInjectSystemBase, IEcsPipelineMember, IEcsInject<InitInjectController>, IEcsPreInitInjectProcess
    {
        private EcsPipeline _pipeline;
        public EcsPipeline Pipeline
        {
            get { return Pipeline; }
            set { _pipeline = value; Init(); }
        }
        private InitInjectController _injectController;
        void IEcsInject<InitInjectController>.Inject(InitInjectController obj) { _injectController = obj; }

        private T _injectedData;
        internal InitInjectionSystem(T injectedData)
        {
            if (injectedData == null)
            {
                Throw.ArgumentNull();
            }
            _injectedData = injectedData;
        }
        private void Init()
        {
            if (_injectedData == null)
            {
                return;
            }
            if (_injectController == null)
            {
                _injectController = new InitInjectController(_pipeline);
                _pipeline.GetRunner<IEcsPreInitInjectProcess>().OnPreInitInjectionBefore(_pipeline);
                _pipeline.Injector.Inject(_injectController);
            }
            _pipeline.Injector.Inject(_injectedData);
            if (_injectController.OnInject())
            {
                _injectController.Destroy();
                var injectCallbacksRunner = _pipeline.GetRunner<IEcsPreInitInjectProcess>();
                injectCallbacksRunner.OnPreInitInjectionAfter();
                EcsRunner.Destroy(injectCallbacksRunner);
            }
            _injectedData = default;
        }
        void IEcsPreInitInjectProcess.OnPreInitInjectionBefore(EcsPipeline pipeline) { }
        void IEcsPreInitInjectProcess.OnPreInitInjectionAfter() { _injectController = null; }
    }
}
