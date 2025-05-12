#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Internal;
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
    /// <summary>Standard component</summary>
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.POOLS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Standard component.")]
    [MetaID("DragonECS_84D2537C9201D6F6B92FEC1C8883A07A")]
    public interface IEcsComponent : IEcsComponentMember { }

    /// <summary>Pool for IEcsComponent components</summary>
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.POOLS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Pool for IEcsComponent components.")]
    [MetaID("DragonECS_C501547C9201A4B03FC25632E4FAAFD7")]
    [DebuggerDisplay("Count: {Count} Type: {ComponentType}")]
    public sealed class EcsPool<T> : IEcsPoolImplementation<T>, IEcsStructPool<T>, IEntityStorage, IEnumerable<T> //IEnumerable<T> - IntelliSense hack
        where T : struct, IEcsComponent
    {
        private EcsWorld _source;
        private int _componentTypeID;
        private EcsMaskChunck _maskBit;

        private int[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID.
        private T[] _items; // dense; _items[0] - fake component.
        private int _itemsCount = 0;
        private int _capacity = 0;

        //private int[] _sparseEntities;
        //private int[] _denseEntitiesDelayed;
        private int[] _table;
        private int _denseEntitiesDelayedCount = 0;
        private bool _isDenseEntitiesDelayedValid = false;
        private int _recycledCount = 0;

        private readonly IEcsComponentLifecycle<T> _componentLifecycleHandler = EcsComponentLifecycleHandler<T>.instance;
        private readonly bool _isHasComponentLifecycleHandler = EcsComponentLifecycleHandler<T>.isHasHandler;
        private readonly IEcsComponentCopy<T> _componentCopyHandler = EcsComponentCopyHandler<T>.instance;
        private readonly bool _isHasComponentCopyHandler = EcsComponentCopyHandler<T>.isHasHandler;

#if !DRAGONECS_DISABLE_POOLS_EVENTS
        private StructList<IEcsPoolEventListener> _listeners = new StructList<IEcsPoolEventListener>(2);
        private int _listenersCachedCount = 0;
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
            get { return _capacity; }
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
            itemIndex = GetFreeItemIndex(entityID);
            _mediator.RegisterComponent(entityID, _componentTypeID, _maskBit);
            ref T result = ref _items[itemIndex];
            //_sparseEntities[itemIndex] = entityID;
            //_table[itemIndex] = entityID;
            EnableComponent(ref result);
#if !DRAGONECS_DISABLE_POOLS_EVENTS
            _listeners.InvokeOnAddAndGet(entityID, _listenersCachedCount);
#endif


#if DRAGONECS_DEEP_DEBUG
            if (_mediator.GetComponentCount(_componentTypeID) != _itemsCount)
            {
                Throw.UndefinedException();
            }
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
            _listeners.InvokeOnGet(entityID, _listenersCachedCount);
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
            //if (Has(entityID))
            //{
            //    return ref Get(entityID);
            //}
            //else
            //{
            //    return ref Add(entityID);
            //}


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
                itemIndex = GetFreeItemIndex(entityID);
                _mediator.RegisterComponent(entityID, _componentTypeID, _maskBit);
                //_sparseEntities[itemIndex] = entityID;
                //_table[itemIndex] = entityID;
                EnableComponent(ref _items[itemIndex]);
#if !DRAGONECS_DISABLE_POOLS_EVENTS
                _listeners.InvokeOnAdd(entityID, _listenersCachedCount);
#endif
            } //Add block end
#if !DRAGONECS_DISABLE_POOLS_EVENTS
            _listeners.InvokeOnGet(entityID, _listenersCachedCount);
#endif


#if DRAGONECS_DEEP_DEBUG
             if (_mediator.GetComponentCount(_componentTypeID) != _itemsCount)
             {
                 Throw.UndefinedException();
             }
#endif
            return ref _items[itemIndex];
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
            DisableComponent(ref _items[itemIndex]);
            //_sparseEntities[itemIndex] = 0;
            _table[itemIndex] = 0;
            itemIndex = 0;
            _itemsCount--;
            _mediator.UnregisterComponent(entityID, _componentTypeID, _maskBit);
            _isDenseEntitiesDelayedValid = false;
#if !DISABLE_POOLS_EVENTS
            _listeners.InvokeOnDel(entityID, _listenersCachedCount);
#endif

#if DRAGONECS_DEEP_DEBUG
            if (_mediator.GetComponentCount(_componentTypeID) != _itemsCount)
            {
                Throw.UndefinedException();
            }
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
            //_recycledItemsCount = 0; // ������� ����� ����������, ��� ��� Del �� ��������
            if (_itemsCount <= 0) { return; }
            var span = _source.Where(out SingleAspect<T> _);
            foreach (var entityID in span)
            {
                ref int itemIndex = ref _mapping[entityID];
                DisableComponent(ref _items[itemIndex]);
                itemIndex = 0;
                _mediator.UnregisterComponent(entityID, _componentTypeID, _maskBit);
#if !DRAGONECS_DISABLE_POOLS_EVENTS
                _listeners.InvokeOnDel(entityID, _listenersCachedCount);
#endif
            }
            _itemsCount = 0;
            //_recycledItemsCount = 0;
        }
        #endregion

        #region Callbacks
        void IEcsPoolImplementation.OnInit(EcsWorld world, EcsWorld.PoolsMediator mediator, int componentTypeID)
        {
#if DEBUG
            AllowedInWorldsAttribute.CheckAllows<T>(world);
#endif

            _source = world;
            _mediator = mediator;
            _componentTypeID = componentTypeID;
            _maskBit = EcsMaskChunck.FromID(componentTypeID);

            _mapping = new int[world.Capacity];
            Resize(ArrayUtility.NextPow2(world.Configs.GetWorldConfigOrDefault().PoolComponentsCapacity));
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

#if DRAGONECS_DEEP_DEBUG
        private bool _lockToSpan = false;
#endif
        public EcsSpan ToSpan()
        {
#if DRAGONECS_DEEP_DEBUG
            if (_lockToSpan)
            {
                return _source.Entities;
            }
#endif
            UpdateDenseEntities();
            //var span = new EcsSpan(_source.ID, _denseEntitiesDelayed, 1, _itemsCount);
            var span = new EcsSpan(_source.ID, _table, 1 + _capacity, _itemsCount);
#if DRAGONECS_DEEP_DEBUG
            if (DCFApixels.DragonECS.UncheckedCore.UncheckedCoreUtility.CheckSpanValideDebug(span) == false)
            {
                Throw.UndefinedException();
            }
#endif
            return span;
        }
        private unsafe void UpdateDenseEntities()
        {
            if (_isDenseEntitiesDelayedValid) { return; }
            //_denseEntitiesDelayedCount = 1;
            _denseEntitiesDelayedCount = 1 | _capacity;
            //_denseEntitiesDelayed[0] = 0;
            _table[_capacity] = 0;
            _recycledCount = 0;
            //for (int i = 1, jRight = _capacity + _itemsCount + 1; _denseEntitiesDelayedCount <= _itemsCount; i++)
            for (int i = 1, jRight = _itemsCount + 1 + _capacity, max = _itemsCount | _capacity; _denseEntitiesDelayedCount <= max; i++)
            {
                //if (_sparseEntities[i] == 0)
                if (_table[i] == 0)
                {
                    //_denseEntitiesDelayed[jRight] = i;
                    _table[jRight] = i;
                    jRight++;
                    _recycledCount++;
                }
                else
                {
                    //_denseEntitiesDelayed[_denseEntitiesDelayedCount++] = _sparseEntities[i];
                    //_table[_denseEntitiesDelayedCount++ + _capacity] = _table[i];
                    _table[_denseEntitiesDelayedCount++] = _table[i];
                }
            }
            _denseEntitiesDelayedCount -= _capacity;

#if DRAGONECS_DEEP_DEBUG
            _lockToSpan = true;
            //if (_denseEntitiesDelayed[0] != 0)
            if (_table[_capacity] != 0)
            {
                Throw.UndefinedException();
            }
            if (_denseEntitiesDelayedCount != _itemsCount + 1)
            {
                Throw.UndefinedException();
            }
            int coreCount = _mediator.GetComponentCount(_componentTypeID);
            if (coreCount != _itemsCount)
            {
                Throw.UndefinedException();
            }
            using (_source.DisableAutoReleaseDelEntBuffer()) using (var group = EcsGroup.New(_source))
            {
                EcsStaticMask.Inc<T>().Build().ToMask(_source).GetIterator().IterateTo(World.Entities, group);
                //var span = new EcsSpan(_source.ID, _denseEntitiesDelayed, 1, _itemsCount);
                var span = new EcsSpan(_source.ID, _table, 1 + _capacity, _itemsCount);
                if (group.SetEquals(span) == false)
                {
                    Throw.UndefinedException();
                }
            }
            _lockToSpan = false;
            for (int i = _denseEntitiesDelayedCount; i < _denseEntitiesDelayedCount + _recycledCount; i++)
            {
                //var index = _denseEntitiesDelayed[i];
                var index = _table[i + _capacity];
                //if (_sparseEntities[index] != 0)
                if (_table[index] != 0)
                {
                    Throw.UndefinedException();
                }
            }
#endif
            _isDenseEntitiesDelayedValid = true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetFreeItemIndex(int entityID)
        {
            if (_denseEntitiesDelayedCount == _capacity)
            {
                UpdateDenseEntities();
                if (_itemsCount + 1 == _capacity)
                {
                    Resize(_items.Length << 1);
                }
            }


            int result = 0;
            //ref int tf = ref _table[_denseEntitiesDelayedCount | _capacity];
            if (_recycledCount != 0)
            {
                _recycledCount--;
                //result = _denseEntitiesDelayed[_denseEntitiesDelayedCount];
                result = _table[_denseEntitiesDelayedCount | _capacity];
            }
            else
            {
                result = _denseEntitiesDelayedCount;
            }


#if DRAGONECS_DEEP_DEBUG
            //if (_sparseEntities[result] != 0)
            if (_table[result] != 0)
            {
                Throw.UndefinedException();
            }
            if (result == 0)
            {
                Throw.UndefinedException();
            }
#endif

            //_denseEntitiesDelayed[_denseEntitiesDelayedCount] = entityID;
            _table[result] = _table[_denseEntitiesDelayedCount | _capacity] = entityID;
            _itemsCount++;
            _denseEntitiesDelayedCount++;
            //_isDenseEntitiesDelayedValid = false;
            return result;
        }
        //private void CheckOrUpsize()
        //{
        //    if (_itemsCount < _capacity) { return; }
        //    Resize(_items.Length << 1);
        //}
        private void Resize(int newSize)
        {
            if (newSize <= _capacity) { return; }

            //ArrayUtility.ResizeOrCreate(ref _sparseEntities, newSize);
            //ArrayUtility.ResizeOrCreate(ref _items, newSize);
            //_denseEntitiesDelayed = new int[newSize];

            ArrayUtility.ResizeOrCreate(ref _items, newSize);
            var newTable = new int[newSize * 2];
            if(_table != null)
            {
                Array.Copy(_table, newTable, _table.Length / 2);
            }
            _table = newTable;

            _capacity = newSize;
            _denseEntitiesDelayedCount = 0;
            //_denseEntitiesDelayedPtr = _capacity;
            _isDenseEntitiesDelayedValid = false;
            UpdateDenseEntities();
        }
#endregion

        #region Listeners
#if !DRAGONECS_DISABLE_POOLS_EVENTS
        public void AddListener(IEcsPoolEventListener listener)
        {
            if (listener == null) { EcsPoolThrowHelper.ThrowNullListener(); }
            _listeners.Add(listener);
            _listenersCachedCount++;
        }
        public void RemoveListener(IEcsPoolEventListener listener)
        {
            if (listener == null) { EcsPoolThrowHelper.ThrowNullListener(); }
            if (_listeners.RemoveWithOrder(listener))
            {
                _listenersCachedCount--;
            }
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
        public static implicit operator EcsPool<T>(IncludeMarker a) { return a.GetInstance<EcsPool<T>>(); }
        public static implicit operator EcsPool<T>(ExcludeMarker a) { return a.GetInstance<EcsPool<T>>(); }
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

        #region Obsolete
        [Obsolete("Use " + nameof(EcsAspect) + "." + nameof(EcsAspect.Builder) + "." + nameof(Inc) + "<T>()")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPool<TComponent> Include<TComponent>(this EcsAspect.Builder self) where TComponent : struct, IEcsComponent
        {
            return self.IncludePool<EcsPool<TComponent>>();
        }
        [Obsolete("Use " + nameof(EcsAspect) + "." + nameof(EcsAspect.Builder) + "." + nameof(Exc) + "<T>()")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPool<TComponent> Exclude<TComponent>(this EcsAspect.Builder self) where TComponent : struct, IEcsComponent
        {
            return self.ExcludePool<EcsPool<TComponent>>();
        }
        [Obsolete("Use " + nameof(EcsAspect) + "." + nameof(EcsAspect.Builder) + "." + nameof(Opt) + "<T>()")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsPool<TComponent> Optional<TComponent>(this EcsAspect.Builder self) where TComponent : struct, IEcsComponent
        {
            return self.OptionalPool<EcsPool<TComponent>>();
        }
        #endregion
    }
}
