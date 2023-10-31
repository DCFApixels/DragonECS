using DCFApixels.DragonECS.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    /// <summary>Pool for IEcsHybridComponent components</summary>
    public sealed class EcsHybridPool<T> : IEcsPoolImplementation<T>, IEcsHybridPool<T>, IEnumerable<T> //IEnumerable<T> - IntelliSense hack
        where T : IEcsHybridComponent
    {
        private EcsWorld _source;
        private int _componentID;

        private int[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID
        private T[] _items; //dense
        private int[] _entities;
        private int _itemsCount;

        private int[] _recycledItems;
        private int _recycledItemsCount;

        private List<IEcsPoolEventListener> _listeners = new List<IEcsPoolEventListener>();

        #region Properites
        public int Count => _itemsCount;
        public int Capacity => _items.Length;
        public int ComponentID => _componentID;
        public Type ComponentType => typeof(T);
        public EcsWorld World => _source;
        #endregion

        #region Init
        void IEcsPoolImplementation.OnInit(EcsWorld world, int componentID)
        {
            _source = world;
            _componentID = componentID;

            const int capacity = 512;

            _mapping = new int[world.Capacity];
            _recycledItems = new int[128];
            _recycledItemsCount = 0;
            _items = new T[capacity];
            _entities = new int[capacity];
            _itemsCount = 0;
        }
        #endregion

        #region Methods
        public void Add(int entityID, T component)
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
            this.IncrementEntityComponentCount(entityID);
            _listeners.InvokeOnAdd(entityID);
            component.OnAddToPool(_source.GetEntityLong(entityID));
            _items[itemIndex] = component;
            _entities[itemIndex] = entityID;
        }
        public void Set(int entityID, T component)
        {
            ref int itemIndex = ref _mapping[entityID];
            if(itemIndex <= 0)
            {//null
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
                this.IncrementEntityComponentCount(entityID);
            }
            else
            {//not null
                _listeners.InvokeOnDel(entityID);
                _items[itemIndex].OnDelFromPool(_source.GetEntityLong(entityID));
            }
            _listeners.InvokeOnAdd(entityID);
            component.OnAddToPool(_source.GetEntityLong(entityID));
            _items[itemIndex] = component;
            _entities[itemIndex] = entityID;
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
        public void Del(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID);
#endif
            ref int itemIndex = ref _mapping[entityID];
            T component = _items[itemIndex];
            component.OnDelFromPool(_source.GetEntityLong(entityID));
            if (_recycledItemsCount >= _recycledItems.Length)
                Array.Resize(ref _recycledItems, _recycledItems.Length << 1);
            _recycledItems[_recycledItemsCount++] = itemIndex;
            _mapping[entityID] = 0;
            _entities[itemIndex] = 0;
            _itemsCount--;
            this.DecrementEntityComponentCount(entityID);
            _listeners.InvokeOnDel(entityID);
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
    public static class IEcsHybridComponentExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrNotAlive(this IEcsHybridComponent self) => self == null || self.IsAlive;
    }
    public static class EcsHybridPoolExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> GetPool<T>(this EcsWorld self) where T : IEcsHybridComponent
        {
            return self.GetPool<EcsHybridPool<T>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsHybridPool<T> UncheckedGetPool<T>(this EcsWorld self) where T : IEcsHybridComponent
        {
            return self.UncheckedGetPool<EcsHybridPool<T>>();
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
    }







    public static class InterfaceMatrix
    {
        private static SparseArray<InterfaceMatrixEdge> _edges = new SparseArray<InterfaceMatrixEdge>();
        private static SparseArray64<InterfaceMatrixEdge> _matrix = new SparseArray64<InterfaceMatrixEdge>();
        public static bool HasEdge<TParent, TChild>()
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!InterfaceIsDeclared<TParent>() || !InterfaceIsDeclared<TChild>())
                EcsDebug.PrintWarning($"{nameof(TParent)} or {nameof(TChild)} not declared.");
#endif
            return _matrix.Contains(InterfaceId<TParent>._id, InterfaceId<TChild>._id);
        }
        public static bool InterfaceIsDeclared<T>() => _edges.Contains(InterfaceId<T>._id);

        public static void DeclareInterfacesFromClass<T>()
        {
            Type type = typeof(T);
            if (type.IsInterface)
                throw new ArgumentException($"The argument {nameof(T)} cannot be an interface");
        }
    }
    internal class InterfaceMatrixEdge
    {
        private static int _increment = 0;

        public readonly int id;
        public readonly Type parentType;
        public readonly Type childType;
        public readonly int parentID;
        public readonly int childID;
        public static InterfaceMatrixEdge New<TParent, TChild>()
        {
            return new InterfaceMatrixEdge(
                typeof(TParent), 
                typeof(TChild), 
                InterfaceId<TParent>._id, 
                InterfaceId<TChild>._id);
        }
        public InterfaceMatrixEdge(Type parentType, Type childType, int parentID, int childID)
        {
            id = _increment++;
            this.parentType = parentType;
            this.childType = childType;
            this.parentID = parentID;
            this.childID = childID;
        }
    }
    internal static class InterfaceId
    {
        internal static int _increment;
    }
    internal static class InterfaceId<T>
    {
        public static int _id = InterfaceId._increment++;
    }
}
