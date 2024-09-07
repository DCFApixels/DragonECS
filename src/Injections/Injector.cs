using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public class Injector : IInjector
    {
        private EcsPipeline _pipeline;
        private Dictionary<Type, InjectionBranch> _branches = new Dictionary<Type, InjectionBranch>(32);
        private Dictionary<Type, InjectionNodeBase> _nodes = new Dictionary<Type, InjectionNodeBase>(32);
        private bool _isInit = false;

        public EcsPipeline Pipelie
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _pipeline; }
        }

        private Injector() { }

        #region Inject/Extract/AddNode
        public void Inject<T>(T obj)
        {
            object raw = obj;
            Type type = obj.GetType();
            if (_branches.TryGetValue(type, out InjectionBranch branch) == false)
            {
                if (typeof(T) == type)
                {
                    if (_nodes.ContainsKey(type) == false)
                    {
                        InitNode(new InjectionNode<T>(type));
                    }
                    branch = new InjectionBranch(this, type);
                    InitBranch(branch);
                }
                else
                {
                    bool hasNode = _nodes.ContainsKey(type);
                    if (hasNode == false && obj is IInjectionUnit unit)
                    {
                        unit.OnInitInjectionBranch(new InjectionBranchIniter(this));
                        hasNode = _nodes.ContainsKey(type);
                    }
                    if (hasNode)
                    {
                        branch = new InjectionBranch(this, type);
                        InitBranch(branch);
                    }
                    else
                    {
                        //TODO переработать это исключение
                        // идея следующая, в режиме дебага с помощью рефлекшена собрать информацию о системах в которых есть IEcsInject, собрать все типы которые принимают системы,
                        // потом при инициирующих инъекциях проверить что во все собранные типы были заинжектены. Если нет, то только тогда бросать исключение.
                        // Исключения можно заранее определять и собирать, а бросать на моменте. Например тут создать исключение, и если инхекции небыло то бросить его.
                        // Дополнительно обернуть все в #if DEBUG
                        throw new EcsInjectionException($"To create an injection branch, no injection node of {type.Name} was found. To create a node, use the AddNode<{type.Name}>() method directly in the injector or in the implementation of the IInjectionUnit for {type.Name}.");
                    }
                }
            }
            branch.Inject(raw);
        }
        public T Extract<T>()
        {
            return (T)Extract_Internal(typeof(T));
        }
        private object Extract_Internal(Type type)
        {
            if (_branches.TryGetValue(type, out InjectionBranch branch))
            {
                return branch.CurrentInjectedDependency;
            }
            return null;

            //if (_nodes.ContainsKey(type))
            //{
            //    return null;
            //}
            //throw new EcsInjectionException($"The injection graph is missing a node for {type.Name} type. To create a node, use the AddNode<{type.Name}>() method directly in the injector or in the implementation of the IInjectionUnit for {type.Name}.");
        }
        public void AddNode<T>()
        {
            Type type = typeof(T);
            if (_nodes.ContainsKey(type) == false)
            {
                InitNode(new InjectionNode<T>(type));
            }
        }
        #endregion

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
                //var type = item.Key;
                var branch = item.Value;
                if (node.Type.IsAssignableFrom(branch.Type))
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
#if !REFLECTION_DISABLED
            if (IsCanInstantiated(type))
#endif
            {
                InitBranch(new InjectionBranch(this, type));
            }
            return true;
        }

        public class Builder : IInjector
        {
            private EcsPipeline.Builder _source;
            private Injector _instance;
            private List<InitInjectBase> _initInjections = new List<InitInjectBase>(16);
            internal Builder(EcsPipeline.Builder source)
            {
                _source = source;
                _instance = new Injector();
            }
            public EcsPipeline.Builder AddNode<T>()
            {
                _instance.TryDeclare<T>();
                return _source;
            }
            public EcsPipeline.Builder Inject<T>(T obj)
            {
                _initInjections.Add(new InitInject<T>(obj));
                return _source;
            }
            public EcsPipeline.Builder Extract<T>(ref T obj)
            {
                Type type = typeof(T);
                for (int i = _initInjections.Count - 1; i >= 0; i--)
                {
                    var item = _initInjections[i];
                    if (item.Type.IsAssignableFrom(type))
                    {
                        obj = (T)item.Raw;
                        return _source;
                    }
                }
                Throw.UndefinedException();
                return _source;
            }
            public Injector Build(EcsPipeline pipeline)
            {
                _instance.Init(pipeline);
                foreach (var item in _initInjections)
                {
                    item.InjectTo(_instance);
                }
                foreach (var system in pipeline.GetProcess<IOnInitInjectionComplete>())
                {
                    system.OnInitInjectionComplete();
                }
                return _instance;
            }
            public void Add(Builder other)
            {
                foreach (var item in other._initInjections)
                {
                    _initInjections.Add(item);
                }
            }

            void IInjector.Inject<T>(T obj) { Inject(obj); }
            T IInjector.Extract<T>()
            {
                T result = default;
                Extract(ref result);
                return result;
            }

            private abstract class InitInjectBase
            {
                public abstract Type Type { get; }
                public abstract object Raw { get; }
                public abstract void InjectTo(Injector instance);
            }
            private sealed class InitInject<T> : InitInjectBase
            {
                private T _injectedData;
                public override Type Type { get { return typeof(T); } }
                public override object Raw { get { return _injectedData; } }
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