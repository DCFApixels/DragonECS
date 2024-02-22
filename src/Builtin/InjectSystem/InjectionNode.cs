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
    public class InjectionNode<T> : InjectionNodeBase
    {
        private EcsProcess<IEcsInject<T>> _process;
        public InjectionNode(Type type) : base(type) { }
        public override void Init(EcsPipeline pipeline)
        {
            _process = pipeline.GetProcess<IEcsInject<T>>();
        }
        public override void Inject(object obj)
        {
            for (int i = 0; i < _process.Length; i++)
            {
                _process[i].Inject((T)obj);
            }
        }
    }
}
