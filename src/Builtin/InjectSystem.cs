using DCFApixels.DragonECS.Internal;
using System.Linq;

namespace DCFApixels.DragonECS
{
    namespace Internal
    {
        internal class PreInitInjectController 
        {
            private EcsSystems _source;
            private InjectSystemBase[] _injectSystems;
            private int _injectCount;

            public PreInitInjectController(EcsSystems source)
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

            public bool IsInjectionEnd => _injectSystems.Length <= _injectCount;

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
    [DebugHide, DebugColor(DebugColor.Gray)]
    public sealed class PreInjectRunner : EcsRunner<IEcsPreInject>, IEcsPreInject
    {
        void IEcsPreInject.PreInject(object obj)
        {
            foreach (var item in targets) item.PreInject(obj);
        }
    }

    public interface IEcsInject<T> : IEcsSystem
    {
        public void Inject(T obj);
    }

    [DebugHide,  DebugColor(DebugColor.Gray)]
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

    public interface IEcsPreInitInjectCallbacks : IEcsSystem
    {
        public void OnPreInitInjectionBefore();
        public void OnPreInitInjectionAfter();
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

        public void PreInit(EcsSystems systems)
        {

            if (_injectController == null)
            {
                _injectController = new PreInitInjectController(systems);
                var injectMapRunner = systems.GetRunner<IEcsInject<PreInitInjectController>>();
                systems.GetRunner<IEcsPreInitInjectCallbacks>().OnPreInitInjectionBefore();
                injectMapRunner.Inject(_injectController);
            }

            var injectRunnerGeneric = systems.GetRunner<IEcsInject<T>>();
            injectRunnerGeneric.Inject(_injectedData);

            if (_injectController.OnInject())
            {
                _injectController.Destroy();
                var injectCallbacksRunner = systems.GetRunner<IEcsPreInitInjectCallbacks>();
                injectCallbacksRunner.OnPreInitInjectionAfter();
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
        public static EcsSystems.Builder Inject<T>(this EcsSystems.Builder self, T data)
        {
            self.Add(new InjectSystem<T>(data));
            return self;
        }
        public static EcsSystems.Builder Inject<A, B>(this EcsSystems.Builder self, A a, B b)
        {
            self.Inject(a).Inject(b);
            return self;
        }
        public static EcsSystems.Builder Inject<A, B, C, D>(this EcsSystems.Builder self, A a, B b, C c, D d)
        {
            self.Inject(a).Inject(b).Inject(c).Inject(d);
            return self;
        }
        public static EcsSystems.Builder Inject<A, B, C, D, E>(this EcsSystems.Builder self, A a, B b, C c, D d, E e)
        {
            self.Inject(a).Inject(b).Inject(c).Inject(d).Inject(e);
            return self;
        }
        public static EcsSystems.Builder Inject<A, B, C, D, E, F>(this EcsSystems.Builder self, A a, B b, C c, D d, E e, F f)
        {
            self.Inject(a).Inject(b).Inject(c).Inject(d).Inject(e).Inject(f);
            return self;
        }
    }
}
