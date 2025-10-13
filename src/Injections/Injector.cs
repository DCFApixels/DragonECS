#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public class Injector : IInjector
    {
        private readonly EcsPipeline _pipeline;
        private readonly Dictionary<Type, InjectionBranch> _branches = new Dictionary<Type, InjectionBranch>(32);
        private readonly Dictionary<Type, InjectionNodeBase> _nodes = new Dictionary<Type, InjectionNodeBase>(32);
        private ReadOnlySpan<InjectionNodeBase> GetNodes(Type type)
        {
            if (_branches.TryGetValue(type, out InjectionBranch branch))
            {
                return branch.Nodes;
            }
            return Array.Empty<InjectionNodeBase>();
        }

        #region InjectionTempHistory
        private StructList<object> _injectionTempHistory = new StructList<object>(32);
        private int _injectionTempHistoryReadersCount = 0;
        private int StartReadHistory_Internal()
        {
            _injectionTempHistoryReadersCount++;
            return _injectionTempHistory.Count;
        }
        private ReadOnlySpan<object> EndReadHistory_Internal(int startIndex)
        {
            _injectionTempHistoryReadersCount--;
            if (_injectionTempHistoryReadersCount < 0)
            {
                Throw.OpeningClosingMethodsBalanceError();
            }
            var result = _injectionTempHistory.AsReadOnlySpan().Slice(startIndex);
            if (_injectionTempHistoryReadersCount == 0)
            {
                _injectionTempHistory.Recreate();
            }
            return result;
        }
        public readonly struct InjectionHistorySpanReader
        {
            private readonly Injector _injector;
            private readonly int _startIndex;
            public InjectionHistorySpanReader(Injector injector)
            {
                _injector = injector;
                _startIndex = _injector.StartReadHistory_Internal();
            }
            public ReadOnlySpan<object> StopReadAndGetHistorySpan()
            {
                return _injector.EndReadHistory_Internal(_startIndex);
            }
        }
        public InjectionHistorySpanReader StartReadHistory()
        {
            return new InjectionHistorySpanReader(this);
        }
        #endregion

        public EcsPipeline Pipelie
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _pipeline; }
        }

        public Injector(EcsPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        #region Inject/Extract/AddNode
        public void Inject<T>(T obj)
        {
            Type tType = typeof(T);
            Type objType = obj.GetType();
            if (_branches.TryGetValue(objType, out InjectionBranch branch) == false)
            {
                if (_nodes.ContainsKey(tType) == false)
                {
                    InitNode(new InjectionNode<T>());
                }
                bool hasObjTypeNode = _nodes.ContainsKey(objType);
                if (hasObjTypeNode == false && obj is IInjectionUnit unit)
                {
                    unit.InitInjectionNode(new InjectionGraph(this));
                    hasObjTypeNode = _nodes.ContainsKey(objType);
                }

                branch = new InjectionBranch(objType);
                InitBranch(branch);
            }


            var branchNodes = branch.Nodes;
            for (int i = 0; i < branchNodes.Length; i++)
            {
                branchNodes[i].Inject(obj);
            }
            if (_injectionTempHistoryReadersCount > 0)
            {
                _injectionTempHistory.Add(obj);
            }
            if (obj is IInjectionBlock block)
            {
                block.InjectTo(this);
            }
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
        public bool AddNode<T>()
        {
            if (_nodes.ContainsKey(typeof(T))) { return false; }
            InitNode(new InjectionNode<T>());
            return true;
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
        #endregion

        #region InjectionList
        public class InjectionList : IInjector
        {
            public static readonly InjectionList _Empty_Internal = new InjectionList();

            private StructList<InjectionBase> _injections = new StructList<InjectionBase>(32);
            private StructList<NodeBase> _nodes = new StructList<NodeBase>(32);
            private EcsWorld _monoWorld;
            public void AddNode<T>()
            {
                _nodes.Add(new Node<T>());
            }
            public void Inject<T>(T obj)
            {
                FindMonoWorld(obj);
                _injections.Add(new Injection<T>(obj));
            }
            public void Extract<T>(ref T obj) // TODO проверить
            {
                Type type = typeof(T);
                for (int i = _injections.Count - 1; i >= 0; i--)
                {
                    var item = _injections[i];
                    if (type.IsAssignableFrom(item.Type))
                    {
                        obj = (T)item.Raw;
                        return;
                    }
                }
                Throw.UndefinedException();
            }
            public void MergeWith(InjectionList other)
            {
                foreach (var item in other._injections)
                {
                    FindMonoWorld(item);
                    _injections.Add(item);
                }
                foreach (var item in other._nodes)
                {
                    _nodes.Add(item);
                }
            }
            public void InitInjectTo(Injector injector, EcsPipeline pipeline)
            {
#if DEBUG
                HashSet<Type> requiredInjectionTypes = new HashSet<Type>();
                var systems = pipeline.AllSystems;
                var injectType = typeof(IEcsInject<>);
                foreach (var system in systems)
                {
                    var type = system.GetType();
                    foreach (var requiredInjectionType in type.GetInterfaces().Where(o => o.IsGenericType && o.GetGenericTypeDefinition() == injectType).Select(o => o.GenericTypeArguments[0]))
                    {
                        requiredInjectionTypes.Add(requiredInjectionType);
                    }
                }
                var reader = injector.StartReadHistory();
#endif


                var initInjectionCallbacks = pipeline.GetProcess<IOnInitInjectionComplete>();
                foreach (var system in initInjectionCallbacks)
                {
                    system.OnBeforeInitInjection();
                }

                injector.Inject(pipeline);
                injector.AddNode<object>();
                InjectTo(injector, pipeline);

                foreach (var system in initInjectionCallbacks)
                {
                    system.OnInitInjectionComplete();
                }


#if DEBUG
                var injectionHistory = reader.StopReadAndGetHistorySpan();
                foreach (var injection in injectionHistory)
                {
                    foreach (var node in injector.GetNodes(injection.GetType()))
                    {
                        requiredInjectionTypes.Remove(node.Type);
                    }
                }
                if (requiredInjectionTypes.Count > 0)
                {
                    foreach (var requiredInjectionType in requiredInjectionTypes)
                    {
                        throw new InjectionException($"A systems in the pipeline implements IEcsInject<{requiredInjectionType.Name}> interface, but no suitable injection node was found in the Injector. To create a node, use Injector.AddNode<{requiredInjectionType.Name}>() or implement the IInjectionUnit interface for the type being injected.");
                    }
                }
#endif
            }
            public void InjectTo(Injector injector, EcsPipeline pipeline)
            {
                var monoWorldProcess = pipeline.GetProcess<IMonoWorldInject>(); // TODO Проверить IMonoWorldInject
                foreach (var monoWorldSystem in monoWorldProcess)
                {
                    monoWorldSystem.World = _monoWorld;
                }
                foreach (var item in _nodes)
                {
                    item.AddNodeTo(injector);
                }
                foreach (var item in _injections)
                {
                    item.InjectTo(injector);
                }
            }

            private void FindMonoWorld(object obj)
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
            }

            void IInjector.Inject<T>(T obj) { Inject(obj); }
            T IInjector.Extract<T>()
            {
                T result = default;
                Extract(ref result);
                return result;
            }

            private abstract class NodeBase
            {
                public abstract Type Type { get; }
                public abstract void AddNodeTo(Injector instance);
            }
            private sealed class Node<T> : NodeBase
            {
                public override Type Type { get { return typeof(T); } }
                public override void AddNodeTo(Injector instance)
                {
                    instance.AddNode<T>();
                }
            }

            private abstract class InjectionBase
            {
                public abstract Type Type { get; }
                public abstract object Raw { get; }
                public abstract void InjectTo(Injector instance);
            }
            private sealed class Injection<T> : InjectionBase
            {
                private T _injectedData;
                public override Type Type { get { return typeof(T); } }
                public override object Raw { get { return _injectedData; } }
                public Injection(T injectedData)
                {
                    _injectedData = injectedData;
                }
                public override void InjectTo(Injector instance)
                {
                    instance.Inject(_injectedData);
                }
            }
        }
        #endregion
    }
}