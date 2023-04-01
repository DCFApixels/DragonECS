﻿using DCFApixels.DragonECS.Internal;
using System.Linq;

namespace DCFApixels.DragonECS
{
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