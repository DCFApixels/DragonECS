using DCFApixels.DragonECS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    namespace Internal
    {
        public interface IEcsHybridPoolInternal : IEcsPool
        {
            void AddRefInternal(int entityID, object component, bool isAppend);
            void DelInternal(int entityID, bool isAppend);
        }
    }
    /// <summary>Pool for IEcsHybridComponent components</summary>
    public sealed class EcsHybridPool<T> : IEcsPoolImplementation<T>, IEcsHybridPool<T>, IEcsHybridPoolInternal, IEnumerable<T> //IEnumerable<T> - IntelliSense hack
        where T : IEcsHybridComponent
    {
        private EcsWorld _source;
        private int _componentTypeID;
        private EcsMaskBit _maskBit;

        private int[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID
        private T[] _items; //dense
        private int[] _entities;
        private int _itemsCount;

        private int[] _recycledItems;
        private int _recycledItemsCount;

        private List<IEcsPoolEventListener> _listeners = new List<IEcsPoolEventListener>();

        private EcsWorld.PoolsMediator _mediator;

        #region Properites
        public int Count => _itemsCount;
        public int Capacity => _items.Length;
        public int ComponentID => _componentTypeID;
        public Type ComponentType => typeof(T);
        public EcsWorld World => _source;
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
                component.OnAddToPool(_source.GetEntityLong(entityID));
            _items[itemIndex] = component;
            _entities[itemIndex] = entityID;
        }
        public void Add(int entityID, T component)
        {
            HybridMapping mapping = _source.GetHybridMapping(component.GetType());
            mapping.GetTargetTypePool().AddRefInternal(entityID, component, false);
            foreach (var pool in mapping.GetPools())
                pool.AddRefInternal(entityID, component, true);
        }
        public void Set(int entityID, T component)
        {
            if (Has(entityID))
                Del(entityID);
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
                component.OnDelFromPool(_source.GetEntityLong(entityID));
            if (_recycledItemsCount >= _recycledItems.Length)
                Array.Resize(ref _recycledItems, _recycledItems.Length << 1);
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
            mapping.GetTargetTypePool().DelInternal(entityID, false);
            foreach (var pool in mapping.GetPools())
                pool.DelInternal(entityID, true);
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
                    Del(_entities[i]);
            }
        }
        #endregion

        #region Callbacks
        void IEcsPoolImplementation.OnInit(EcsWorld world, EcsWorld.PoolsMediator mediator, int componentTypeID)
        {
            _source = world;
            _mediator = mediator;
            _componentTypeID = componentTypeID;
            _maskBit = EcsMaskBit.FromID(componentTypeID);

            const int capacity = 512;

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
        object IEcsPool.GetRaw(int entityID) => Read(entityID);
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
        public static EcsHybridPool<T> GetPool<T>(this EcsWorld self) where T : IEcsHybridComponent
        {
            return self.GetPool<EcsHybridPool<T>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> GetPoolUnchecked<T>(this EcsWorld self) where T : IEcsHybridComponent
        {
            return self.GetPoolUnchecked<EcsHybridPool<T>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> Include<T>(this EcsAspectBuilderBase self) where T : IEcsHybridComponent
        {
            return self.Include<EcsHybridPool<T>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> Exclude<T>(this EcsAspectBuilderBase self) where T : IEcsHybridComponent
        {
            return self.Exclude<EcsHybridPool<T>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> Optional<T>(this EcsAspectBuilderBase self) where T : IEcsHybridComponent
        {
            return self.Optional<EcsHybridPool<T>>();
        }

        //-------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> GetHybridPool<T>(this EcsWorld self) where T : IEcsHybridComponent
        {
            return self.GetPool<EcsHybridPool<T>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> GetHybridPoolUnchecked<T>(this EcsWorld self) where T : IEcsHybridComponent
        {
            return self.GetPoolUnchecked<EcsHybridPool<T>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> IncludeHybrid<T>(this EcsAspectBuilderBase self) where T : IEcsHybridComponent
        {
            return self.Include<EcsHybridPool<T>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> ExcludeHybrid<T>(this EcsAspectBuilderBase self) where T : IEcsHybridComponent
        {
            return self.Exclude<EcsHybridPool<T>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> OptionalHybrid<T>(this EcsAspectBuilderBase self) where T : IEcsHybridComponent
        {
            return self.Optional<EcsHybridPool<T>>();
        }
    }

    public abstract partial class EcsWorld
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
            //var x = (IEcsHybridPoolInternal)getHybridPoolMethod.MakeGenericMethod(componentType).Invoke(null, _sourceForReflection);
            //Debug.Log("_" + x.ComponentID + "_" +x.ComponentType.Name);
            //return x;
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
