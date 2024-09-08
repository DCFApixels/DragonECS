using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public abstract class InjectionNodeBase
    {
        private readonly Type _type;
        public Type Type
        {
            get { return _type; }
        }
        public abstract object CurrentInjectedDependencyRaw { get; }
        protected InjectionNodeBase(Type type)
        {
            _type = type;
        }
        public abstract void Inject(object obj);
        public abstract void Init(EcsPipeline pipeline);
    }
}
namespace DCFApixels.DragonECS.Internal
{
    internal sealed class InjectionNode<T> : InjectionNodeBase
    {
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
        }
    }
}
