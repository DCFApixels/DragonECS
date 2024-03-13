using System;

namespace DCFApixels.DragonECS
{
    public abstract class InjectionNodeBase
    {
        private readonly Type _type;
        public Type Type
        {
            get { return _type; }
        }
        protected InjectionNodeBase(Type type)
        {
            _type = type;
        }
        public abstract void Inject(object obj);
        public abstract void Init(EcsPipeline pipeline);
    }
    public sealed class InjectionNode<T> : InjectionNodeBase
    {
        private EcsProcess<IEcsInject<T>> _process;
        public InjectionNode(Type type) : base(type) { }
        public sealed override void Init(EcsPipeline pipeline)
        {
            _process = pipeline.GetProcess<IEcsInject<T>>();
        }
        public sealed override void Inject(object raw)
        {
            T obj = (T)raw;
            for (int i = 0; i < _process.Length; i++)
            {
                _process[i].Inject(obj);
            }
        }
    }
}
