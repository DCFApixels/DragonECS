#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public class Injector : IInjector
    {
        private EcsPipeline _pipeline;
        private Dictionary<Type, InjectionBranch> _branches = new Dictionary<Type, InjectionBranch>(32);
        private Dictionary<Type, InjectionNodeBase> _nodes = new Dictionary<Type, InjectionNodeBase>(32);
        private bool _isInit = false;

#if DEBUG
        private HashSet<Type> _requiredInjectionTypes = new HashSet<Type>();
#endif

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
            Type tType = typeof(T);
            Type objType = obj.GetType();
            if (_branches.TryGetValue(objType, out InjectionBranch branch) == false)
            {
                if (_nodes.ContainsKey(tType) == false)
                {
                    InitNode(new InjectionNode<T>());
                }
                bool hasNode = _nodes.ContainsKey(objType);
                if (hasNode == false && obj is IInjectionUnit unit)
                {
                    unit.InitInjectionNode(new InjectionGraph(this));
                    hasNode = _nodes.ContainsKey(objType);
                }

                branch = new InjectionBranch(this, objType);
                InitBranch(branch);

#if DEBUG
                foreach (var requiredInjectionType in _requiredInjectionTypes)
                {
                    if (requiredInjectionType.IsAssignableFrom(objType))
                    {
                        if (_nodes.ContainsKey(requiredInjectionType) == false)
                        {
                            throw new InjectionException($"A systems in the pipeline implements IEcsInject<{requiredInjectionType.Name}> interface, but no suitable injection node was found in the Injector. To create a node, use Injector.AddNode<{requiredInjectionType.Name}>() or implement the IInjectionUnit interface for type {objType.Name}.");
                        }
                    }
                }
#endif
            }
            branch.Inject(raw);
        }
        public void ExtractAllTo(object target)
        {
            if (target is IEcsInjectProcess == false) { return; }

            foreach (var node in _nodes)
            {
                node.Value.ExtractTo(target);
            }
        }
        public T Extract<T>()
        {
            return (T)Extract_Internal(typeof(T));
        }
        private object Extract_Internal(Type type)//TODO проверить
        {
            if (_nodes.TryGetValue(type, out InjectionNodeBase node))
            {
                return node.CurrentInjectedDependencyRaw;
            }
            throw new InjectionException($"The injection graph is missing a node for {type.Name} type. To create a node, use the Injector.AddNode<{type.Name}>() method directly in the injector or in the implementation of the IInjectionUnit for {type.Name}.");
        }
        public void AddNode<T>()
        {
            if (_nodes.ContainsKey(typeof(T)) == false)
            {
                InitNode(new InjectionNode<T>());
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
            if (_isInit) { Throw.Exception("Already initialized"); }

            _pipeline = pipeline;
            foreach (var pair in _nodes)
            {
                pair.Value.Init(pipeline);
            }
            _isInit = true;

#if DEBUG
            var systems = _pipeline.AllSystems;
            var injectType = typeof(IEcsInject<>);
            foreach (var system in systems)
            {
                var type = system.GetType();
                foreach (var requiredInjectionType in type.GetInterfaces().Where(o => o.IsGenericType && o.GetGenericTypeDefinition() == injectType).Select(o => o.GenericTypeArguments[0]))
                {
                    _requiredInjectionTypes.Add(requiredInjectionType);
                }
            }
#endif
        }
        private bool TryDeclare<T>()
        {
            Type type = typeof(T);
            if (_nodes.ContainsKey(type))
            {
                return false;
            }
            InitNode(new InjectionNode<T>());
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
            private EcsWorld _monoWorld;
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
                if (obj is EcsWorld objWorld)
                {
                    if (_monoWorld == null)
                    {
                        _monoWorld = objWorld;
                    }
                    else
                    {
                        Type monoWorldType = _monoWorld.GetType();
                        Type objWorldType = objWorld.GetType();
                        if (monoWorldType != objWorldType)
                        {
                            if (objWorldType == typeof(EcsWorld))
                            { // Екземпляр EcsWorld имеет самый больший приоритет.
                                _monoWorld = objWorld;
                            }
                            if (objWorldType == typeof(EcsDefaultWorld) &&
                                monoWorldType != typeof(EcsWorld))
                            { // Екземпляр EcsDefaultWorld имеет приоритет больше других типов, но меньше приоритета EcsWorld.
                                _monoWorld = objWorld;
                            }
                        }
                    }
                }
                _initInjections.Add(new InitInject<T>(obj));
                return _source;
            }
            public EcsPipeline.Builder Extract<T>(ref T obj) // TODO проверить
            {
                Type type = typeof(T);
                for (int i = _initInjections.Count - 1; i >= 0; i--)
                {
                    var item = _initInjections[i];
                    if (type.IsAssignableFrom(item.Type))
                    {
                        obj = (T)item.Raw;
                        return _source;
                    }
                }
                Throw.UndefinedException();
                return default;
            }
            public Injector Build(EcsPipeline pipeline)
            {
                var monoWorldProcess = pipeline.GetProcess<IMonoWorldInject>(); // TODO Проверить IMonoWorldInject
                foreach (var monoWorldSystem in monoWorldProcess)
                {
                    monoWorldSystem.World = _monoWorld;
                }


                var initInjectionCallbacks = pipeline.GetProcess<IOnInitInjectionComplete>();
                foreach (var system in initInjectionCallbacks)
                {
                    system.OnBeforeInitInjection();
                }
                _instance.Init(pipeline);
                foreach (var item in _initInjections)
                {
                    item.InjectTo(_instance);
                }
                foreach (var system in initInjectionCallbacks)
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