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
using static DCFApixels.DragonECS.Core.Internal.MemoryAllocator;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS
{
    /// <summary>Standard component</summary>
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.POOLS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Standard component.")]
    [MetaID("DragonECS_84D2537C9201D6F6B92FEC1C8883A07A")]
    public interface IEcsComponent : IEcsComponentMember { }

    /// <summary>Pool for IEcsComponent components</summary>
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.POOLS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Pool for IEcsComponent components.")]
    [MetaID("DragonECS_C501547C9201A4B03FC25632E4FAAFD7")]
    [DebuggerDisplay("Count: {Count} Type: {ComponentType}")]
    public sealed class EcsPool<T> : IEcsPoolImplementation<T>, IEcsStructPool<T>, IEnumerable<T>, IEntityStorage //IEnumerable<T> - IntelliSense hack
        where T : struct, IEcsComponent
    {
        private short _worldID;
        private EcsWorld _world;
        private int _componentTypeID;
        private EcsMaskChunck _maskBit;

        private int[] _dense;
        private int _denseCount = 0;
        private MappingSlot[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID.
        private T[] _items; // dense; _items[0] - fake component.
        private int _itemsCount = 0;
        private int _recycledItemsCount;

        private readonly IEcsComponentLifecycle<T> _customLifecycle = EcsComponentLifecycle<T>.CustomHandler;
        private readonly bool _isCustomLifecycle = EcsComponentLifecycle<T>.IsCustom;
        private readonly IEcsComponentCopy<T> _customCopy = EcsComponentCopy<T>.CustomHandler;
        private readonly bool _isCustomCopy = EcsComponentCopy<T>.IsCustom;

#if !DRAGONECS_DISABLE_POOLS_EVENTS
        private StructList<IEcsPoolEventListener> _listeners = new StructList<IEcsPoolEventListener>(2);
        private bool _hasAnyListener = false;
#endif
        private bool _isLocked;

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
            get { return _world; }
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
        public EcsPool() { }
        public EcsPool(int capacity, int recycledCapacity = -1)
        {
            capacity = ArrayUtility.CeilPow2Safe(capacity);
            if (recycledCapacity < 0)
            {
                recycledCapacity = capacity / 2;
            }
            _items = new T[capacity];
            _dense = new int[capacity];
        }
        void IEcsPoolImplementation.OnInit(EcsWorld world, EcsWorld.PoolsMediator mediator, int componentTypeID)
        {
            _world = world;
            _worldID = world.ID;
            _mediator = mediator;
            _componentTypeID = componentTypeID;
            _maskBit = EcsMaskChunck.FromID(componentTypeID);

            _mapping = new MappingSlot[world.Capacity];
            var worldConfig = world.Configs.GetWorldConfigOrDefault();
            if (_items == null)
            {
                _items = new T[ArrayUtility.CeilPow2Safe(worldConfig.PoolComponentsCapacity)];
                _dense = new int[ArrayUtility.CeilPow2Safe(worldConfig.PoolComponentsCapacity)];
            }
        }
        void IEcsPoolImplementation.OnWorldDestroy() { }
        #endregion

        #region Methods
        public ref T Add(int entityID)
        {
            ref var mappingSlot = ref _mapping[entityID];
#if DEBUG
            if (entityID == EcsConsts.NULL_ENTITY_ID) { EcsPoolThrowHelper.ThrowEntityIsNotAlive(_world, entityID); }
            if (_world.IsUsed(entityID) == false) { EcsPoolThrowHelper.ThrowEntityIsNotAlive(_world, entityID); }
            if (mappingSlot.ComponentPosIndex > 0) { EcsPoolThrowHelper.ThrowAlreadyHasComponent<T>(entityID); }
            if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
            if (itemIndex > 0) { return ref Get(entityID); }
            if (_isLocked | _world.IsUsed(entityID) == false) { return ref _items[0]; }
#endif
            if (_recycledItemsCount > 0)
            {
                //mappingSlot.ComponentPosIndex = _recycledItems[--_recycledItemsCount];
                //mappingSlot.ComponentPosIndex = _dense[_dense.Length - _recycledItemsCount] & int.MaxValue;
                mappingSlot.ComponentPosIndex = _dense[_dense.Length - _recycledItemsCount];
                _recycledItemsCount--;
                _itemsCount++;
            }
            else
            {
                mappingSlot.ComponentPosIndex = ++_itemsCount;
                if (mappingSlot.ComponentPosIndex >= _items.Length)
                {
                    Array.Resize(ref _dense, ArrayUtility.NextPow2(mappingSlot.ComponentPosIndex));
                    Array.Resize(ref _items, ArrayUtility.NextPow2(mappingSlot.ComponentPosIndex));
                }
            }
            mappingSlot.EntityPosIndex = _denseCount;
            _dense[_denseCount++] = entityID;
            _mediator.RegisterComponent(entityID, _componentTypeID, _maskBit);
            ref T result = ref _items[mappingSlot.ComponentPosIndex];
            EcsComponentLifecycle<T>.OnAdd(_isCustomLifecycle, _customLifecycle, ref result, _worldID, entityID);
#if !DRAGONECS_DISABLE_POOLS_EVENTS
            if (_hasAnyListener) { _listeners.InvokeOnAddAndGet(entityID); }
#endif
            return ref result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entityID)
        {
#if DEBUG // не нужен STAB_MODE
            if (!Has(entityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
#endif
#if !DRAGONECS_DISABLE_POOLS_EVENTS
            if (_hasAnyListener) { _listeners.InvokeOnGet(entityID); }
#endif
            return ref _items[_mapping[entityID].ComponentPosIndex];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID)
        {
#if DEBUG // не нужен STAB_MODE
            if (!Has(entityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return ref _items[_mapping[entityID].ComponentPosIndex];
        }
        public ref T TryAddOrGet(int entityID)
        {
#if DEBUG
            if (entityID == EcsConsts.NULL_ENTITY_ID) { EcsPoolThrowHelper.ThrowEntityIsNotAlive(_world, entityID); }
#endif
            ref var mappingSlot = ref _mapping[entityID];
            if (mappingSlot.ComponentPosIndex <= 0)
            { //Add block
#if DEBUG
                if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
                if (_isLocked) { return ref _items[0]; }
#endif
                if (_recycledItemsCount > 0)
                {
                    //mappingSlot.ComponentPosIndex = _dense[_dense.Length - _recycledItemsCount] & int.MaxValue;
                    mappingSlot.ComponentPosIndex = _dense[_dense.Length - _recycledItemsCount];
                    _recycledItemsCount--;
                    _itemsCount++;
                }
                else
                {
                    mappingSlot.ComponentPosIndex = ++_itemsCount;
                    if (mappingSlot.ComponentPosIndex >= _items.Length)
                    {
                        Array.Resize(ref _dense, ArrayUtility.NextPow2(mappingSlot.ComponentPosIndex));
                        Array.Resize(ref _items, ArrayUtility.NextPow2(mappingSlot.ComponentPosIndex));
                    }
                }
                mappingSlot.EntityPosIndex = _denseCount;
                _dense[_denseCount++] = entityID;
                _mediator.RegisterComponent(entityID, _componentTypeID, _maskBit);
                EcsComponentLifecycle<T>.OnAdd(_isCustomLifecycle, _customLifecycle, ref _items[mappingSlot.ComponentPosIndex], _worldID, entityID);
#if !DRAGONECS_DISABLE_POOLS_EVENTS
                if (_hasAnyListener) { _listeners.InvokeOnAdd(entityID); }
#endif
            } //Add block end
#if !DRAGONECS_DISABLE_POOLS_EVENTS
            if (_hasAnyListener) { _listeners.InvokeOnGet(entityID); }
#endif
            return ref _items[mappingSlot.ComponentPosIndex];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            return _mapping[entityID].ComponentPosIndex > 0;
        }
        public void Del(int entityID)
        {
            ref var mappingSlot = ref _mapping[entityID];
#if DEBUG
            if (entityID == EcsConsts.NULL_ENTITY_ID) { EcsPoolThrowHelper.ThrowEntityIsNotAlive(_world, entityID); }
            if (mappingSlot.ComponentPosIndex <= 0) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
            if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
            if (itemIndex <= 0) { return; }
            if (_isLocked) { return; }
#endif
            EcsComponentLifecycle<T>.OnDel(_isCustomLifecycle, _customLifecycle, ref _items[mappingSlot.ComponentPosIndex], _worldID, entityID);

            //_dense[_sparse[entityID]] = _dense[_count];
            //_sparse[_dense[_count--]] = _sparse[entityID];

            //_dense[_mapping[entityID].EntityPosIndex] = _dense[_denseCount];
            //_mapping[_dense[_denseCount--]].EntityPosIndex = _mapping[entityID].EntityPosIndex;

            //_dense[mappingSlot.EntityPosIndex] = _dense[_denseCount];
            //_mapping[_dense[_denseCount--]].EntityPosIndex = mappingSlot.EntityPosIndex;


            _dense[mappingSlot.EntityPosIndex] = _dense[--_denseCount];
            _mapping[_dense[_denseCount]].EntityPosIndex = mappingSlot.EntityPosIndex;

            _recycledItemsCount++;
            //_dense[_dense.Length - _recycledItemsCount] = mappingSlot.ComponentPosIndex | int.MinValue;
            _dense[_dense.Length - _recycledItemsCount] = mappingSlot.ComponentPosIndex;
            mappingSlot.ComponentPosIndex = 0;
            _itemsCount--;
            _mediator.UnregisterComponent(entityID, _componentTypeID, _maskBit);

#if !DRAGONECS_DISABLE_POOLS_EVENTS
            if (_hasAnyListener) { _listeners.InvokeOnDel(entityID); }
#endif
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
            EcsComponentCopy<T>.Copy(_isCustomCopy, _customCopy, ref Get(fromEntityID), ref TryAddOrGet(toEntityID));
        }
        public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
#if DEBUG
            if (!Has(fromEntityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(fromEntityID); }
#elif DRAGONECS_STABILITY_MODE
            if (!Has(fromEntityID)) { return; }
#endif
            EcsComponentCopy<T>.Copy(_isCustomCopy, _customCopy, ref Get(fromEntityID), ref toWorld.GetPool<T>().TryAddOrGet(toEntityID));
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
            var span = _world.Where(out SingleAspect<T> _);
            foreach (var entityID in span)
            {
                ref var mappingSlot = ref _mapping[entityID];
                EcsComponentLifecycle<T>.OnDel(_isCustomLifecycle, _customLifecycle, ref _items[mappingSlot.ComponentPosIndex], _worldID, entityID);
                mappingSlot.ComponentPosIndex = 0;
                _mediator.UnregisterComponent(entityID, _componentTypeID, _maskBit);
#if !DRAGONECS_DISABLE_POOLS_EVENTS
                if (_hasAnyListener) { _listeners.InvokeOnDel(entityID); }
#endif
            }
            _denseCount = 0;
            _itemsCount = 0;
            _recycledItemsCount = 0;
        }
        #endregion

        #region Callbacks

        void IEcsPoolImplementation.OnWorldResize(int newSize)
        {
            Array.Resize(ref _mapping, newSize);
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

        public EcsSpan ToSpan()
        {
            return new EcsSpan(_worldID, _dense, _denseCount);
        }

        #region Listeners
#if !DRAGONECS_DISABLE_POOLS_EVENTS
        public void AddListener(IEcsPoolEventListener listener)
        {
            if (listener == null) { EcsPoolThrowHelper.ThrowNullListener(); }
            _listeners.Add(listener);
            _hasAnyListener = _listeners.Count > 0;
        }
        public void RemoveListener(IEcsPoolEventListener listener)
        {
            if (listener == null) { EcsPoolThrowHelper.ThrowNullListener(); }
            if (_listeners.RemoveWithOrder(listener))
            {
                _hasAnyListener = _listeners.Count > 0;
            }
        }
#endif
        #endregion

        #region IEnumerator - IntelliSense hack
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { throw new NotImplementedException(); }
        IEnumerator IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
        #endregion

        #region Convertors
        public static implicit operator EcsPool<T>(IncludeMarker a) { return a.GetInstance<EcsPool<T>>(); }
        public static implicit operator EcsPool<T>(ExcludeMarker a) { return a.GetInstance<EcsPool<T>>(); }
        public static implicit operator EcsPool<T>(AnyMarker a) { return a.GetInstance<EcsPool<T>>(); }
        public static implicit operator EcsPool<T>(OptionalMarker a) { return a.GetInstance<EcsPool<T>>(); }
        public static implicit operator EcsPool<T>(EcsWorld.GetPoolInstanceMarker a) { return a.GetInstance<EcsPool<T>>(); }
        #endregion

        #region Apply
        public static void Apply(ref T component, int entityID, short worldID)
        {
            EcsWorld.GetPoolInstance<EcsPool<T>>(worldID).TryAddOrGet(entityID) = component;
        }
        public static void Apply(ref T component, int entityID, EcsPool<T> pool)
        {
            pool.TryAddOrGet(entityID) = component;
        }
        #endregion
    }
    internal struct MappingSlot
    {
        public int ComponentPosIndex;
        public int EntityPosIndex;
    }
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct ReadonlyEcsPool<T> : IEcsReadonlyPool //IEnumerable<T> - IntelliSense hack
        where T : struct, IEcsComponent
    {
        private readonly EcsPool<T> _pool;

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
        internal ReadonlyEcsPool(EcsPool<T> pool)
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
        public static implicit operator ReadonlyEcsPool<T>(EcsPool<T> a) { return new ReadonlyEcsPool<T>(a); }
        public static implicit operator ReadonlyEcsPool<T>(IncludeMarker a) { return a.GetInstance<EcsPool<T>>(); }
        public static implicit operator ReadonlyEcsPool<T>(ExcludeMarker a) { return a.GetInstance<EcsPool<T>>(); }
        public static implicit operator ReadonlyEcsPool<T>(AnyMarker a) { return a.GetInstance<EcsPool<T>>(); }
        public static implicit operator ReadonlyEcsPool<T>(OptionalMarker a) { return a.GetInstance<EcsPool<T>>(); }
        public static implicit operator ReadonlyEcsPool<T>(EcsWorld.GetPoolInstanceMarker a) { return a.GetInstance<EcsPool<T>>(); }
        #endregion
    }

    public static class EcsPoolExtensions
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
        public static EcsPool<TComponent> Inc<TComponent>(this EcsAspect.Builder self) where TComponent : struct, IEcsComponent
        {
            return self.IncludePool<EcsPool<TComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPool<TComponent> Exc<TComponent>(this EcsAspect.Builder self) where TComponent : struct, IEcsComponent
        {
            return self.ExcludePool<EcsPool<TComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPool<TComponent> Opt<TComponent>(this EcsAspect.Builder self) where TComponent : struct, IEcsComponent
        {
            return self.OptionalPool<EcsPool<TComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPool<TComponent> Any<TComponent>(this EcsAspect.Builder self) where TComponent : struct, IEcsComponent
        {
            return self.AnyPool<EcsPool<TComponent>>();
        }
    }
}