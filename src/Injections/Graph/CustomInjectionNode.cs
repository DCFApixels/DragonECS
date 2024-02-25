using System;

namespace DCFApixels.DragonECS
{
    public abstract class CustomInjectionNodeBase : InjectionNodeBase
    {
        protected CustomInjectionNodeBase(Type type) : base(type) { }
    }
    public abstract class CustomInjectionNode<TProcess, TType> : CustomInjectionNodeBase
        where TProcess : IEcsProcess
    {
        private EcsProcess<TProcess> _processProperty;
        private EcsProcess<IEcsInject<TType>> _process;
        public CustomInjectionNode() : base(typeof(TType)) { }
        public sealed override void Init(EcsPipeline pipeline)
        {
            _processProperty = pipeline.GetProcess<TProcess>();
            _process = pipeline.GetProcess<IEcsInject<TType>>();
            OnInitialized(pipeline);
        }
        public sealed override void Inject(object obj)
        {
            TType target = (TType)obj;
            for (int i = 0; i < _process.Length; i++)
            {
                _process[i].Inject(target);
            }
            for (int i = 0; i < _processProperty.Length; i++)
            {
                InjectTo(_processProperty[i], target);
            }
        }
        public virtual void OnInitialized(EcsPipeline pipeline) { }
        public abstract void InjectTo(TProcess system, TType obj);
    }
}
