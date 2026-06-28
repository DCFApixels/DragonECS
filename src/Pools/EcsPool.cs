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
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS
{
    /// <summary>
    /// A marker interface for struct components that are stored in a general-purpose component storage.
    /// </summary>
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.POOLS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Standard component.")]
    [MetaID("DragonECS_84D2537C9201D6F6B92FEC1C8883A07A")]
    public interface IEcsComponent : IEcsComponentMember { }

    /// <summary>
    /// A component storage (pool) that provides methods for adding, reading, editing, and removing
    /// <typeparamref name="T"/> components on entities.
    /// </summary>
    /// <typeparam name="T">The component type, which must implement <see cref="IEcsComponent"/>.</typeparam>
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.POOLS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Pool for IEcsComponent components.")]
    [MetaID("DragonECS_C501547C9201A4B03FC25632E4FAAFD7")]
    [DebuggerDisplay("Count: {Count} Type: {ComponentType}")]
    public sealed unsafe class EcsPool<T> : IEcsPoolImplementation<T>, IEcsStructPool<T>, IEnumerable<T>, IEntityStorage, IComponentMask //IEnumerable<T> - IntelliSense hack
        where T : struct, IEcsComponent
    {
        private EcsWorld.ComponentsRegistrar _registrar;
        private readonly static EcsStaticMask _staticMask = EcsStaticMask.Inc<T>();

        private int[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID.
        private T[] _items; // dense; _items[0] - fake component.
        private int _itemsCount = 0;
        private int _recycledItemsCount = 0;

        private MemoryAllocator.HMem<int> _memHandler;
        private UnsafeArray<int> _dense;
        private UnsafeArray<int> _itemEntites;
        private int _usedBlockCount;

        private readonly IEcsComponentLifecycle<T> _customLifecycle = EcsComponentLifecycle<T>.CustomHandler;
        private readonly bool _isCustomLifecycle = EcsComponentLifecycle<T>.IsCustom;
        private readonly IEcsComponentCopy<T> _customCopy = EcsComponentCopy<T>.CustomHandler;
        private readonly bool _isCustomCopy = EcsComponentCopy<T>.IsCustom;

#if !DRAGONECS_DISABLE_POOLS_EVENTS
        private StructList<IEcsPoolEventListener> _listeners = new StructList<IEcsPoolEventListener>(2);
        private bool _hasAnyListener = false;
#endif
        private bool _isLocked;


        #region Properites
        /// <summary>
        /// Number of components stored in the pool.
        /// </summary>
        public int Count
        {
            get { return _itemsCount; }
        }

        /// <summary>
        /// Capacity of the internal component array.
        /// </summary>
        public int Capacity
        {
            get { return _items.Length; }
        }

        /// <summary>
        /// Internal component type identifier for this pool.
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
        /// The world instance that owns this pool.
        /// </summary>
        public EcsWorld World
        {
            get { return _registrar.World; }
        }
        public bool IsReadOnly
        {
            get { return false; }
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
        /// Create an empty EcsPool.
        /// </summary>
        public EcsPool() { _isDensified = true; }

        /// <summary>
        /// Create an EcsPool with an explicit initial capacity for components.
        /// </summary>
        /// <param name="capacity">Initial internal capacity (will be rounded to power-of-two).</param>
        public EcsPool(int capacity)
        {
            capacity = ArrayUtility.CeilPow2Safe(capacity);
            _items = new T[capacity];
            _memHandler = MemoryAllocator.AllocAndInit<int>(capacity * 2);
            _dense = UnsafeArray<int>.Manual(_memHandler.Ptr, capacity);
            _itemEntites = UnsafeArray<int>.Manual(_memHandler.Ptr + capacity, capacity);
        }
        void IEcsPoolImplementation.OnInit(EcsWorld.ComponentsRegistrar registrar)
        {
            var world = registrar.World;
            _registrar = registrar;
            _mapping = new int[world.Capacity];
            var worldConfig = world.Configs.GetWorldConfigOrDefault();
            if (_items == null)
            {
                var capacity = ArrayUtility.CeilPow2Safe(worldConfig.PoolComponentsCapacity);
                _items = new T[capacity];
                _memHandler = MemoryAllocator.AllocAndInit<int>(capacity * 2);
                _dense = UnsafeArray<int>.Manual(_memHandler.Ptr, capacity);
                _itemEntites = UnsafeArray<int>.Manual(_memHandler.Ptr + capacity, capacity);
            }
        }
        void IEcsPoolImplementation.OnWorldDestroy()
        {
            _memHandler.Dispose();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Add a component for the specified entity and return a reference to the component.
        /// </summary>
        /// <param name="entityID">Entity identifier to add the component to.</param>
        /// <returns>Reference to the added component instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Add(int entityID)
        {
            ref int itemIndex = ref _mapping[entityID];
#if DEBUG
            if (entityID == EcsConsts.NULL_ENTITY_ID) { EcsPoolThrowHelper.ThrowEntityIsNotAlive(_registrar.World, entityID); }
            if (_registrar.World.IsUsed(entityID) == false) { EcsPoolThrowHelper.ThrowEntityIsNotAlive(_registrar.World, entityID); }
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
                if (itemIndex >= _items.Length)
                {
                    var oldCapacity = _items.Length;
                    var capacity = ArrayUtility.NextPow2(itemIndex);
                    Array.Resize(ref _items, capacity);

                    _memHandler = MemoryAllocator.ReallocAndInit<int>(_memHandler, capacity * 2);
                    _dense = UnsafeArray<int>.Manual(_memHandler.Ptr, capacity);
                    _itemEntites = UnsafeArray<int>.Manual(_memHandler.Ptr + capacity, capacity);
                    _dense.AsSpan().Slice(oldCapacity, oldCapacity).CopyTo(_itemEntites.AsSpan());
                }
                _usedBlockCount++;
            }
            _dense.ptr[_itemsCount] = entityID;
            _registrar.RegisterComponent(entityID);
            ref T result = ref _items[itemIndex];
            _itemEntites.ptr[itemIndex] = entityID;
            InvokeOnAdd(entityID, ref _items[itemIndex]);
#if !DRAGONECS_DISABLE_POOLS_EVENTS
            if (_hasAnyListener) { _listeners.InvokeOnAddAndGet(entityID); }
#endif
            return ref result;
        }

        /// <summary>
        /// Get a reference to the component for the specified entity.
        /// Throws when the component is not present in DEBUG mode.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>Reference to the component instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get(int entityID)
        {
#if DEBUG // �� ����� STAB_MODE
            if (!Has(entityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
#endif
#if !DRAGONECS_DISABLE_POOLS_EVENTS
            if (_hasAnyListener) { _listeners.InvokeOnGet(entityID); }
#endif
            return ref _items[_mapping[entityID]];
        }

        /// <summary>
        /// Read-only access to the component for the specified entity.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>Read-only reference to the component.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID)
        {
#if DEBUG // �� ����� STAB_MODE
            if (!Has(entityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return ref _items[_mapping[entityID]];
        }

        /// <summary>
        /// Gets a reference to the component for the specified entity, or adds it if the component is not present.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        /// <returns>Reference to the component instance (either existing or newly added).</returns>
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
        /// Remove component from the specified entity.
        /// </summary>
        /// <param name="entityID">Entity identifier.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
#if !DRAGONECS_DISABLE_POOLS_EVENTS
            if (_hasAnyListener) { _listeners.InvokeOnDel(entityID); }
#endif
            if(_itemsCount == 0)
            {
                _itemsCount = 0;
                _usedBlockCount = 0;
                _recycledItemsCount = 0;
                _isDensified = true;
            }
        }

        /// <summary>
        /// Try to remove the component from the specified entity if present.
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
        /// Copy component data from one entity to another inside the another world.
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
            _recycledItemsCount = 0; // ������� ����� ����������, ��� ��� Del �� ��������
            if (_itemsCount <= 0) { return; }
            var span = _registrar.World.Where(out SingleAspect<T> _);
#if DRAGONECS_DEEP_DEBUG
            if(span.Count != _itemsCount)
            {
                Throw.DeepDebugException();
            }
#endif
            foreach (var entityID in span)
            {
                ref int itemIndex = ref _mapping[entityID];
                InvokeOnDel(entityID, itemIndex);
                _itemEntites.ptr[itemIndex] = 0;
                itemIndex = 0;
                _registrar.UnregisterComponent(entityID);
#if !DRAGONECS_DISABLE_POOLS_EVENTS
                if (_hasAnyListener) { _listeners.InvokeOnDel(entityID); }
#endif
            }
            _itemsCount = 0;
            _usedBlockCount = 0;
            _recycledItemsCount = 0;
            _isDensified = true;
        }


        private bool _isDensified;
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

            if(useds.Count != _itemsCount)
            {
                Throw.DeepDebugException();
            }

            var result = new EcsSpan(_registrar.WorldID, new ReadOnlySpan<int>(_dense.ptr + 1, _itemsCount));
            Core.Unchecked.UncheckedUtility.CheckSpanValideDebug(result);
            if(newUsedBlockCount > _usedBlockCount)
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
            else if(RuntimeHelpers.IsReferenceOrContainsReferences<T>())
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
                if(Has(e) == false)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Apply(short worldID, int entityID)
        {
            return ref EcsWorld.GetPoolInstance<EcsPool<T>>(worldID).TryAddOrGet(entityID);
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