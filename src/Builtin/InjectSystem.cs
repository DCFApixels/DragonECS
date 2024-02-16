using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.RunnersCore;
using System;

namespace DCFApixels.DragonECS
{
    public readonly struct Injector
    {
        private readonly EcsPipeline _pipeline;
        public Injector(EcsPipeline pipeline)
        {
            _pipeline = pipeline;
        }
        public void Inject<T>(T data)
        {
            _pipeline.Inject(data);
        }
    }
    public interface IInjectionBlock
    {
        void InjectTo(Injector i);
    }

    [MetaName(nameof(PreInject))]
    [BindWithEcsRunner(typeof(EcsPreInjectRunner))]
    public interface IEcsPreInject : IEcsProcess
    {
        void PreInject(object obj);
    }
    [MetaName(nameof(Inject))]
    [BindWithEcsRunner(typeof(EcsInjectRunner<>))]
    public interface IEcsInject<T> : IEcsProcess
    {
        void Inject(T obj);
    }
    [MetaName("PreInitInject")]
    [BindWithEcsRunner(typeof(EcsPreInitInjectProcessRunner))]
    public interface IEcsPreInitInjectProcess : IEcsProcess
    {
        void OnPreInitInjectionBefore();
        void OnPreInitInjectionAfter();
    }

    namespace Internal
    {
        internal class InitInjectController
        {
            private EcsPipeline _source;
            private InitInjectSystemBase[] _injectSystems;
            private int _injectCount;
            public bool IsInjectionEnd
            {
                get { return _injectCount >= _injectSystems.Length; }
            }
            public InitInjectController(EcsPipeline source)
            {
                _injectCount = 0;
                _source = source;
                _injectSystems = _source.GetSystems<InitInjectSystemBase>();
            }
            public bool OnInject()
            {
                _injectCount++;
                return IsInjectionEnd;
            }
            public void Destroy()
            {
                _source = null;
                _injectSystems = null;
            }
        }
        [MetaTags(MetaTags.HIDDEN)]
        [MetaColor(MetaColor.Gray)]
        public sealed class EcsPreInjectRunner : EcsRunner<IEcsPreInject>, IEcsPreInject
        {
            void IEcsPreInject.PreInject(object obj)
            {
                if (obj is IInjectionBlock container)
                {
                    container.InjectTo(new Injector(Pipeline));
                }
                foreach (var item in targets)
                {
                    item.PreInject(obj);
                }
            }
        }
        [MetaTags(MetaTags.HIDDEN)]
        [MetaColor(MetaColor.Gray)]
        public sealed class EcsInjectRunner<T> : EcsRunner<IEcsInject<T>>, IEcsInject<T>
        {
            private EcsBaseTypeInjectRunner _baseTypeInjectRunner;
            void IEcsInject<T>.Inject(T obj)
            {
                if (obj == null) Throw.ArgumentNull();
                _baseTypeInjectRunner.Inject(obj);
                foreach (var item in targets) item.Inject(obj);
            }
            protected override void OnSetup()
            {
                Type baseType = typeof(T).BaseType;
                if (baseType != typeof(object) && baseType != typeof(ValueType) && baseType != null)
                    _baseTypeInjectRunner = (EcsBaseTypeInjectRunner)Activator.CreateInstance(typeof(EcsBaseTypeInjectRunner<>).MakeGenericType(baseType), Pipeline);
                else
                    _baseTypeInjectRunner = new EcsObjectTypePreInjectRunner(Pipeline);
            }
        }
        internal abstract class EcsBaseTypeInjectRunner
        {
            public abstract void Inject(object obj);
        }
        internal sealed class EcsBaseTypeInjectRunner<T> : EcsBaseTypeInjectRunner
        {
            private IEcsInject<T> _runner;
            public EcsBaseTypeInjectRunner(EcsPipeline pipeline) => _runner = pipeline.GetRunner<IEcsInject<T>>();
            public sealed override void Inject(object obj) => _runner.Inject((T)obj);
        }
        internal sealed class EcsObjectTypePreInjectRunner : EcsBaseTypeInjectRunner
        {
            private IEcsPreInject _runner;
            public EcsObjectTypePreInjectRunner(EcsPipeline pipeline) => _runner = pipeline.GetRunner<IEcsPreInject>();
            public sealed override void Inject(object obj) => _runner.PreInject(obj);
        }

        [MetaTags(MetaTags.HIDDEN)]
        [MetaColor(MetaColor.Gray)]
        public sealed class EcsPreInitInjectProcessRunner : EcsRunner<IEcsPreInitInjectProcess>, IEcsPreInitInjectProcess
        {
            public void OnPreInitInjectionAfter()
            {
                foreach (var item in targets) item.OnPreInitInjectionAfter();
            }
            public void OnPreInitInjectionBefore()
            {
                foreach (var item in targets) item.OnPreInitInjectionBefore();
            }
        }
        public abstract class InitInjectSystemBase { }

        [MetaTags(MetaTags.HIDDEN)]
        [MetaColor(MetaColor.Gray)]
        public class InitInjectSystem<T> : InitInjectSystemBase, IEcsPipelineMember, IEcsInject<InitInjectController>, IEcsPreInitInjectProcess
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
                        var injectPipelineRunner = _pipeline.GetRunner<IEcsInject<EcsPipeline>>();
                        injectPipelineRunner.Inject(_pipeline);
                        EcsRunner.Destroy(injectPipelineRunner);

                        _injectController = new InitInjectController(_pipeline);
                        var injectMapRunner = _pipeline.GetRunner<IEcsInject<InitInjectController>>();
                        _pipeline.GetRunner<IEcsPreInitInjectProcess>().OnPreInitInjectionBefore();
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
            internal InitInjectSystem(T injectedData)
            {
                if (injectedData == null) Throw.ArgumentNull();
                _injectedData = injectedData;
            }
            void IEcsPreInitInjectProcess.OnPreInitInjectionBefore() { }
            void IEcsPreInitInjectProcess.OnPreInitInjectionAfter() { _injectController = null; }
        }
    }

    public static partial class EcsPipelineExtensions
    {
        public static void Inject<T>(this EcsPipeline self, T data)
        {
            self.GetRunner<IEcsInject<T>>().Inject(data);
        }
    }
    public static partial class EcsPipelineBuilderExtensions
    {
        public static EcsPipeline.Builder Inject<T>(this EcsPipeline.Builder self, T data)
        {
            if (data == null) Throw.ArgumentNull();
            self.Add(new InitInjectSystem<T>(data));
            if (data is IEcsModule module)
                self.AddModule(module);
            return self;
        }
        public static EcsPipeline.Builder Inject<T0, T1>(this EcsPipeline.Builder self, T0 d0, T1 d1)
        {
            return self.Inject(d0).Inject(d1);
        }
        public static EcsPipeline.Builder Inject<T0, T1, T2>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2)
        {
            return self.Inject(d0).Inject(d1).Inject(d2);
        }
        public static EcsPipeline.Builder Inject<T0, T1, T2, T3>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2, T3 d3)
        {
            return self.Inject(d0).Inject(d1).Inject(d2).Inject(d3);
        }
        public static EcsPipeline.Builder Inject<T0, T1, T2, T3, T4>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2, T3 d3, T4 d4)
        {
            return self.Inject(d0).Inject(d1).Inject(d2).Inject(d3).Inject(d4);
        }
        public static EcsPipeline.Builder Inject<T0, T1, T2, T3, T4, T5>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2, T3 d3, T4 d4, T5 f)
        {
            return self.Inject(d0).Inject(d1).Inject(d2).Inject(d3).Inject(d4).Inject(f);
        }
        public static EcsPipeline.Builder Inject<T0, T1, T2, T3, T4, T5, T6>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2, T3 d3, T4 d4, T5 f, T6 d6)
        {
            return self.Inject(d0).Inject(d1).Inject(d2).Inject(d3).Inject(d4).Inject(f).Inject(d6);
        }
        public static EcsPipeline.Builder Inject<T0, T1, T2, T3, T4, T5, T6, T7>(this EcsPipeline.Builder self, T0 d0, T1 d1, T2 d2, T3 d3, T4 d4, T5 f, T6 d6, T7 d7)
        {
            return self.Inject(d0).Inject(d1).Inject(d2).Inject(d3).Inject(d4).Inject(f).Inject(d6).Inject(d7);
        }
    }
}
