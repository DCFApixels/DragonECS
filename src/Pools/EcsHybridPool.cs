using DCFApixels.DragonECS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    /// <summary>Pool for IEcsHybridComponent components</summary>
    public sealed class EcsHybridPool<T> : IEcsPoolImplementation<T>, IEcsHybridPool<T>, IEcsHybridPoolInternal, IEnumerable<T> //IEnumerable<T> - IntelliSense hack
        where T : class, IEcsHybridComponent
    {
        private EcsWorld _source;
        private int _componentTypeID;
        private EcsMaskChunck _maskBit;

        private int[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID
        private T[] _items; //dense
        private int[] _entities;
        private int _itemsCount;

        private int[] _recycledItems;
        private int _recycledItemsCount;

        private List<IEcsPoolEventListener> _listeners = new List<IEcsPoolEventListener>();

        private EcsWorld.PoolsMediator _mediator;

        private HybridPoolGraph _graph;

        #region Properites
        public int Count
        {
            get { return _itemsCount; }
        }
        public int Capacity
        {
            get { return _items.Length; }
        }
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
        public bool IReadOnly
        {
            get { return false; }
        }
        #endregion

        #region Methods
        void IEcsHybridPoolInternal.AddRefInternal(int entityID, object component, bool isMain)
        {
            AddInternal(entityID, (T)component, isMain);
        }
        private void AddInternal(int entityID, T component, bool isMain)
        {
            ref int itemIndex = ref _mapping[entityID];
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (itemIndex > 0) EcsPoolThrowHalper.ThrowAlreadyHasComponent<T>(entityID);
#endif
            if (_recycledItemsCount > 0)
            {
                itemIndex = _recycledItems[--_recycledItemsCount];
                _itemsCount++;
            }
            else
            {
                itemIndex = ++_itemsCount;
                if (itemIndex >= _items.Length)
                {
                    Array.Resize(ref _items, _items.Length << 1);
                    Array.Resize(ref _entities, _items.Length);
                }
            }
            _mediator.RegisterComponent(entityID, _componentTypeID, _maskBit);
            _listeners.InvokeOnAdd(entityID);
            if (isMain)
            {
                component.OnAddToPool(_source.GetEntityLong(entityID));
            }
            _items[itemIndex] = component;
            _entities[itemIndex] = entityID;
        }
        public void Add(int entityID, T component)
        {
            //HybridMapping mapping = _source.GetHybridMapping(component.GetType());
            //mapping.GetTargetTypePool().AddRefInternal(entityID, component, true);
            //foreach (var pool in mapping.GetPools())
            //{
            //    pool.AddRefInternal(entityID, component, false);
            //}
            _graph.GetBranch(component.GetType()).Add(entityID, component);
        }
        public void Set(int entityID, T component)
        {
            if (Has(entityID))
            {
                Del(entityID);
            }
            Add(entityID, component);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID);
#endif
            _listeners.InvokeOnGet(entityID);
            return _items[_mapping[entityID]];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID);
#endif
            return ref _items[_mapping[entityID]];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            return _mapping[entityID] > 0;
        }
        void IEcsHybridPoolInternal.DelInternal(int entityID, bool isMain)
        {
            DelInternal(entityID, isMain);
        }
        private void DelInternal(int entityID, bool isMain)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID);
#endif
            ref int itemIndex = ref _mapping[entityID];
            T component = _items[itemIndex];
            if (isMain)
            {
                component.OnDelFromPool(_source.GetEntityLong(entityID));
            }
            if (_recycledItemsCount >= _recycledItems.Length)
            {
                Array.Resize(ref _recycledItems, _recycledItems.Length << 1);
            }
            _recycledItems[_recycledItemsCount++] = itemIndex;
            _mapping[entityID] = 0;
            _entities[itemIndex] = 0;
            _itemsCount--;
            _mediator.UnregisterComponent(entityID, _componentTypeID, _maskBit);
            _listeners.InvokeOnDel(entityID);
        }
        public void Del(int entityID)
        {
            var component = Get(entityID);
            HybridMapping mapping = _source.GetHybridMapping(component.GetType());
            mapping.GetTargetTypePool().DelInternal(entityID, true);
            foreach (var pool in mapping.GetPools())
            {
                pool.DelInternal(entityID, false);
            }
        }
        public void TryDel(int entityID)
        {
            if (Has(entityID)) Del(entityID);
        }
        public void Copy(int fromEntityID, int toEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(fromEntityID)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(fromEntityID);
#endif
            Set(toEntityID, Get(fromEntityID));
        }
        public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(fromEntityID)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(fromEntityID);
#endif
            toWorld.GetPool<T>().Set(toEntityID, Get(fromEntityID));
        }

        public void ClearNotAliveComponents()
        {
            for (int i = _itemsCount - 1; i >= 0; i--)
            {
                if (!_items[i].IsAlive)
                {
                    Del(_entities[i]);
                }
            }
        }
        #endregion

        #region Callbacks
        void IEcsPoolImplementation.OnInit(EcsWorld world, EcsWorld.PoolsMediator mediator, int componentTypeID)
        {
            _graph = world.Get<HybridPoolGraphCmp>().Graph;

            _source = world;
            _mediator = mediator;
            _componentTypeID = componentTypeID;
            _maskBit = EcsMaskChunck.FromID(componentTypeID);

            const int capacity = 512;//TODO заменить на значение из конфига

            _mapping = new int[world.Capacity];
            _recycledItems = new int[128];
            _recycledItemsCount = 0;
            _items = new T[capacity];
            _entities = new int[capacity];
            _itemsCount = 0;
        }
        void IEcsPoolImplementation.OnWorldResize(int newSize)
        {
            Array.Resize(ref _mapping, newSize);
        }
        void IEcsPoolImplementation.OnWorldDestroy() { }
        void IEcsPoolImplementation.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
        {
            foreach (var entityID in buffer)
                TryDel(entityID);
        }
        #endregion

        #region Other
        void IEcsPool.AddRaw(int entityID, object dataRaw) => Add(entityID, (T)dataRaw);
        object IEcsReadonlyPool.GetRaw(int entityID) => Read(entityID);
        void IEcsPool.SetRaw(int entityID, object dataRaw) => Set(entityID, (T)dataRaw);
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
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        #endregion

        #region Devirtualize
        void IEcsHybridPoolInternal.Devirtualize(VirtualHybridPool virtualHybridPool)
        {
            _mapping = virtualHybridPool._mapping;
            _itemsCount = virtualHybridPool._itemsCount;
        }
        #endregion
    }
    /// <summary>Hybrid component</summary>
    public interface IEcsHybridComponent
    {
        bool IsAlive { get; }
        void OnAddToPool(entlong entity);
        void OnDelFromPool(entlong entity);
    }
    public static class EcsHybridPoolExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrNotAlive(this IEcsHybridComponent self) => self == null || self.IsAlive;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> GetPool<T>(this EcsWorld self) where T : class, IEcsHybridComponent
        {
            return self.GetPool<EcsHybridPool<T>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> GetPoolUnchecked<T>(this EcsWorld self) where T : class, IEcsHybridComponent
        {
            return self.GetPoolUnchecked<EcsHybridPool<T>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> Include<T>(this EcsAspect.Builder self) where T : class, IEcsHybridComponent
        {
            return self.Include<EcsHybridPool<T>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> Exclude<T>(this EcsAspect.Builder self) where T : class, IEcsHybridComponent
        {
            return self.Exclude<EcsHybridPool<T>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> Optional<T>(this EcsAspect.Builder self) where T : class, IEcsHybridComponent
        {
            return self.Optional<EcsHybridPool<T>>();
        }

        //-------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> GetHybridPool<T>(this EcsWorld self) where T : class, IEcsHybridComponent
        {
            return self.GetPool<EcsHybridPool<T>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> GetHybridPoolUnchecked<T>(this EcsWorld self) where T : class, IEcsHybridComponent
        {
            return self.GetPoolUnchecked<EcsHybridPool<T>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> IncludeHybrid<T>(this EcsAspect.Builder self) where T : class, IEcsHybridComponent
        {
            return self.Include<EcsHybridPool<T>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> ExcludeHybrid<T>(this EcsAspect.Builder self) where T : class, IEcsHybridComponent
        {
            return self.Exclude<EcsHybridPool<T>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> OptionalHybrid<T>(this EcsAspect.Builder self) where T : class, IEcsHybridComponent
        {
            return self.Optional<EcsHybridPool<T>>();
        }
    }

    public partial class EcsWorld
    {
        private Dictionary<Type, HybridMapping> _hybridMapping = new Dictionary<Type, HybridMapping>();
        internal HybridMapping GetHybridMapping(Type type)
        {
            if (!_hybridMapping.TryGetValue(type, out HybridMapping mapping))
            {
                mapping = new HybridMapping(this, type);
                _hybridMapping.Add(type, mapping);
            }
            return mapping;
        }
    }

    internal class HybridMapping
    {
        private EcsWorld _source;
        private object[] _sourceForReflection;
        private Type _type;

        private IEcsHybridPoolInternal _targetTypePool;
        private List<IEcsHybridPoolInternal> _relatedPools;

        private static Type hybridPoolType = typeof(EcsHybridPool<>);
        private static MethodInfo getHybridPoolMethod = typeof(EcsHybridPoolExtensions).GetMethod($"{nameof(EcsHybridPoolExtensions.GetPool)}", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        private static HashSet<Type> _hybridComponents = new HashSet<Type>();
        static HybridMapping()
        {
            Type hybridComponentType = typeof(IEcsHybridComponent);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.GetInterface(nameof(IEcsHybridComponent)) != null && type != hybridComponentType)
                    {
                        _hybridComponents.Add(type);
                    }
                }
            }
        }
        public static bool IsEcsHybridComponentType(Type type)
        {
            return _hybridComponents.Contains(type);
        }

        public HybridMapping(EcsWorld source, Type type)
        {
            if (!type.IsClass)
                throw new ArgumentException();

            _source = source;
            _type = type;
            _relatedPools = new List<IEcsHybridPoolInternal>();
            _sourceForReflection = new object[] { source };
            _targetTypePool = CreateHybridPool(type);
            foreach (var item in type.GetInterfaces())
            {
                if (IsEcsHybridComponentType(item))
                {
                    _relatedPools.Add(CreateHybridPool(item));
                }
            }
            Type baseType = type.BaseType;
            while (baseType != typeof(object) && IsEcsHybridComponentType(baseType))
            {
                _relatedPools.Add(CreateHybridPool(baseType));
                baseType = baseType.BaseType;
            }
        }
        private IEcsHybridPoolInternal CreateHybridPool(Type componentType)
        {
            return (IEcsHybridPoolInternal)getHybridPoolMethod.MakeGenericMethod(componentType).Invoke(null, _sourceForReflection);
        }

        public IEcsHybridPoolInternal GetTargetTypePool()
        {
            return _targetTypePool;
        }
        public List<IEcsHybridPoolInternal> GetPools()
        {
            return _relatedPools;
        }
    }
}




namespace DCFApixels.DragonECS.Internal
{
    internal interface IEcsHybridPoolInternal
    {
        Type ComponentType { get; }
        void AddRefInternal(int entityID, object component, bool isMain);
        void DelInternal(int entityID, bool isMain);
        void Devirtualize(VirtualHybridPool virtualHybridPool);
    }
    internal readonly struct HybridPoolGraphCmp : IEcsWorldComponent<HybridPoolGraphCmp>
    {
        public readonly HybridPoolGraph Graph;
        private HybridPoolGraphCmp(EcsWorld world)
        {
            Graph = new HybridPoolGraph(world);
        }
        public void Init(ref HybridPoolGraphCmp component, EcsWorld world)
        {
            component = new HybridPoolGraphCmp(world);
        }
        public void OnDestroy(ref HybridPoolGraphCmp component, EcsWorld world)
        {
            component = default;
        }
    }
    public class HybridPoolGraph
    {
        private EcsWorld _world;
        private Dictionary<Type, HybridPoolBranch> _branches = new Dictionary<Type, HybridPoolBranch>();

        public HybridPoolGraph(EcsWorld world)
        {
            _world = world;
        }

        public bool IsInstantiable(Type type)
        {
            return _branches.ContainsKey(type);
        }
        public bool TryGetBranch(Type type, out HybridPoolBranch branch)
        {
            return _branches.TryGetValue(type, out branch);
        }
        public void InitNewPool(IEcsHybridPoolInternal pool)
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

        public HybridPoolBranch GetBranch(Type targetType)
        {
            if (_branches.TryGetValue(targetType, out HybridPoolBranch branch) == false)
            {
                branch = new HybridPoolBranch(_world, targetType, null);
                _branches.Add(targetType, branch);
            }
            return branch;
        }
    }
    public class HybridPoolBranch
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