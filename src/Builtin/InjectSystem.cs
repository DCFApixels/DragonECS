using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.RunnersCore;
using System.Linq;

namespace DCFApixels.DragonECS
{
    //TODO развить идею инжектов
    //1) добавить расширенный метод инжекта, с 2 джинерик-аргументами, первый базовый тип и второй инжектируемый тип.
    //напримере это будет работать так Inject<object, Foo> делает инжект объекта типа Foo для систем с IEcsInject<object> или с IEcsInject<Foo>
    //2) добавить контейнер, который автоматически создается, собирает в себя все пре-инжекты и авто-инжектится во все системы.
    //но это спорная идея
    namespace Internal
    {
        internal class PreInitInjectController 
        {
            private EcsPipeline _source;
            private InjectSystemBase[] _injectSystems;
            private int _injectCount;
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
            public bool IsInjectionEnd => _injectCount >= _injectSystems.Length;
            public void Destroy()
            {
                _source = null;
                _injectSystems = null;
            }
        }
    }

    public interface IEcsPreInject : IEcsSystem
    {
        public void PreInject(object obj);
    }
    public interface IEcsInject<T> : IEcsSystem
    {
        public void Inject(T obj);
    }
    public interface IEcsPreInitInjectCallbacks : IEcsSystem
    {
        public void OnPreInitInjectionBefore();
        public void OnPreInitInjectionAfter();
    }

    namespace Internal
    {
        [DebugHide, DebugColor(DebugColor.Gray)]
        public sealed class PreInjectRunner : EcsRunner<IEcsPreInject>, IEcsPreInject
        {
            void IEcsPreInject.PreInject(object obj)
            {
                foreach (var item in targets) item.PreInject(obj);
            }
        }
        [DebugHide, DebugColor(DebugColor.Gray)]
        public sealed class InjectRunner<T> : EcsRunner<IEcsInject<T>>, IEcsInject<T>
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
        public sealed class InjectCallbacksRunner : EcsRunner<IEcsPreInitInjectCallbacks>, IEcsPreInitInjectCallbacks
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
    }

    public class InjectSystemBase { }

    [DebugHide, DebugColor(DebugColor.Gray)]
    public class InjectSystem<T> : InjectSystemBase, IEcsPreInitSystem, IEcsInject<PreInitInjectController>, IEcsPreInitInjectCallbacks
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

            if (_injectController == null)
            {
                _injectController = new PreInitInjectController(pipeline);
                var injectMapRunner = pipeline.GetRunner<IEcsInject<PreInitInjectController>>();
                pipeline.GetRunner<IEcsPreInitInjectCallbacks>().OnPreInitInjectionBefore();
                injectMapRunner.Inject(_injectController);
            }

            var injectRunnerGeneric = pipeline.GetRunner<IEcsInject<T>>();
            injectRunnerGeneric.Inject(_injectedData);

            if (_injectController.OnInject())
            {
                _injectController.Destroy();
                var injectCallbacksRunner = pipeline.GetRunner<IEcsPreInitInjectCallbacks>();
                injectCallbacksRunner.OnPreInitInjectionAfter();
                EcsRunner.Destroy(injectCallbacksRunner);
            }
        }

        public void OnPreInitInjectionBefore() { }

        public void OnPreInitInjectionAfter()
        {
            _injectController = null;
        }
    }

    public static class InjectSystemExstensions
    {
        public static EcsPipeline.Builder Inject<T>(this EcsPipeline.Builder self, T data)
        {
            self.Add(new InjectSystem<T>(data));
            return self;
        }
        public static EcsPipeline.Builder Inject<A, B>(this EcsPipeline.Builder self, A a, B b)
        {
            self.Inject(a).Inject(b);
            return self;
        }
        public static EcsPipeline.Builder Inject<A, B, C, D>(this EcsPipeline.Builder self, A a, B b, C c, D d)
        {
            self.Inject(a).Inject(b).Inject(c).Inject(d);
            return self;
        }
        public static EcsPipeline.Builder Inject<A, B, C, D, E>(this EcsPipeline.Builder self, A a, B b, C c, D d, E e)
        {
            self.Inject(a).Inject(b).Inject(c).Inject(d).Inject(e);
            return self;
        }
        public static EcsPipeline.Builder Inject<A, B, C, D, E, F>(this EcsPipeline.Builder self, A a, B b, C c, D d, E e, F f)
        {
            self.Inject(a).Inject(b).Inject(c).Inject(d).Inject(e).Inject(f);
            return self;
        }
    }
}
