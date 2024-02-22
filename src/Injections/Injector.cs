using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    [MetaName(nameof(Inject))]
    public interface IEcsInject<T> : IEcsProcess
    {
        void Inject(T obj);
    }
    public class Injector
    {
        private EcsPipeline _pipeline;
        private Dictionary<Type, InjectionBranch> _branches = new Dictionary<Type, InjectionBranch>(32);
        private Dictionary<Type, InjectionNodeBase> _nodes = new Dictionary<Type, InjectionNodeBase>(32);
        private bool _isInit = false;

        private Injector() { }
        
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
//        public void InjectNoBoxing<T>(T data) where T : struct
//        {
//            foreach (var system in _pipeline.GetProcess<IEcsInject<T>>())
//            {
//                system.Inject(data);
//            }
//        }
//#if !REFLECTION_DISABLED
//        public void InjectRaw(object obj)
//        {
//            Type type = obj.GetType();
//            if (_branches.TryGetValue(type, out InjectionBranch branch) == false)
//            {
//                branch = new InjectionBranch(this, type, false);
//                InitBranch(branch);
//            }
//            branch.Inject(obj);
//        }
//#endif

        #region Internal
        private void InitBranch(InjectionBranch branch)
        {
            _branches.Add(branch.Type, branch);
            foreach (var item in _nodes)
            {
                var type = item.Key;
                var node = item.Value;
                if (type.IsAssignableFrom(branch.Type))
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
            foreach (var item in _branches)
            {
                var type = item.Key;
                var branch = item.Value;
                if (type.IsAssignableFrom(branch.Type))
                {
                    branch.AddNode(node);
                }
            }
        }
        private bool IsCanInstantiated(Type type)
        {
            return !type.IsAbstract && !type.IsInterface;
        }
        #endregion

        #region Build
        private void Init(EcsPipeline pipeline)
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
        private bool TryDeclare<T>()
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
        public class Builder
        {
            private EcsPipeline.Builder _source;
            private Injector _instance;
            private List<InitInjectBase> _initInjections = new List<InitInjectBase>(16);
            internal Builder(EcsPipeline.Builder source)
            {
                _source = source;
                _instance = new Injector();
            }
            public EcsPipeline.Builder Declare<T>()
            {
                _instance.TryDeclare<T>();
                return _source;
            }
            public void Inject<T>(T obj)
            {
                _initInjections.Add(new InitInject<T>(obj));
            }
            public Injector Build(EcsPipeline pipeline)
            {
                _instance.Init(pipeline);
                foreach (var item in _initInjections)
                {
                    item.InjectTo(_instance);
                }
                return _instance;
            }

            private abstract class InitInjectBase
            {
                public abstract void InjectTo(Injector instance);
            }
            private sealed class InitInject<T> : InitInjectBase
            {
                private T _injectedData;
                public InitInject(T injectedData)
                {
                    _injectedData = injectedData;
                }
                public override void InjectTo(Injector instance)
                {
                    instance.Inject<T>(_injectedData);
                }
            }
        }
        #endregion
    }
}
