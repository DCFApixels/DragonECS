using DCFApixels.DragonECS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    /// <summary>Pool for IEcsComponent components</summary>
    public sealed class EcsPool<T> : IEcsPoolImplementation<T>, IEcsStructPool<T>, IEnumerable<T> //IEnumerable<T> - IntelliSense hack
        where T : struct, IEcsComponent
    {
        private EcsWorld _source;
        private int _componentTypeID;
        private EcsMaskChunck _maskBit;

        private int[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID
        private T[] _items; //dense
        private int _itemsCount = 0;
        private int[] _recycledItems;
        private int _recycledItemsCount = 0;

        private IEcsComponentLifecycle<T> _componentLifecycleHandler = EcsComponentResetHandler<T>.instance;
        private bool _isHasComponentLifecycleHandler = EcsComponentResetHandler<T>.isHasHandler;
        private IEcsComponentCopy<T> _componentCopyHandler = EcsComponentCopyHandler<T>.instance;
        private bool _isHasComponentCopyHandler = EcsComponentCopyHandler<T>.isHasHandler;

        private List<IEcsPoolEventListener> _listeners = new List<IEcsPoolEventListener>();
        private int _listenersCachedCount = 0;

        private EcsWorld.PoolsMediator _mediator;

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
        public bool IsReadOnly
        {
            get { return false; }
        }
        #endregion

        #region Methods
        public ref T Add(int entityID)
        {
            ref int itemIndex = ref _mapping[entityID];
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (itemIndex > 0) { EcsPoolThrowHalper.ThrowAlreadyHasComponent<T>(entityID); }
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
                }
            }
            _mediator.RegisterComponent(entityID, _componentTypeID, _maskBit);
            EnableComponent(ref _items[itemIndex]);
            _listeners.InvokeOnAddAndGet(entityID, _listenersCachedCount);
            return ref _items[itemIndex];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) { EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID); }
#endif
            _listeners.InvokeOnGet(entityID, _listenersCachedCount);
            return ref _items[_mapping[entityID]];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) { EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return ref _items[_mapping[entityID]];
        }
        public ref T TryAddOrGet(int entityID)
        {
            ref int itemIndex = ref _mapping[entityID];
            if (itemIndex <= 0)
            {
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
                    }
                }
                _mediator.RegisterComponent(entityID, _componentTypeID, _maskBit);
                EnableComponent(ref _items[itemIndex]);
                _listeners.InvokeOnAdd(entityID, _listenersCachedCount);
            }
            _listeners.InvokeOnGet(entityID, _listenersCachedCount);
            return ref _items[itemIndex];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            return _mapping[entityID] > 0;
        }
        public void Del(int entityID)
        {
            ref int itemIndex = ref _mapping[entityID];
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (itemIndex <= 0) { EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID); }
#endif
            DisableComponent(ref _items[itemIndex]);
            if (_recycledItemsCount >= _recycledItems.Length)
            {
                Array.Resize(ref _recycledItems, _recycledItems.Length << 1);
            }
            _recycledItems[_recycledItemsCount++] = itemIndex;
            itemIndex = 0;
            _itemsCount--;
            _mediator.UnregisterComponent(entityID, _componentTypeID, _maskBit);
            _listeners.InvokeOnDel(entityID, _listenersCachedCount);
        }
        public void TryDel(int entityID)
        {
            if (Has(entityID))
            {
                Del(entityID);
            }
        }
        public void Copy(int fromEntityID, int toEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(fromEntityID)) { EcsPoolThrowHalper.ThrowNotHaveComponent<T>(fromEntityID); }
#endif
            CopyComponent(ref Get(fromEntityID), ref TryAddOrGet(toEntityID));
        }
        public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(fromEntityID)) { EcsPoolThrowHalper.ThrowNotHaveComponent<T>(fromEntityID); }
#endif
            CopyComponent(ref Get(fromEntityID), ref toWorld.GetPool<T>().TryAddOrGet(toEntityID));
        }

        public void ClearAll()
        {
            var span = _source.Where(out SingleAspect<EcsPool<T>> _);
            _itemsCount = 0;
            _recycledItemsCount = 0;
            foreach (var entityID in span)
            {
                ref int itemIndex = ref _mapping[entityID];
                DisableComponent(ref _items[itemIndex]);
                itemIndex = 0;
                _mediator.UnregisterComponent(entityID, _componentTypeID, _maskBit);
                _listeners.InvokeOnDel(entityID, _listenersCachedCount);
            }
        }
        #endregion

        #region Callbacks
        void IEcsPoolImplementation.OnInit(EcsWorld world, EcsWorld.PoolsMediator mediator, int componentTypeID)
        {
            _source = world;
            _mediator = mediator;
            _componentTypeID = componentTypeID;
            _maskBit = EcsMaskChunck.FromID(componentTypeID);

            _mapping = new int[world.Capacity];
            _items = new T[ArrayUtility.NormalizeSizeToPowerOfTwo(world.Configs.GetWorldConfigOrDefault().PoolComponentsCapacity)];
            _recycledItems = new int[world.Configs.GetWorldConfigOrDefault().PoolRecycledComponentsCapacity];
        }
        void IEcsPoolImplementation.OnWorldResize(int newSize)
        {
            Array.Resize(ref _mapping, newSize);
        }
        void IEcsPoolImplementation.OnWorldDestroy() { }
        void IEcsPoolImplementation.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
        {
            if (_itemsCount <= 0)
            {
                return;
            }
            foreach (var entityID in buffer)
            {
                TryDel(entityID);
            }
        }
        #endregion

        #region Other
        void IEcsPool.AddRaw(int entityID, object dataRaw)
        {
            Add(entityID) = dataRaw == null ? default : (T)dataRaw;
        }
        object IEcsReadonlyPool.GetRaw(int entityID) { return Get(entityID); }
        void IEcsPool.SetRaw(int entityID, object dataRaw)
        {
            Get(entityID) = dataRaw == null ? default : (T)dataRaw;
        }
        #endregion

        #region Listeners
        public void AddListener(IEcsPoolEventListener listener)
        {
            if (listener == null) { throw new ArgumentNullException("listener is null"); }
            _listeners.Add(listener);
            _listenersCachedCount++;
        }
        public void RemoveListener(IEcsPoolEventListener listener)
        {
            if (listener == null) { throw new ArgumentNullException("listener is null"); }
            if (_listeners.Remove(listener))
            {
                _listenersCachedCount--;
            }
        }
        #endregion

        #region Enable/Disable/Copy
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnableComponent(ref T component)
        {
            if (_isHasComponentLifecycleHandler)
            {
                _componentLifecycleHandler.Enable(ref component);
            }
            else
            {
                component = default;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DisableComponent(ref T component)
        {
            if (_isHasComponentLifecycleHandler)
            {
                _componentLifecycleHandler.Disable(ref component);
            }
            else
            {
                component = default;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CopyComponent(ref T from, ref T to)
        {
            if (_isHasComponentCopyHandler)
            {
                _componentCopyHandler.Copy(ref from, ref to);
            }
            else
            {
                to = from;
            }
        }
        #endregion

        #region IEnumerator - IntelliSense hack
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { throw new NotImplementedException(); }
        IEnumerator IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
        #endregion

        #region MarkersConverter
        public static implicit operator EcsPool<T>(IncludeMarker a) { return a.GetInstance<EcsPool<T>>(); }
        public static implicit operator EcsPool<T>(ExcludeMarker a) { return a.GetInstance<EcsPool<T>>(); }
        public static implicit operator EcsPool<T>(OptionalMarker a) { return a.GetInstance<EcsPool<T>>(); }
        #endregion
    }
    /// <summary>Standard component</summary>
    public interface IEcsComponent { }
    public static class EcsPoolExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPool<TComponent> GetPool<TComponent>(this EcsWorld self) where TComponent : struct, IEcsComponent
        {
            return self.GetPoolInstance<EcsPool<TComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPool<TComponent> GetPoolUnchecked<TComponent>(this EcsWorld self) where TComponent : struct, IEcsComponent
        {
            return self.GetPoolInstanceUnchecked<EcsPool<TComponent>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPool<TComponent> Include<TComponent>(this EcsAspect.Builder self) where TComponent : struct, IEcsComponent
        {
            return self.IncludePool<EcsPool<TComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPool<TComponent> Exclude<TComponent>(this EcsAspect.Builder self) where TComponent : struct, IEcsComponent
        {
            return self.ExcludePool<EcsPool<TComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPool<TComponent> Optional<TComponent>(this EcsAspect.Builder self) where TComponent : struct, IEcsComponent
        {
            return self.OptionalPool<EcsPool<TComponent>>();
        }
    }
}
