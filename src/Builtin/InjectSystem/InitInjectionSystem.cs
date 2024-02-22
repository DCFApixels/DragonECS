namespace DCFApixels.DragonECS.DI.Internal
{
    public abstract class InitInjectSystemBase : IEcsSystem { }

    [MetaTags(MetaTags.HIDDEN)]
    [MetaColor(MetaColor.Gray)]
    public  class InitInjectionSystem<T> : InitInjectSystemBase, IEcsPipelineMember, IEcsInject<InitInjectController>, IEcsPreInitInjectProcess
    {
        private EcsPipeline _pipeline;
        public EcsPipeline Pipeline
        {
            get { return Pipeline; }
            set
            {
                _pipeline = value;

                if (_injectedData == null)
                {
                    return;
                }
                if (_injectController == null)
                {
                    _pipeline.Config.Get<InjectionGraph>(InjectionGraph.CONFIG_NAME).Init(_pipeline);
                    var injectPipelineRunner = _pipeline.GetRunner<IEcsInject<EcsPipeline>>();
                    injectPipelineRunner.Inject(_pipeline);
                    EcsRunner.Destroy(injectPipelineRunner);

                    _injectController = new InitInjectController(_pipeline);
                    var injectMapRunner = _pipeline.GetRunner<IEcsInject<InitInjectController>>();
                    _pipeline.GetRunner<IEcsPreInitInjectProcess>().OnPreInitInjectionBefore(_pipeline);
                    injectMapRunner.Inject(_injectController);
                    EcsRunner.Destroy(injectMapRunner);
                }
                var injectRunnerGeneric = _pipeline.GetRunner<IEcsInject<T>>();
                injectRunnerGeneric.Inject(_injectedData);
                if (_injectController.OnInject())
                {
                    _injectController.Destroy();
                    var injectCallbacksRunner = _pipeline.GetRunner<IEcsPreInitInjectProcess>();
                    injectCallbacksRunner.OnPreInitInjectionAfter();
                    EcsRunner.Destroy(injectCallbacksRunner);
                }
                _injectedData = default;
            }
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
        void IEcsPreInitInjectProcess.OnPreInitInjectionBefore(EcsPipeline pipeline) { }
        void IEcsPreInitInjectProcess.OnPreInitInjectionAfter() { _injectController = null; }
    }
}
