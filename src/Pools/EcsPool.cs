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
    public sealed class EcsPool<T> : IEcsPoolImplementation<T>, IEcsStructPool<T>, IEnumerable<T>, IEntityStorage, IComponentMask //IEnumerable<T> - IntelliSense hack
        where T : struct, IEcsComponent
    {
        private EcsWorld.ComponentsRegister _register;
        private readonly static EcsStaticMask _staticMask = EcsStaticMask.Inc<T>();

        private int[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID.
        private T[] _items; // dense; _items[0] - fake component.
        private int _itemsCount = 0;
        private int _recycledItemsCount = 0;

        private int[] _dense;
        private int[] _itemEntites;
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
            get { return _register.ComponentTypeID; }
        }
        public Type ComponentType
        {
            get { return typeof(T); }
        }
        public EcsWorld World
        {
            get { return _register.World; }
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
        public EcsPool() { _isDensified = true; }
        public EcsPool(int capacity, int recycledCapacity = -1)
        {
            capacity = ArrayUtility.CeilPow2Safe(capacity);
            if (recycledCapacity < 0)
            {
                recycledCapacity = capacity / 2;
            }
            _items = new T[capacity];
            _dense = new int[capacity];
            _itemEntites = new int[capacity];
        }
        void IEcsPoolImplementation.OnInit(EcsWorld.ComponentsRegister register)
        {
            var world = register.World;
            _register = register;

            _mapping = new int[world.Capacity];
            var worldConfig = world.Configs.GetWorldConfigOrDefault();
            if (_items == null)
            {
                _items = new T[ArrayUtility.CeilPow2Safe(worldConfig.PoolComponentsCapacity)];
                _dense = new int[_items.Length];
                _itemEntites = new int[_items.Length];
            }
        }
        void IEcsPoolImplementation.OnWorldDestroy() { }
        #endregion

        #region Methods
        public ref T Add(int entityID)
        {
            ref int itemIndex = ref _mapping[entityID];
#if DEBUG
            if (entityID == EcsConsts.NULL_ENTITY_ID) { EcsPoolThrowHelper.ThrowEntityIsNotAlive(_register.World, entityID); }
            if (_register.World.IsUsed(entityID) == false) { EcsPoolThrowHelper.ThrowEntityIsNotAlive(_register.World, entityID); }
            if (itemIndex > 0) { EcsPoolThrowHelper.ThrowAlreadyHasComponent<T>(entityID); }
            if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
            if (itemIndex > 0) { return ref Get(entityID); }
            if (_isLocked | _register.World.IsUsed(entityID) == false) { return ref _items[0]; }
#endif
            _itemsCount++;
            if (_recycledItemsCount > 0)
            {
                itemIndex = _dense[_itemsCount];
                _recycledItemsCount--;
            }
            else
            {
                itemIndex = _itemsCount;
                if (itemIndex >= _items.Length)
                {
                    Array.Resize(ref _items, ArrayUtility.NextPow2(itemIndex));
                    Array.Resize(ref _dense, _items.Length);
                    Array.Resize(ref _itemEntites, _items.Length);
                }
                _usedBlockCount++;
            }
            _dense[_itemsCount] = entityID;
            _register.RegisterComponent(entityID);
            ref T result = ref _items[itemIndex];
            _itemEntites[itemIndex] = entityID;
            InvokeOnAdd(entityID, ref _items[itemIndex]);
#if !DRAGONECS_DISABLE_POOLS_EVENTS
            if (_hasAnyListener) { _listeners.InvokeOnAddAndGet(entityID); }
#endif
            return ref result;
        }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID)
        {
#if DEBUG // �� ����� STAB_MODE
            if (!Has(entityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return ref _items[_mapping[entityID]];
        }
        public ref T TryAddOrGet(int entityID)
        {
            if (Has(entityID))
            {
                return ref Get(entityID);
            }
            return ref Add(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            return _mapping[entityID] != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int entityID)
        {
            ref int itemIndex = ref _mapping[entityID];
#if DEBUG
            if (entityID == EcsConsts.NULL_ENTITY_ID) { EcsPoolThrowHelper.ThrowEntityIsNotAlive(_register.World, entityID); }
            if (itemIndex <= 0) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
            if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
            if (itemIndex <= 0) { return; }
            if (_isLocked) { return; }
#endif
            InvokeOnDel(entityID, itemIndex);
            _itemEntites[itemIndex] = 0;

            _dense[_itemsCount] = itemIndex;
            _itemsCount--;
            itemIndex = 0;

            _recycledItemsCount++;
            _isDensified = false;
            _register.UnregisterComponent(entityID);
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
            _recycledItemsCount = 0; // ������� ����� ����������, ��� ��� Del �� ��������
            if (_itemsCount <= 0) { return; }
            var span = _register.World.Where(out SingleAspect<T> _);
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
                _itemEntites[itemIndex] = 0;
                itemIndex = 0;
                _register.UnregisterComponent(entityID);
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

            _dense[0] = 0;
            int denseIndex = 1;
            int recycleIndex = denseIndex + _itemsCount;
            for (int i = 1; i <= _usedBlockCount; i++)
            {
                var e = _itemEntites[i];
                if (e == 0)
                {
                    _dense[recycleIndex++] = i;
                }
                else
                {
                    _dense[denseIndex++] = e;
                    newUsedBlockCount = i;
                }
            }

            #region Depp Debug
#if DRAGONECS_DEEP_DEBUG
            HashSet<int> useds = new HashSet<int>();
            HashSet<int> recycleds = new HashSet<int>();

            for (int i = 1; i <= newUsedBlockCount; i++)
            {
                var value = _dense[i];
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
                    var e = _itemEntites[value];
                    bool isHasComponent = Has(e);
                    bool isUsedsContains = useds.Contains(e);
                    bool isWorldUsed = _register.World.IsUsed(e);
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

            var result = new EcsSpan(_register.WorldID, new ReadOnlySpan<int>(_dense, 1, _itemsCount));
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
        public void InvokeOnAdd(int entityID, ref T component)
        {
            if (_isCustomLifecycle)
            {
                _customLifecycle.OnAdd(ref component, _register.WorldID, entityID);
            }
            else if (RuntimeHelpers.IsReferenceOrContainsReferences<T>() == false)
            {
                component = default;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeOnDel(int entityID, int itemIndex)
        {
            if (_isCustomLifecycle)
            {
                _customLifecycle.OnDel(ref _items[itemIndex], _register.WorldID, entityID);
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
                return _register.World.Entities;
            }
            _toSpans++;
#endif
            Densify();
            var result = new EcsSpan(_register.WorldID, new ReadOnlySpan<int>(_dense, 1, _itemsCount));
#if DRAGONECS_DEEP_DEBUG
            //var r2 = _register.World.WhereToGroup(out SingleAspect<T> _);
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
                if (_register.World.IsUsed(e) == false)
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