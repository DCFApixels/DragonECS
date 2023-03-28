using System.Collections.Generic;
using DCFApixels.DragonECS.Internal;
using System.Linq;

namespace DCFApixels.DragonECS
{

    namespace Internal
    {
        internal class InjectController 
        {
            private EcsSystems _source;
            private InjectSystemBase[] _injectSystems;
            private int _injectCount;

            public InjectController(EcsSystems source)
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


    public interface IEcsInject : IEcsSystem
    {
        public void Inject(object obj);
    }
    [DebugColor(DebugColor.Gray)]
    public sealed class InjectRunner : EcsRunner<IEcsInject>, IEcsInject
    {
        void IEcsInject.Inject(object obj)
        {
            foreach (var item in targets) item.Inject(obj);
        }
    }

    public interface IEcsInject<T> : IEcsSystem
    {
        public void Inject(T obj);
    }
    [DebugColor(DebugColor.Gray)]
    public sealed class InjectRunner<T> : EcsRunner<IEcsInject<T>>, IEcsInject<T>
    {
        void IEcsInject<T>.Inject(T obj)
        {
            foreach (var item in targets) item.Inject(obj);
        }
    }

    public interface IEcsInjectCallbacks : IEcsSystem
    {
        public void OnInjectionBefore();
        public void OnInjectionAfter();
    }
    [DebugColor(DebugColor.Gray)]
    public sealed class InjectCallbacksRunner : EcsRunner<IEcsInjectCallbacks>, IEcsInjectCallbacks
    {
        public void OnInjectionAfter()
        {
            foreach (var item in targets) item.OnInjectionAfter();
        }
        public void OnInjectionBefore()
        {
            foreach (var item in targets) item.OnInjectionBefore();
        }
    }

    public class InjectSystemBase
    {
        private static int _injectSystemID = EcsDebug.RegisterMark("InjectSystem");

        protected static void ProfileMarkerBegin()
        {
            EcsDebug.ProfileMarkBegin(_injectSystemID);
        }
        protected static void ProfileMarkerEnd()
        {
            EcsDebug.ProfileMarkEnd(_injectSystemID);
        }
    }

    [DebugColor(DebugColor.Gray)]
    public class InjectSystem<T> : InjectSystemBase, IEcsPreInitSystem, IEcsInject<InjectController>, IEcsInjectCallbacks
    {
        private T _injectedData;

        private InjectController _injectController;
        void IEcsInject<InjectController>.Inject(InjectController obj) => _injectController = obj;

        public InjectSystem(T injectedData)
        {
            _injectedData = injectedData;
        }

        public void PreInit(EcsSystems systems)
        {
            if(_injectController == null)
            {
                ProfileMarkerBegin();

                _injectController = new InjectController(systems);
                var injectMapRunner = systems.GetRunner<IEcsInject<InjectController>>();
                systems.GetRunner<IEcsInjectCallbacks>().OnInjectionBefore();
                injectMapRunner.Inject(_injectController);
            }

            var injectRunnerGeneric = systems.GetRunner<IEcsInject<T>>();
            var injectRunner = systems.GetRunner<IEcsInject>();
            injectRunnerGeneric.Inject(_injectedData);
            injectRunner.Inject(_injectedData);

            if (_injectController.OnInject())
            {
                _injectController.Destroy();
                var injectCallbacksRunner = systems.GetRunner<IEcsInjectCallbacks>();
                injectCallbacksRunner.OnInjectionAfter();
                ProfileMarkerEnd();
            }
        }

        public void OnInjectionBefore() { }

        public void OnInjectionAfter()
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
