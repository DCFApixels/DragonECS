using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{

    public interface IEcsInterfacePool<T> : IEcsReadonlyPool where T : class
    {
        T Get(int entityID);
    }
    internal interface IEcsInterfacePoolInternal
    {
        void Add(int entityID, object component);
        void Del(int entityID);
    }
    public interface IEcsInterfaceComponent { }
    public class EcsInterfacePool<T> : IEcsPoolImplementation<T>, IEcsInterfacePool<T>, IEcsInterfacePoolInternal, IEnumerable<T> //IEnumerable<T> - IntelliSense hack
        where T : class, IEcsInterfaceComponent
    {
        private EcsWorld _source;
        private int _componentTypeID;

        private int[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID
        private T[] _items; //dense
        private int _itemsCount;
        private int[] _recycledItems;
        private int _recycledItemsCount;

        private List<IEcsPoolEventListener> _listeners = new List<IEcsPoolEventListener>();

        private EcsWorld.PoolsMediator _mediator;

        #region Properties
        public int ComponentTypeID
        {
            get { return _componentTypeID; }
        }
        public Type ComponentType
        {
            get { return typeof(T); }
        }
        public EcsWorld World
        {
            get { return _source; }
        }
        public int Count
        {
            get { return _itemsCount; }
        }
        public bool IReadOnly
        {
            get { return true; }
        }
        #endregion

        #region Methdos
        public T Get(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) { EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID); }
#endif
            _listeners.InvokeOnGet(entityID);
            return _items[_mapping[entityID]];
        }
        public bool Has(int entityID)
        {
            return _mapping[entityID] > 0;
        }
        #endregion

        #region IEcsInterfacePoolInternal
        void IEcsInterfacePoolInternal.Add(int entityID, object component)
        {

        }
        void IEcsInterfacePoolInternal.Del(int entityID)
        {

        }
        #endregion

        #region Other
        object IEcsReadonlyPool.GetRaw(int entityID)
        {
            return Get(entityID);
        }
        public void Copy(int fromEntityID, int toEntityID) { }
        public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID) { }
        void IEcsPool.AddRaw(int entityID, object dataRaw)
        {
            EcsDebug.PrintWarning("Is read only!");
        }
        void IEcsPool.SetRaw(int entityID, object dataRaw)
        {
            EcsDebug.PrintWarning("Is read only!");
        }
        void IEcsPool.Del(int entityID)
        {
            EcsDebug.PrintWarning("Is read only!");
        }
        #endregion

        #region Callbacks
        void IEcsPoolImplementation.OnInit(EcsWorld world, EcsWorld.PoolsMediator mediator, int componentTypeID)
        {
            throw new NotImplementedException();
        }
        void IEcsPoolImplementation.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
        {
            throw new NotImplementedException();
        }
        void IEcsPoolImplementation.OnWorldResize(int newSize)
        {
            throw new NotImplementedException();
        }
        void IEcsPoolImplementation.OnWorldDestroy()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Listeners
        public void AddListener(IEcsPoolEventListener listener)
        {
            if (listener == null) { throw new ArgumentNullException("listener is null"); }
            _listeners.Add(listener);
        }
        public void RemoveListener(IEcsPoolEventListener listener)
        {
            if (listener == null) { throw new ArgumentNullException("listener is null"); }
            _listeners.Remove(listener);
        }
        #endregion

        #region IEnumerator - IntelliSense hack
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { throw new NotImplementedException(); }
        IEnumerator IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
        #endregion
    }
}











namespace DCFApixels.DragonECS.Internal
{
    internal readonly struct InterfacePoolGraphCmp : IEcsWorldComponent<InterfacePoolGraphCmp>
    {
        public readonly InterfacePoolGraph Graph;
        private InterfacePoolGraphCmp(EcsWorld world)
        {
            Graph = new InterfacePoolGraph(world);
        }
        public void Init(ref InterfacePoolGraphCmp component, EcsWorld world)
        {
            component = new InterfacePoolGraphCmp(world);
        }
        public void OnDestroy(ref InterfacePoolGraphCmp component, EcsWorld world)
        {
            component = default;
        }
    }
    public class InterfacePoolGraph
    {
        private EcsWorld _world;
        private Dictionary<Type, InterfacePoolBranch> _branches = new Dictionary<Type, InterfacePoolBranch>();

        public InterfacePoolGraph(EcsWorld world)
        {
            _world = world;
        }

        public bool IsInstantiable(Type type)
        {
            return _branches.ContainsKey(type);
        }
        public bool TryGetBranch(Type type, out InterfacePoolBranch branch)
        {
            return _branches.TryGetValue(type, out branch);
        }
        public void InitNewInterfacePool(IEcsHybridPoolInternal pool)
        {
            foreach (var pair in _branches)
            {
                var type = pair.Key;
                var branch = pair.Value;
                if (type.IsAssignableFrom(pool.ComponentType))
                {
                    if (type == pool.ComponentType)
                    {
                        branch.InitRootTypePool(pool);
                    }
                    else
                    {
                        branch.InitNewPool(pool);
                    }
                }
            }
        }

        public InterfacePoolBranch GetBranch(Type targetType)
        {
            if (_branches.TryGetValue(targetType, out InterfacePoolBranch branch) == false)
            {
                branch = new InterfacePoolBranch(_world, targetType, null);
                _branches.Add(targetType, branch);
            }
            return branch;
        }
    }
    public class InterfacePoolBranch
    {
        private EcsWorld _world;

        private Type _rootComponentType;
        private int _rootComponentTypeID;
        private IEcsHybridPoolInternal _rootTypePool;
        private List<IEcsHybridPoolInternal> _relatedPools = new List<IEcsHybridPoolInternal>();

        private VirtualHybridPool _virtualPoolRef;
        private bool _isVirtualPool = false;

        public bool IsVirtualPool
        {
            get { return _isVirtualPool; }
        }

        public HybridPoolBranch(EcsWorld world, Type rootComponentType, IEcsHybridPoolInternal rootTypePool)
        {
            _world = world;

            _rootComponentType = rootComponentType;
            _rootComponentTypeID = world.GetComponentTypeID(rootComponentType);

            if (rootTypePool == null)
            {
                _virtualPoolRef = new VirtualHybridPool(world, rootComponentType);
                rootTypePool = _virtualPoolRef;
                _isVirtualPool = true;
            }
            _rootTypePool = rootTypePool;
        }


        public void InitRootTypePool(IEcsHybridPoolInternal rootTypePool)
        {
            if (_isVirtualPool == false)
            {
                Throw.UndefinedException();
            }
            _isVirtualPool = false;
            rootTypePool.Devirtualize(_virtualPoolRef);
            _rootTypePool = rootTypePool;
            _virtualPoolRef = null;
        }
        public void InitNewPool(IEcsHybridPoolInternal pool)
        {
            _relatedPools.Add(pool);
        }

        public void Set(int entityID, object component)
        {
            throw new NotImplementedException();
        }
        public void Add(int entityID, object component)
        {
            _rootTypePool.AddRefInternal(entityID, component, true);
            foreach (var pool in _relatedPools)
            {
                pool.AddRefInternal(entityID, component, false);
            }
        }
        public void Del(int entityID)
        {
            _rootTypePool.DelInternal(entityID, true);
            foreach (var pool in _relatedPools)
            {
                pool.DelInternal(entityID, false);
            }
        }
    }
}
