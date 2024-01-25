using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.RunnersCore;
using System;
using System.Linq;

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
        internal class PreInitInjectController
        {
            private EcsPipeline _source;
            private InjectSystemBase[] _injectSystems;
            private int _injectCount;
            public bool IsInjectionEnd => _injectCount >= _injectSystems.Length;
            public PreInitInjectController(EcsPipeline source)
            {
                _injectCount = 0;
                _source = source;
                _injectSystems = _source.AllSystems.OfType<InjectSystemBase>().ToArray();
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
        public abstract class InjectSystemBase { }

        [MetaTags(MetaTags.HIDDEN)]
        [MetaColor(MetaColor.Gray)]
        public class InjectSystem<T> : InjectSystemBase, IEcsInject<EcsPipeline>, IEcsPreInitProcess, IEcsInject<PreInitInjectController>, IEcsPreInitInjectProcess
        {
            private EcsPipeline _pipeline;
            void IEcsInject<EcsPipeline>.Inject(EcsPipeline obj) => _pipeline = obj;
            private PreInitInjectController _injectController;
            void IEcsInject<PreInitInjectController>.Inject(PreInitInjectController obj) => _injectController = obj;

            private T _injectedData;

            public InjectSystem(T injectedData)
            {
                if (injectedData == null) Throw.ArgumentNull();
                _injectedData = injectedData;
            }
            public void PreInit()
            {
                if (_injectedData == null) return;
                if (_injectController == null)
                {
                    _injectController = new PreInitInjectController(_pipeline);
                    var injectMapRunner = _pipeline.GetRunner<IEcsInject<PreInitInjectController>>();
                    _pipeline.GetRunner<IEcsPreInitInjectProcess>().OnPreInitInjectionBefore();
                    injectMapRunner.Inject(_injectController);
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
            public void OnPreInitInjectionBefore() { }
            public void OnPreInitInjectionAfter() => _injectController = null;
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
            self.Add(new InjectSystem<T>(data));
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
