#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Core.Internal;
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
    /// <summary>
    /// Marker interface for unmanaged component structs stored in a value pool.
    /// </summary>
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.POOLS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Value type component.")]
    [MetaID("DragonECS_B053D6FA9C01208AFD1922E6A1D57D83")]
    public interface IEcsValueComponent : IEcsComponentMember { }

    /// <summary>
    /// Storage for unmanaged components that provides a native view for low‑level access.
    /// </summary>
    /// <typeparam name="T">The component type, which must implement <see cref="IEcsValueComponent"/> and be unmanaged.</typeparam>
    /// <remarks>Use <see cref="AsNative"/> to obtain a native view for use inside Unity Jobs.</remarks>
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.POOLS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Pool for IEcsValueComponent components.")]
    [MetaID("DragonECS_5097D6FA9C0109349197EEAC3A0D2858")]
    [DebuggerDisplay("Count: {Count} Value: {ComponentType}")]
    public sealed unsafe class EcsValuePool<T> : IEcsPoolImplementation<T>, IEcsStructPool<T>, IEnumerable<T>, IEntityStorage, IComponentMask //IEnumerable<T> - IntelliSense hack
        where T : unmanaged, IEcsValueComponent
    {
        private EcsWorld.ComponentsRegistrar _registrar;
        private readonly static EcsStaticMask _staticMask = EcsStaticMask.Inc<T>();
        private readonly EcsValuePoolSharedStore* _sharedStore;

        public int* _mapping; // index = entityID / value = itemIndex;/ value = 0 = no entityID.
        public T* _items; // dense; _items[0] - fake component.
        public int _itemsLength;
        public int _itemsCount;
        private int _recycledItemsCount = 0;

        private UnsafeArray<int> _dense;
        private UnsafeArray<int> _itemEntites;
        private int _usedBlockCount;
        private bool _isDensified;

        private HMem<int> _mappingHandler;
        private HMem<T> _itemsHandler;
        private HMem<int> _denseHandler;


        private readonly IEcsComponentLifecycle<T> _customLifecycle = EcsComponentLifecycle<T>.CustomHandler;
        private readonly bool _isCustomLifecycle = EcsComponentLifecycle<T>.IsCustom;
        private readonly IEcsComponentCopy<T> _customCopy = EcsComponentCopy<T>.CustomHandler;
        private readonly bool _isCustomCopy = EcsComponentCopy<T>.IsCustom;

        private bool _isLocked;

        #region Properites
        /// <summary>
        /// Number of stored values in the pool.
        /// </summary>
        public int Count
        {
            get { return _itemsCount; }
        }

        /// <summary>
        /// Capacity of the internal values array.
        /// </summary>
        public int Capacity
        {
            get { return _itemsLength; }
        }

        /// <summary>
        /// Internal component type identifier for this value pool.
        /// </summary>
        public int ComponentTypeID
        {
            get { return _registrar.ComponentTypeID; }
        }

        /// <summary>
        /// Type of the component stored in this pool.
        /// </summary>
        public Type ComponentType
        {
            get { return typeof(T); }
        }

        /// <summary>
        /// The world instance that owns this value pool.
        /// </summary>
        public EcsWorld World
        {
            get { return _registrar.World; }
        }

        /// <summary>
        /// Get component by entity id inside the pool.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>Reference to the component instance.</returns>
        public ref T this[int entityID]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ref Get(entityID); }
        }
        #endregion

        #region Constructors/Init/Destroy
        /// <summary>
        /// Create an empty EcsValuePool.
        /// </summary>
        public EcsValuePool()
        {
            if (_sharedStore == null)
            {
                _sharedStore = Alloc<EcsValuePoolSharedStore>(1).Ptr;
            }
            _isDensified = true;
        }
        /// <summary>
        /// Create an EcsValuePool with an explicit initial capacity for components.
        /// </summary>
        /// <param name="capacity">Initial internal capacity (will be rounded to power-of-two).</param>
        public EcsValuePool(int capacity)
        {
            if (_sharedStore == null)
            {
                _sharedStore = Alloc<EcsValuePoolSharedStore>(1).Ptr;
            }
            capacity = ArrayUtility.NextPow2(capacity);
            _itemsLength = capacity;
            _itemsHandler = Alloc<T>(_itemsLength);
            _items = _itemsHandler.Ptr;
            _sharedStore->_itemsCapacity = capacity;
            _sharedStore->_items = _items;

            _denseHandler = AllocAndInit<int>(capacity * 2);
            _dense = UnsafeArray<int>.Manual(_denseHandler.Ptr, capacity);
            _itemEntites = UnsafeArray<int>.Manual(_denseHandler.Ptr + capacity, capacity);
            _isDensified = true;
        }
        void IEcsPoolImplementation.OnInit(EcsWorld.ComponentsRegistrar registrar)
        {
            _registrar = registrar;

            _sharedStore->_worldID = registrar.WorldID;
            _sharedStore->_componentTypeID = registrar.ComponentTypeID;

            _mappingHandler = AllocAndInit<int>(registrar.World.Capacity);
            _mapping = _mappingHandler.Ptr;

            _sharedStore->_mapping = _mapping;
            _sharedStore->_worldCapacity = registrar.World.Capacity;
            var worldConfig = registrar.World.Configs.GetWorldConfigOrDefault();
            if (_items == null)
            {
                var capacity = ArrayUtility.CeilPow2Safe(worldConfig.PoolComponentsCapacity);

                _itemsLength = capacity;
                _itemsHandler = Alloc<T>(_itemsLength);
                _items = _itemsHandler.Ptr;
                _sharedStore->_itemsCapacity = _itemsLength;
                _sharedStore->_items = _items;

                _denseHandler = AllocAndInit<int>(capacity * 2);
                _dense = UnsafeArray<int>.Manual(_denseHandler.Ptr, capacity);
                _itemEntites = UnsafeArray<int>.Manual(_denseHandler.Ptr + capacity, capacity);
            }
        }
        void IEcsPoolImplementation.OnWorldDestroy() { }
        ~EcsValuePool()
        {
            Free(_mappingHandler);
            Free(_itemsHandler);
            Free(_denseHandler);

            Free(_sharedStore);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Return a native (unmanaged) view over the pool storage.
        /// </summary>
        /// <returns>NativeEcsValuePool wrapper for direct unmanaged access.</returns>
        /// <remarks>Suitable for use in Unity Jobs or other high‑performance contexts.</remarks>
        public NativeEcsValuePool<T> AsNative()
        {
            return new NativeEcsValuePool<T>(_sharedStore);
        }

        /// <summary>
        /// Add a value component for the specified entity and return a reference to it.
        /// </summary>
        /// <param name="entityID">Entity identifier to add the component to.</param>
        /// <returns>Reference to the added value component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Add(int entityID)
        {
            ref int itemIndex = ref _mapping[entityID];
#if DEBUG
            if (entityID == EcsConsts.NULL_ENTITY_ID) { Throw.Ent_ThrowIsNotAlive(_registrar.World, entityID); }
            if (_registrar.World.IsUsed(entityID) == false) { Throw.Ent_ThrowIsNotAlive(_registrar.World, entityID); }
            if (itemIndex > 0) { EcsPoolThrowHelper.ThrowAlreadyHasComponent<T>(entityID); }
            if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
            if (itemIndex > 0) { return ref Get(entityID); }
            if (_isLocked | _registrar.World.IsUsed(entityID) == false) { return ref _items[0]; }
#endif
            _itemsCount++;
            if (_recycledItemsCount > 0)
            {
                itemIndex = _dense.ptr[_itemsCount];
                _recycledItemsCount--;
            }
            else
            {
                itemIndex = _itemsCount;
                if (itemIndex >= _itemsLength)
                {
                    var oldCapacity = _itemsLength;
                    var capacity = ArrayUtility.NextPow2(itemIndex);
                    _itemsLength = capacity;

                    _itemsHandler = Realloc(_itemsHandler, capacity);
                    _items = _itemsHandler.Ptr;
                    _sharedStore->_itemsCapacity = capacity;
                    _sharedStore->_items = _items;

                    _denseHandler = MemoryAllocator.ReallocAndInit<int>(_denseHandler, capacity * 2);
                    _dense = UnsafeArray<int>.Manual(_denseHandler.Ptr, capacity);
                    _itemEntites = UnsafeArray<int>.Manual(_denseHandler.Ptr + capacity, capacity);
                    _dense.AsSpan().Slice(oldCapacity, oldCapacity).CopyTo(_itemEntites.AsSpan());
                }
                _usedBlockCount++;
            }
            _dense.ptr[_itemsCount] = entityID;
            _registrar.RegisterComponent(entityID);
            ref T result = ref _items[itemIndex];
            _itemEntites.ptr[itemIndex] = entityID;
            InvokeOnAdd(entityID, ref _items[itemIndex]);
            return ref result;
        }

        /// <summary>
        /// Get a reference to the value component for the specified entity.
        /// Throws in DEBUG if the component is not present.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>Reference to the stored value component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entityID)
        {
#if DEBUG // не нужен STAB_MODE
            if (!Has(entityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return ref _items[_mapping[entityID]];
        }

        /// <summary>
        /// Read-only access to the component for the specified entity.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>Read-only reference to the value component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID)
        {
#if DEBUG // не нужен STAB_MODE
            if (!Has(entityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return ref _items[_mapping[entityID]];
        }

        /// <summary>
        /// Gets a reference to the component for the specified entity, or adds it if the component is not present.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>Reference to the existing or newly added value component.</returns>
        public ref T TryAddOrGet(int entityID)
        {
            if (Has(entityID))
            {
                return ref Get(entityID);
            }
            return ref Add(entityID);
        }

        /// <summary>
        /// Check whether the specified entity has a component in this pool.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>True when the component is present.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            return _mapping[entityID] != 0;
        }

        /// <summary>
        /// Remove value component from the specified entity.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        public void Del(int entityID)
        {
            ref int itemIndex = ref _mapping[entityID];
#if DEBUG
            if (entityID == EcsConsts.NULL_ENTITY_ID) { EcsPoolThrowHelper.ThrowEntityIsNotAlive(_registrar.World, entityID); }
            if (itemIndex <= 0) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
            if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
            if (itemIndex <= 0) { return; }
            if (_isLocked) { return; }
#endif
            InvokeOnDel(entityID, itemIndex);
            _itemEntites.ptr[itemIndex] = 0;

            _dense.ptr[_itemsCount] = itemIndex;
            _itemsCount--;
            itemIndex = 0;

            _recycledItemsCount++;
            _isDensified = false;
            _registrar.UnregisterComponent(entityID);

            if (_itemsCount == 0)
            {
                _itemsCount = 0;
                _usedBlockCount = 0;
                _recycledItemsCount = 0;
                _isDensified = true;
            }
        }

        /// <summary>
        /// Try to remove the value component from the specified entity if present.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        public void TryDel(int entityID)
        {
            if (Has(entityID))
            {
                Del(entityID);
            }
        }

        /// <summary>
        /// Copy component data from one entity to another inside the same world.
        /// </summary>
        /// <param name="fromEntityID">Source entity identifier.</param>
        /// <param name="toEntityID">Destination entity identifier.</param>
        /// <remarks>Uses custom copy logic if the component implements <see cref="IEcsComponentCopy{T}"/>; otherwise falls back to default copying.</remarks>
        public void Copy(int fromEntityID, int toEntityID)
        {
#if DEBUG
            if (!Has(fromEntityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(fromEntityID); }
#elif DRAGONECS_STABILITY_MODE
            if (!Has(fromEntityID)) { return; }
#endif
            EcsComponentCopy<T>.Copy(_isCustomCopy, _customCopy, ref Get(fromEntityID), ref TryAddOrGet(toEntityID));
        }

        /// <summary>
        /// Copy component data from one entity to another inside another world.
        /// </summary>
        /// <param name="fromEntityID">Source entity identifier.</param>
        /// <param name="toEntityID">Destination entity identifier.</param>
        /// <remarks>Uses custom copy logic if the component implements <see cref="IEcsComponentCopy{T}"/>; otherwise falls back to default copying.</remarks>
        public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
#if DEBUG
            if (!Has(fromEntityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(fromEntityID); }
#elif DRAGONECS_STABILITY_MODE
            if (!Has(fromEntityID)) { return; }
#endif
            EcsComponentCopy<T>.Copy(_isCustomCopy, _customCopy, ref Get(fromEntityID), ref toWorld.GetPool<T>().TryAddOrGet(toEntityID));
        }

        /// <summary>
        /// Remove all components from the pool and unregister them from the world.
        /// </summary>
        public void ClearAll()
        {
#if DEBUG
            if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
            if (_isLocked) { return; }
#endif
            _recycledItemsCount = 0; // спереди чтобы обнулялось, так как Del не обнуляет
            if (_itemsCount <= 0) { return; }
            var span = _registrar.World.Where(out SinglePoolAspect<EcsValuePool<T>> _);
            foreach (var entityID in span)
            {
                ref int itemIndex = ref _mapping[entityID];
                EcsComponentLifecycle<T>.OnDel(_isCustomLifecycle, _customLifecycle, ref ((T*)_items)[itemIndex], _registrar.WorldID, entityID);
                itemIndex = 0;
                _registrar.UnregisterComponent(entityID);
            }
            _itemsCount = 0;
            _recycledItemsCount = 0;
        }
        private void Densify()
        {
            if (_isDensified) { return; }
            var newUsedBlockCount = 0;

            _dense.ptr[0] = 0;
            int denseIndex = 1;
            int recycleIndex = denseIndex + _itemsCount;
            for (int i = 1; i <= _usedBlockCount; i++)
            {
                var e = _itemEntites.ptr[i];
                if (e == 0)
                {
                    _dense.ptr[recycleIndex++] = i;
                }
                else
                {
                    _dense.ptr[denseIndex++] = e;
                    newUsedBlockCount = i;
                }
            }

            #region Depp Debug
#if DRAGONECS_DEEP_DEBUG
            HashSet<int> useds = new HashSet<int>();
            HashSet<int> recycleds = new HashSet<int>();

            for (int i = 1; i <= newUsedBlockCount; i++)
            {
                var value = _dense.ptr[i];
                if (i <= _itemsCount)
                {
                    if (useds.Add(value) == false)
                    {
                        Throw.DeepDebugException();
                    }
                }
                else
                {
                    if (recycleds.Add(value) == false)
                    {
                        Throw.DeepDebugException();
                    }
                    var e = _itemEntites.ptr[value];
                    bool isHasComponent = Has(e);
                    bool isUsedsContains = useds.Contains(e);
                    bool isWorldUsed = _registrar.World.IsUsed(e);
                    if (e != 0 && (isHasComponent || isUsedsContains))
                    {
                        Throw.DeepDebugException();
                    }
                }
            }

            if (useds.Count != _itemsCount)
            {
                Throw.DeepDebugException();
            }

            var result = new EcsSpan(_registrar.WorldID, new ReadOnlySpan<int>(_dense.ptr + 1, _itemsCount));
            Core.Unchecked.UncheckedUtility.CheckSpanValideDebug(result);
            if (newUsedBlockCount > _usedBlockCount)
            {
                Throw.DeepDebugException();
            }
#endif
            #endregion

            _usedBlockCount = newUsedBlockCount;
            _recycledItemsCount = newUsedBlockCount - _itemsCount;
            _isDensified = true;
        }
        #endregion

        #region Callbacks
        void IEcsPoolImplementation.OnWorldResize(int newSize)
        {
            _mappingHandler = ReallocAndInit<int>(_mappingHandler, newSize);
            _mapping = _mappingHandler.Ptr;
            _sharedStore->_worldCapacity = newSize;

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvokeOnAdd(int entityID, ref T component)
        {
            if (_isCustomLifecycle)
            {
                _customLifecycle.OnAdd(ref component, _registrar.WorldID, entityID);
            }
            else if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() == false)
            {
                component = default;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InvokeOnDel(int entityID, int itemIndex)
        {
            if (_isCustomLifecycle)
            {
                _customLifecycle.OnDel(ref _items[itemIndex], _registrar.WorldID, entityID);
            }
            else if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                _items[itemIndex] = default;
            }
        }
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
        EcsMask IComponentMask.ToMask(EcsWorld world)
        {
            return _staticMask.ToMask(world);
        }
        #endregion


#if DRAGONECS_DEEP_DEBUG
        private int _toSpans = 0;
#endif
        /// <summary>
        /// Get a span of all entities that have <typeparamref name="T"/> component.
        /// </summary>
        /// <returns>A read-only span of entity identifiers.</returns>
        public EcsSpan ToSpan()
        {
#if DRAGONECS_DEEP_DEBUG
            if (_toSpans > 0)
            {
                return _registrar.World.Entities;
            }
            _toSpans++;
#endif
            Densify();
            var result = new EcsSpan(_registrar.WorldID, new ReadOnlySpan<int>(_dense.ptr + 1, _itemsCount));
#if DRAGONECS_DEEP_DEBUG
            //var r2 = _registrar.World.WhereToGroup(out SingleAspect<T> _);
            //if(r2.SetEquals(result) == false)
            //{
            //    Throw.DeepDebugException();
            //}
            if (result.Count != _itemsCount)
            {
                Throw.DeepDebugException();
            }
            foreach (var e in result)
            {
                if (Has(e) == false)
                {
                    Throw.DeepDebugException();
                }
                if (_registrar.World.IsUsed(e) == false)
                {
                    Throw.DeepDebugException();
                }
            }

            Core.Unchecked.UncheckedUtility.CheckSpanValideDebug(result);
            _toSpans--;
#endif
            return result;
        }

        #region Listeners
#if !DRAGONECS_DISABLE_POOLS_EVENTS
        void IEcsReadonlyPool.AddListener(IEcsPoolEventListener listener)
        {
            EcsDebug.PrintWarning($"the {nameof(IEcsReadonlyPool.AddListener)} method is not supported for the {nameof(EcsValuePool<T>)}.");
        }
        void IEcsReadonlyPool.RemoveListener(IEcsPoolEventListener listener)
        {
            EcsDebug.PrintWarning($"the {nameof(IEcsReadonlyPool.RemoveListener)} method is not supported for the {nameof(EcsValuePool<T>)}.");
        }
#endif
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Apply(short worldID, int entityID)
        {
            return ref EcsWorld.GetPoolInstance<EcsValuePool<T>>(worldID).TryAddOrGet(entityID);
        }
        #endregion
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    [MetaTags(MetaTags.HIDDEN)]
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.OTHER_GROUP)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct ReadonlyEcsValuePool<T> : IEcsReadonlyPool //IEnumerable<T> - IntelliSense hack
        where T : unmanaged, IEcsValueComponent
    {
        private readonly EcsValuePool<T> _pool;

        #region Properties
        /// <summary>
        /// Internal component type identifier for this value pool.
        /// </summary>
        public int ComponentTypeID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _pool.ComponentTypeID; }
        }
        /// <summary>
        /// Type of the component stored in this pool.
        /// </summary>
        public Type ComponentType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _pool.ComponentType; }
        }
        /// <summary>
        /// The world instance that owns this value pool.
        /// </summary>
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _pool.World; }
        }
        /// <summary>
        /// Number of stored values in the pool.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _pool.Count; }
        }
        /// <summary>
        /// Get component by entity id inside the pool.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>Read-only reference to the component.</returns>
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
        /// <summary>
        /// Check whether the specified entity has this component.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>True when the component is present.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) { return _pool.Has(entityID); }
        /// <summary>
        /// Get a reference to the component for the specified entity.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>Read-only reference to the component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Get(int entityID) { return ref _pool.Read(entityID); }
        /// <summary>
        /// Read-only access to the component for the specified entity.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>Read-only reference to the component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID) { return ref _pool.Read(entityID); }
        /// <summary>
        /// Get raw component data for the specified entity.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>Raw component data as an object.</returns>
        object IEcsReadonlyPool.GetRaw(int entityID) { return _pool.Read(entityID); }

#if !DRAGONECS_DISABLE_POOLS_EVENTS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IEcsReadonlyPool.AddListener(IEcsPoolEventListener listener) { ((IEcsReadonlyPool)_pool).AddListener(listener); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IEcsReadonlyPool.RemoveListener(IEcsPoolEventListener listener) { ((IEcsReadonlyPool)_pool).AddListener(listener); }
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

    internal unsafe struct EcsValuePoolSharedStore
    {
        public short _worldID;
        public int _componentTypeID;
        public int _itemsCapacity;
        public int _worldCapacity;
#if UNITY_2020_3_OR_NEWER
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
#endif
        public int* _mapping;
#if UNITY_2020_3_OR_NEWER
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
#endif
        public void* _items;
    }

    /// <summary>
    /// Unsafe native view of a value pool, providing direct pointer‑based access to unmanaged components.
    /// Designed for high‑performance scenarios and use inside Unity Jobs.
    /// </summary>
    /// <typeparam name="T">The unmanaged component type, must implement <see cref="IEcsValueComponent"/>.</typeparam>
    /// <remarks>
    /// This view does not perform bounds or presence checks for performance reasons.
    /// Use <see cref="Has"/> to validate presence before accessing data.
    /// </remarks>
    public readonly unsafe struct NativeEcsValuePool<T>
        where T : unmanaged, IEcsValueComponent
    {
#if UNITY_2020_3_OR_NEWER
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
#endif
        private readonly EcsValuePoolSharedStore* _store;

        /// <summary>
        /// Gets the ID of the world that owns this pool.
        /// </summary>
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _store->_worldID; }
        }

        /// <summary>
        /// Gets the internal component type identifier.
        /// </summary>
        public int ComponentTypeID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _store->_componentTypeID; }
        }

        /// <summary>
        /// Gets a reference to the component for the specified entity.
        /// </summary>
        /// <param name="entityID">The entity identifier.</param>
        /// <returns>A reference to the component instance.</returns>
        public ref T this[int entityID]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ref Get(entityID); }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal NativeEcsValuePool(EcsValuePoolSharedStore* store)
        {
            _store = store;
        }

        /// <summary>
        /// Checks whether the specified entity has this component.
        /// </summary>
        /// <param name="entityID">The entity identifier.</param>
        /// <returns>True if the component is present; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) 
        {
#if DEBUG
            if (entityID <= 0 || entityID >= _store->_worldCapacity) { Throw.ArgumentOutOfRange(); }
#elif  DRAGONECS_STABILITY_MODE
            if (entityID <= 0 || entityID >= _store->_worldCapacity) { return false; }
#endif
            return _store->_mapping[entityID] != 0; 
        }

        /// <summary>
        /// Gets a reference to the component for the specified entity.
        /// </summary>
        /// <param name="entityID">The entity identifier.</param>
        /// <returns>A reference to the component instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entityID) 
        {
#if DEBUG
            if (entityID <= 0 || entityID >= _store->_worldCapacity) { Throw.ArgumentOutOfRange(); }
            var mappingIndex = _store->_mapping[entityID];
            if (mappingIndex <= 0 || mappingIndex >= _store->_itemsCapacity) { Throw.ArgumentOutOfRange(); }
#elif DRAGONECS_STABILITY_MODE
            if (entityID <= 0 || entityID >= _store->_worldCapacity) { return ref ((T*)_store->_items)[0]; }
            var mappingIndex = _store->_mapping[entityID];
            if(mappingIndex <= 0 || mappingIndex >= _store->_itemsCapacity) { return ref ((T*)_store->_items)[0]; }
#endif
            return ref ((T*)_store->_items)[_store->_mapping[entityID]]; 
        }

        /// <summary>
        /// Gets a read‑only reference to the component for the specified entity.
        /// </summary>
        /// <param name="entityID">The entity identifier.</param>
        /// <returns>A read‑only reference to the component instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID) 
        {
            return ref Get(entityID); 
        }
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