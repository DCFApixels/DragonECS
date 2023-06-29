using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    /// <summary>Pool for IEcsComponent components</summary>
    public sealed class EcsHybridPool<T> : IEcsPoolImplementation<T>, IEcsHybridPool<T>, IEnumerable<T> //IEnumerable<T> - IntelliSense hack
        where T : IEcsHybridComponent
    {
        private EcsWorld _source;
        private int _componentID;

        private int[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID
        private T[] _items; //dense
        private int _itemsCount;
        private int[] _recycledItems;
        private int _recycledItemsCount;

        private IEcsComponentReset<T> _componentResetHandler = EcsComponentResetHandler<T>.instance;
        private IEcsComponentCopy<T> _componentCopyHandler = EcsComponentCopyHandler<T>.instance;

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
                    Array.Resize(ref _items, _items.Length << 1);
            }
            this.IncrementEntityComponentCount(entityID);
            _listeners.InvokeOnAdd(entityID);
            _items[itemIndex] = component;
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
                        Array.Resize(ref _items, _items.Length << 1);
                }
                this.IncrementEntityComponentCount(entityID);
            }
            else
            {//not null
                _listeners.InvokeOnDel(entityID);
                if (_items[itemIndex] is IDisposable disposable)
                    disposable.Dispose();
            }
            _listeners.InvokeOnAdd(entityID);
            _items[itemIndex] = component;
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
            if (component is IDisposable disposable)
                disposable.Dispose();
            if (_recycledItemsCount >= _recycledItems.Length)
                Array.Resize(ref _recycledItems, _recycledItems.Length << 1);
            _recycledItems[_recycledItemsCount++] = itemIndex;
            _mapping[entityID] = 0;
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
        entlong Entity { set; }
        void OnAddToPool();
        void OnDelFromPool();
    }
    public static class EcsHybridPoolExt
    {
        public static EcsHybridPool<T> GetPool<T>(this EcsWorld self) where T : IEcsHybridComponent
        {
            return self.GetPool<EcsHybridPool<T>>();
        }

        public static EcsHybridPool<T> Include<T>(this EcsAspectBuilderBase self) where T : IEcsHybridComponent
        {
            return self.Include<EcsHybridPool<T>>();
        }
        public static EcsHybridPool<T> Exclude<T>(this EcsAspectBuilderBase self) where T : IEcsHybridComponent
        {
            return self.Exclude<EcsHybridPool<T>>();
        }
        public static EcsHybridPool<T> Optional<T>(this EcsAspectBuilderBase self) where T : IEcsHybridComponent
        {
            return self.Optional<EcsHybridPool<T>>();
        }
    }
}
