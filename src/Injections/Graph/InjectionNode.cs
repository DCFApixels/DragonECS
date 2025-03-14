#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public abstract class InjectionNodeBase
    {
        private readonly Type _type;
        public Type Type { get { return _type; } }
        public abstract object CurrentInjectedDependencyRaw { get; }
        protected InjectionNodeBase(Type type) { _type = type; }
        public abstract void Inject(object obj);
        public abstract void ExtractTo(object target);
        public abstract void Init(EcsPipeline pipeline);
    }
}
namespace DCFApixels.DragonECS.Internal
{
    internal sealed class InjectionNode<T> : InjectionNodeBase
    {
        private EcsPipeline _pipeline;
        private EcsProcess<IEcsInject<T>> _process;
        private T _currentInjectedDependency;
        public sealed override object CurrentInjectedDependencyRaw
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _currentInjectedDependency; }
        }
        public T CurrentInjectedDependency
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _currentInjectedDependency; }
        }
        public InjectionNode() : base(typeof(T)) { }
        public sealed override void Init(EcsPipeline pipeline)
        {
            _pipeline = pipeline;
            _process = pipeline.GetProcess<IEcsInject<T>>();
        }
        public sealed override void Inject(object raw)
        {
            T obj = (T)raw;
            _currentInjectedDependency = obj;
            for (int i = 0; i < _process.Length; i++)
            {
                _process[i].Inject(obj);
            }
            foreach (var runner in _pipeline.AllRunners)
            {
                ExtractTo_Internal(runner.Value);
            }
        }
        public sealed override void ExtractTo(object target)
        {
            if (_currentInjectedDependency == null) { return; }
            ExtractTo_Internal(target);
        }
        private void ExtractTo_Internal(object target)
        {
            var type = target.GetType();
            var intrfs = type.GetInterfaces();
            if (target is IEcsInject<T> intrf)
            {
                intrf.Inject(_currentInjectedDependency);
            }
        }
    }
}
