using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.RunnersCore;
using System.Linq;

namespace DCFApixels.DragonECS
{
    public interface IEcsPreInject : IEcsSystem
    {
        void PreInject(object obj);
    }
    public interface IEcsInject<T> : IEcsSystem
    {
        void Inject(T obj);
    }
    public interface IEcsPreInitInjectProcess : IEcsSystem
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
        [DebugHide, DebugColor(DebugColor.Gray)]
        public sealed class EcsPreInjectRunner : EcsRunner<IEcsPreInject>, IEcsPreInject
        {
            void IEcsPreInject.PreInject(object obj)
            {
                foreach (var item in targets) item.PreInject(obj);
            }
        }
        [DebugHide, DebugColor(DebugColor.Gray)]
        public sealed class EcsInjectRunner<T> : EcsRunner<IEcsInject<T>>, IEcsInject<T>
        {
            private IEcsPreInject _preInjectchache;
            void IEcsInject<T>.Inject(T obj)
            {
                _preInjectchache.PreInject(obj);
                foreach (var item in targets) item.Inject(obj);
            }
            protected override void OnSetup()
            {
                _preInjectchache = Source.GetRunner<IEcsPreInject>();
            }
        }
        [DebugHide, DebugColor(DebugColor.Gray)]
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
        public class InjectSystemBase { }
        [DebugHide, DebugColor(DebugColor.Gray)]
        public class InjectSystem<T> : InjectSystemBase, IEcsPreInitProcess, IEcsInject<PreInitInjectController>, IEcsPreInitInjectProcess
        {
            private T _injectedData;
            private PreInitInjectController _injectController;
            void IEcsInject<PreInitInjectController>.Inject(PreInitInjectController obj) => _injectController = obj;
            public InjectSystem(T injectedData)
            {
                _injectedData = injectedData;
            }
            public void PreInit(EcsPipeline pipeline)
            {
                if (_injectedData == null) return;
                if (_injectController == null)
                {
                    _injectController = new PreInitInjectController(pipeline);
                    var injectMapRunner = pipeline.GetRunner<IEcsInject<PreInitInjectController>>();
                    pipeline.GetRunner<IEcsPreInitInjectProcess>().OnPreInitInjectionBefore();
                    injectMapRunner.Inject(_injectController);
                }
                var injectRunnerGeneric = pipeline.GetRunner<IEcsInject<T>>();
                injectRunnerGeneric.Inject(_injectedData);
                if (_injectController.OnInject())
                {
                    _injectController.Destroy();
                    var injectCallbacksRunner = pipeline.GetRunner<IEcsPreInitInjectProcess>();
                    injectCallbacksRunner.OnPreInitInjectionAfter();
                    EcsRunner.Destroy(injectCallbacksRunner);
                }
            }
            public void OnPreInitInjectionBefore() { }
            public void OnPreInitInjectionAfter() => _injectController = null;
        }
    }

    public static class InjectSystemExtensions
    {
        public static EcsPipeline.Builder Inject<T>(this EcsPipeline.Builder self, T data)
        {
            return self.Add(new InjectSystem<T>(data));
        }
        public static EcsPipeline.Builder Inject<A, B>(this EcsPipeline.Builder self, A a, B b)
        {
            return self.Inject(a).Inject(b);
        }
        public static EcsPipeline.Builder Inject<A, B, C>(this EcsPipeline.Builder self, A a, B b, C c)
        {
            return self.Inject(a).Inject(b).Inject(c);
        }
        public static EcsPipeline.Builder Inject<A, B, C, D>(this EcsPipeline.Builder self, A a, B b, C c, D d)
        {
            return self.Inject(a).Inject(b).Inject(c).Inject(d);
        }
        public static EcsPipeline.Builder Inject<A, B, C, D, E>(this EcsPipeline.Builder self, A a, B b, C c, D d, E e)
        {
            return self.Inject(a).Inject(b).Inject(c).Inject(d).Inject(e);
        }
        public static EcsPipeline.Builder Inject<A, B, C, D, E, F>(this EcsPipeline.Builder self, A a, B b, C c, D d, E e, F f)
        {
            return self.Inject(a).Inject(b).Inject(c).Inject(d).Inject(e).Inject(f);
        }
        public static EcsPipeline.Builder Inject<A, B, C, D, E, F, G>(this EcsPipeline.Builder self, A a, B b, C c, D d, E e, F f, G g)
        {
            return self.Inject(a).Inject(b).Inject(c).Inject(d).Inject(e).Inject(f).Inject(g);
        }
        public static EcsPipeline.Builder Inject<A, B, C, D, E, F, G, H>(this EcsPipeline.Builder self, A a, B b, C c, D d, E e, F f, G g, H h)
        {
            return self.Inject(a).Inject(b).Inject(c).Inject(d).Inject(e).Inject(f).Inject(g).Inject(h);
        }
    }
}
