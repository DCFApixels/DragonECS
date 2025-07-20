#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Core.Internal;
using DCFApixels.DragonECS.PoolsCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS
{
    /// <summary>Value type component</summary>
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.POOLS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Standard component.")]
    [MetaID("DragonECS_84D2537C9201D6F6B92FEC1C8883A07A")]
    public interface IEcsValueComponent : IEcsComponentMember { }

    internal unsafe struct EcsValuePoolSharedStore
    {
        public int _componentTypeID;
        public int* _mapping;
        public void* _items;
    }
    internal readonly unsafe struct EcsValuePoolNative<T>
    {
        private readonly EcsValuePoolSharedStore* _store;
    }


    /// <summary>Pool for IEcsValueComponent components</summary>
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.POOLS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Pool for IEcsValueComponent components.")]
    [MetaID("DragonECS_C501547C9201A4B03FC25632E4FAAFD7")]
    [DebuggerDisplay("Count: {Count} Type: {ComponentType}")]
    public sealed unsafe class EcsValuePool<T> : IEcsPoolImplementation<T>, IEcsStructPool<T>, IEnumerable<T> //IEnumerable<T> - IntelliSense hack
        where T : unmanaged, IEcsValueComponent
    {
        private EcsWorld _source;
        private int _componentTypeID;
        private EcsMaskChunck _maskBit;

        public int* _mapping; // index = entityID / value = itemIndex;/ value = 0 = no entityID.
        public void* _items; // dense; _items[0] - fake component.
        public int _itemsLength;
        public int _itemsCount;
        public int* _recycledItems;
        public int _recycledItemsLength;
        public int _recycledItemsCount;
        private readonly EcsValuePoolSharedStore* _sharedStore;

        private readonly IEcsComponentLifecycle<T> _componentLifecycleHandler = EcsComponentLifecycleHandler<T>.instance;
        private readonly bool _isHasComponentLifecycleHandler = EcsComponentLifecycleHandler<T>.isHasHandler;
        private readonly IEcsComponentCopy<T> _componentCopyHandler = EcsComponentCopyHandler<T>.instance;
        private readonly bool _isHasComponentCopyHandler = EcsComponentCopyHandler<T>.isHasHandler;

        private bool _isLocked;

        private EcsWorld.PoolsMediator _mediator;

        #region Properites
        public int Count
        {
            get { return _itemsCount; }
        }
        public int Capacity
        {
            get { return _itemsLength; }
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
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ref Get(index); }
        }
        #endregion

        #region Constructors/Init/Destroy
        public EcsValuePool()
        {
            if (_sharedStore == null)
            {
                _sharedStore = MemoryAllocator.Alloc<EcsValuePoolSharedStore>(1).As<EcsValuePoolSharedStore>();
            }
        }
        public EcsValuePool(int capacity, int recycledCapacity = -1)
        {
            if (_sharedStore == null)
            {
                _sharedStore = MemoryAllocator.Alloc<EcsValuePoolSharedStore>(1).As<EcsValuePoolSharedStore>();
            }
            capacity = ArrayUtility.NextPow2(capacity);
            if (recycledCapacity < 0)
            {
                recycledCapacity = capacity / 2;
            }
            _itemsLength = capacity;
            _items = MemoryAllocator.Alloc<T>(_itemsLength).As<T>();
            _sharedStore->_items = _items;
            _recycledItemsLength = recycledCapacity;
            _recycledItems = MemoryAllocator.Alloc<int>(_recycledItemsLength).As<int>();
        }
        void IEcsPoolImplementation.OnInit(EcsWorld world, EcsWorld.PoolsMediator mediator, int componentTypeID)
        {
            _source = world;
            _mediator = mediator;
            _componentTypeID = componentTypeID;
            _maskBit = EcsMaskChunck.FromID(componentTypeID);

            _mapping = MemoryAllocator.Alloc<int>(world.Capacity).As<int>();
            _sharedStore->_mapping = _mapping;
            var worldConfig = world.Configs.GetWorldConfigOrDefault();
            if (_items == null)
            {
                _itemsLength = ArrayUtility.NextPow2(worldConfig.PoolComponentsCapacity);
                _items = MemoryAllocator.Alloc<T>(_itemsLength).As<T>();
                _sharedStore->_items = _items;
            }
            if (_recycledItems == null)
            {
                _recycledItemsLength = worldConfig.PoolRecycledComponentsCapacity;
                _recycledItems = MemoryAllocator.Alloc<int>(_recycledItemsLength).As<int>();
            }
        }
        void IEcsPoolImplementation.OnWorldDestroy() { }
        ~EcsValuePool()
        {
            MemoryAllocator.Free(_mapping);
            MemoryAllocator.Free(_items);
            MemoryAllocator.Free(_recycledItems);
        }
        #endregion

        #region Methods
        public ref T Add(int entityID)
        {
            ref int itemIndex = ref _mapping[entityID];
#if DEBUG
            if (entityID == EcsConsts.NULL_ENTITY_ID) { Throw.Ent_ThrowIsNotAlive(_source, entityID); }
            if (_source.IsUsed(entityID) == false) { Throw.Ent_ThrowIsNotAlive(_source, entityID); }
            if (itemIndex > 0) { EcsPoolThrowHelper.ThrowAlreadyHasComponent<T>(entityID); }
            if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
            if (itemIndex > 0) { return ref Get(entityID); }
            if (_isLocked | _source.IsUsed(entityID) == false) { return ref _items[0]; }
#endif
            if (_recycledItemsCount > 0)
            {
                itemIndex = _recycledItems[--_recycledItemsCount];
                _itemsCount++;
            }
            else
            {
                itemIndex = ++_itemsCount;
                if (itemIndex >= _itemsLength)
                {
                    _itemsLength = ArrayUtility.NextPow2(_itemsLength << 1);
                    _items = MemoryAllocator.Realloc(_items, _itemsLength).As<T>();
                    _sharedStore->_items = _items;
                }
            }
            _mediator.RegisterComponent(entityID, _componentTypeID, _maskBit);
            ref T result = ref ((T*)_items)[itemIndex];
            EnableComponent(ref result);
            return ref result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entityID)
        {
#if DEBUG // не нужен STAB_MODE
            if (!Has(entityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return ref ((T*)_items)[_mapping[entityID]];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID)
        {
#if DEBUG // не нужен STAB_MODE
            if (!Has(entityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return ref ((T*)_items)[_mapping[entityID]];
        }
        public ref T TryAddOrGet(int entityID)
        {
#if DEBUG
            if (entityID == EcsConsts.NULL_ENTITY_ID) { Throw.Ent_ThrowIsNotAlive(_source, entityID); }
#endif
            ref int itemIndex = ref _mapping[entityID];
            if (itemIndex <= 0)
            { //Add block
#if DEBUG
                if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
                if (_isLocked) { return ref _items[0]; }
#endif
                if (_recycledItemsCount > 0)
                {
                    itemIndex = _recycledItems[--_recycledItemsCount];
                    _itemsCount++;
                }
                else
                {
                    itemIndex = ++_itemsCount;
                    if (itemIndex >= _itemsLength)
                    {
                        _itemsLength = ArrayUtility.NextPow2(_itemsLength << 1);
                        _items = MemoryAllocator.Realloc(_items, _itemsLength).As<T>();
                        _sharedStore->_items = _items;
                    }
                }
                _mediator.RegisterComponent(entityID, _componentTypeID, _maskBit);
                EnableComponent(ref ((T*)_items)[itemIndex]);
            } //Add block end
            return ref ((T*)_items)[itemIndex];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            return _mapping[entityID] != 0;
        }
        public void Del(int entityID)
        {
            ref int itemIndex = ref _mapping[entityID];
#if DEBUG
            if (entityID == EcsConsts.NULL_ENTITY_ID) { Throw.Ent_ThrowIsNotAlive(_source, entityID); }
            if (itemIndex <= 0) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
            if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
            if (itemIndex <= 0) { return; }
            if (_isLocked) { return; }
#endif
            DisableComponent(ref ((T*)_items)[itemIndex]);
            if (_recycledItemsCount >= _recycledItemsLength)
            {
                _recycledItemsLength = ArrayUtility.NextPow2(_recycledItemsLength << 1);
                _recycledItems = MemoryAllocator.Realloc<int>(_recycledItems, _recycledItemsLength).As<int>();
            }
            _recycledItems[_recycledItemsCount++] = itemIndex;
            itemIndex = 0;
            _itemsCount--;
            _mediator.UnregisterComponent(entityID, _componentTypeID, _maskBit);
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
#if DEBUG
            if (!Has(fromEntityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(fromEntityID); }
#elif DRAGONECS_STABILITY_MODE
            if (!Has(fromEntityID)) { return; }
#endif
            CopyComponent(ref Get(fromEntityID), ref TryAddOrGet(toEntityID));
        }
        public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
#if DEBUG
            if (!Has(fromEntityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(fromEntityID); }
#elif DRAGONECS_STABILITY_MODE
            if (!Has(fromEntityID)) { return; }
#endif
            CopyComponent(ref Get(fromEntityID), ref toWorld.GetPool<T>().TryAddOrGet(toEntityID));
        }

        public void ClearAll()
        {
#if DEBUG
            if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
            if (_isLocked) { return; }
#endif
            _recycledItemsCount = 0; // спереди чтобы обнулялось, так как Del не обнуляет
            if (_itemsCount <= 0) { return; }
            var span = _source.Where(out SinglePoolAspect<EcsValuePool<T>> _);
            foreach (var entityID in span)
            {
                ref int itemIndex = ref _mapping[entityID];
                DisableComponent(ref ((T*)_items)[itemIndex]);
                itemIndex = 0;
                _mediator.UnregisterComponent(entityID, _componentTypeID, _maskBit);
            }
            _itemsCount = 0;
            _recycledItemsCount = 0;
        }
        #endregion

        #region Callbacks
        void IEcsPoolImplementation.OnWorldResize(int newSize)
        {
            _mapping = MemoryAllocator.Realloc(_mapping, newSize).As<int>();
            _sharedStore->_mapping = _mapping;
        }
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
        void IEcsPoolImplementation.OnLockedChanged_Debug(bool locked) { _isLocked = locked; }
        #endregion

        #region Other
        void IEcsPool.AddEmpty(int entityID) { Add(entityID); }
        void IEcsPool.AddRaw(int entityID, object dataRaw)
        {
            Add(entityID) = dataRaw == null ? default : (T)dataRaw;
        }
        object IEcsReadonlyPool.GetRaw(int entityID) { return Read(entityID); }
        void IEcsPool.SetRaw(int entityID, object dataRaw)
        {
            Get(entityID) = dataRaw == null ? default : (T)dataRaw;
        }
        #endregion

        #region Listeners
#if !DRAGONECS_DISABLE_POOLS_EVENTS
        public void AddListener(IEcsPoolEventListener listener)
        {
            EcsDebug.PrintWarning($"the {nameof(AddListener)} method is not supported for the {nameof(EcsValuePool<T>)}.");
        }
        public void RemoveListener(IEcsPoolEventListener listener)
        {
            EcsDebug.PrintWarning($"the {nameof(RemoveListener)} method is not supported for the {nameof(EcsValuePool<T>)}.");
        }
#endif
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

        #region Convertors
        public static implicit operator EcsValuePool<T>(IncludeMarker a) { return a.GetInstance<EcsValuePool<T>>(); }
        public static implicit operator EcsValuePool<T>(ExcludeMarker a) { return a.GetInstance<EcsValuePool<T>>(); }
        public static implicit operator EcsValuePool<T>(AnyMarker a) { return a.GetInstance<EcsValuePool<T>>(); }
        public static implicit operator EcsValuePool<T>(OptionalMarker a) { return a.GetInstance<EcsValuePool<T>>(); }
        public static implicit operator EcsValuePool<T>(EcsWorld.GetPoolInstanceMarker a) { return a.GetInstance<EcsValuePool<T>>(); }
        #endregion

        #region Apply
        public static void Apply(ref T component, int entityID, short worldID)
        {
            EcsWorld.GetPoolInstance<EcsValuePool<T>>(worldID).TryAddOrGet(entityID) = component;
        }
        public static void Apply(ref T component, int entityID, EcsValuePool<T> pool)
        {
            pool.TryAddOrGet(entityID) = component;
        }
        #endregion
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct ReadonlyEcsValuePool<T> : IEcsReadonlyPool //IEnumerable<T> - IntelliSense hack
        where T : unmanaged, IEcsValueComponent
    {
        private readonly EcsValuePool<T> _pool;

        #region Properties
        public int ComponentTypeID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _pool.ComponentTypeID; }
        }
        public Type ComponentType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _pool.ComponentType; }
        }
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _pool.World; }
        }
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _pool.Count; }
        }
        public bool IsReadOnly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _pool.IsReadOnly; }
        }
        public ref readonly T this[int entityID]
        {
            get { return ref _pool.Read(entityID); }
        }
        #endregion

        #region Constructors
        internal ReadonlyEcsValuePool(EcsValuePool<T> pool)
        {
            _pool = pool;
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) { return _pool.Has(entityID); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Get(int entityID) { return ref _pool.Read(entityID); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID) { return ref _pool.Read(entityID); }
        object IEcsReadonlyPool.GetRaw(int entityID) { return _pool.Read(entityID); }

#if !DRAGONECS_DISABLE_POOLS_EVENTS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddListener(IEcsPoolEventListener listener) { _pool.AddListener(listener); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveListener(IEcsPoolEventListener listener) { _pool.AddListener(listener); }
#endif
        #endregion

        #region Convertors
        public static implicit operator ReadonlyEcsValuePool<T>(EcsValuePool<T> a) { return new ReadonlyEcsValuePool<T>(a); }
        public static implicit operator ReadonlyEcsValuePool<T>(IncludeMarker a) { return a.GetInstance<EcsValuePool<T>>(); }
        public static implicit operator ReadonlyEcsValuePool<T>(ExcludeMarker a) { return a.GetInstance<EcsValuePool<T>>(); }
        public static implicit operator ReadonlyEcsValuePool<T>(AnyMarker a) { return a.GetInstance<EcsValuePool<T>>(); }
        public static implicit operator ReadonlyEcsValuePool<T>(OptionalMarker a) { return a.GetInstance<EcsValuePool<T>>(); }
        public static implicit operator ReadonlyEcsValuePool<T>(EcsWorld.GetPoolInstanceMarker a) { return a.GetInstance<EcsValuePool<T>>(); }
        #endregion
    }


    public static class EcsValuePoolExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsValuePool<TComponent> GetPool<TComponent>(this EcsWorld self) where TComponent : unmanaged, IEcsValueComponent
        {
            return self.GetPoolInstance<EcsValuePool<TComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsValuePool<TComponent> GetPoolUnchecked<TComponent>(this EcsWorld self) where TComponent : unmanaged, IEcsValueComponent
        {
            return self.GetPoolInstanceUnchecked<EcsValuePool<TComponent>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsValuePool<TComponent> Inc<TComponent>(this EcsAspect.Builder self) where TComponent : unmanaged, IEcsValueComponent
        {
            return self.IncludePool<EcsValuePool<TComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsValuePool<TComponent> Exc<TComponent>(this EcsAspect.Builder self) where TComponent : unmanaged, IEcsValueComponent
        {
            return self.ExcludePool<EcsValuePool<TComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsValuePool<TComponent> Opt<TComponent>(this EcsAspect.Builder self) where TComponent : unmanaged, IEcsValueComponent
        {
            return self.OptionalPool<EcsValuePool<TComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsValuePool<TComponent> Any<TComponent>(this EcsAspect.Builder self) where TComponent : unmanaged, IEcsValueComponent
        {
            return self.AnyPool<EcsValuePool<TComponent>>();
        }
    }
}