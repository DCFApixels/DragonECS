using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    public class InjectionGraph
    {
        internal const string CONFIG_NAME = "DCFApixels.DragonECS.DI:" + nameof(InjectionGraph);

        private EcsPipeline _pipeline;
        private Dictionary<Type, InjectionBranch> _branches = new Dictionary<Type, InjectionBranch>(32);
        private Dictionary<Type, InjectionNodeBase> _nodes = new Dictionary<Type, InjectionNodeBase>(32);
        private bool _isInit = false;

        public InjectionGraph()
        {
            Declare<object>();
            Declare<EcsWorld>();
        }
        public void Init(EcsPipeline pipeline)
        {
            if (_isInit)
            {
                throw new Exception("Already initialized");
            }
            _pipeline = pipeline;
            foreach (var node in _nodes.Values)
            {
                node.Init(pipeline);
            }
            _isInit = true;
        }
        public bool TryDeclare<T>()
        {
            Type type = typeof(T);
            if (_nodes.ContainsKey(type))
            {
                return false;
            }
            InitNode(new InjectionNode<T>(type));
            if (IsCanInstantiated(type))
            {
                InitBranch(new InjectionBranch(this, type, true));
            }
            return true;
        }
        public void Declare<T>()
        {
            if (TryDeclare<T>() == false)
            {
                throw new Exception();
            }
        }
        public void InjectNoBoxing<T>(T data)
        {
            _pipeline.GetRunner<IEcsInject<T>>().Inject(data);
        }
        public void Inject<T>(T obj)
        {
            Type type = typeof(T);
#if DEBUG
            if (obj.GetType() != type)
            {
                throw new ArgumentException();
            }
            if (IsCanInstantiated(type) == false)
            {
                throw new Exception();
            }
#endif
            if (_branches.TryGetValue(type, out InjectionBranch branch) == false)
            {
                InitNode(new InjectionNode<T>(type));
                branch = new InjectionBranch(this, type, true);
                InitBranch(branch);
            }
            branch.Inject(obj);
        }

#if !REFLECTION_DISABLED
        public void InjectRaw(object obj)
        {
            Type type = obj.GetType();
            if (_branches.TryGetValue(type, out InjectionBranch branch) == false)
            {
                branch = new InjectionBranch(this, type, false);
                InitBranch(branch);
            }
            branch.Inject(obj);
        }
#endif

        private void InitBranch(InjectionBranch branch)
        {
            _branches.Add(branch.Type, branch);
            foreach (var (type, node) in _nodes)
            {
                if (branch.Type.IsAssignableTo(type))
                {
                    branch.AddNode(node);
                }
            }
        }
        private void InitNode(InjectionNodeBase node)
        {
            if (_pipeline != null)
            {
                node.Init(_pipeline);
            }
            _nodes.Add(node.Type, node);
            foreach (var (type, branch) in _branches)
            {
                if (branch.Type.IsAssignableTo(type))
                {
                    branch.AddNode(node);
                }
            }
        }
        private bool IsCanInstantiated(Type type)
        {
            return !type.IsAbstract && !type.IsInterface;
        }
    }
}
