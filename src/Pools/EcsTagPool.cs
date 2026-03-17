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
    /// <summary> Component without data. </summary>
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.POOLS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Tag component or component without data.")]
    [MetaID("DragonECS_8D3E547C92013C6A2C2DFC8D2F1FA297")]
    public interface IEcsTagComponent : IEcsComponentMember { }

    /// <summary> Pool for IEcsTagComponent components. </summary>
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.POOLS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Pool for IEcsTagComponent components. EcsTagPool is optimized for storing tag components or components without data.")]
    [MetaID("DragonECS_9D80547C9201E852E4F17324EAC1E15A")]
    [DebuggerDisplay("Count: {Count} Type: {ComponentType}")]
    public sealed class EcsTagPool<T> : IEcsPoolImplementation<T>, IEcsStructPool<T>, IEnumerable<T> //IEnumerable<T> - IntelliSense hack
        where T : struct, IEcsTagComponent
    {
        private EcsWorld _world;
        private int _componentTypeID;
        private EcsMaskChunck _maskBit;

        private bool[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID
        private int _count = 0;

#if !DRAGONECS_DISABLE_POOLS_EVENTS
        private StructList<IEcsPoolEventListener> _listeners = new StructList<IEcsPoolEventListener>(2);
        private bool _hasAnyListener = false;
#endif
        private bool _isLocked;

        private T _fakeComponent = default;
        private EcsWorld.PoolsMediator _mediator;

        #region CheckValide
#if DEBUG
        private static bool _isInvalidType;
        static EcsTagPool()
        {
#pragma warning disable IL2090 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The generic parameter of the source method or type does not have matching annotations.
            _isInvalidType = typeof(T).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Length > 0;
#pragma warning restore IL2090
        }
#endif
        #endregion

        #region Properites
        public int Count
        {
            get { return _count; }
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
        public bool this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Has(index); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { Set(index, value); }
        }
        #endregion

        #region Constructors/Init/Destroy
        public EcsTagPool()
        {
#if DEBUG
            if (_isInvalidType) { Throw.Exception($"{typeof(T).Name} type must not contain any data."); }
#endif
        }
        void IEcsPoolImplementation.OnInit(EcsWorld world, EcsWorld.PoolsMediator mediator, int componentTypeID)
        {
            _world = world;
            _mediator = mediator;
            _componentTypeID = componentTypeID;
            _maskBit = EcsMaskChunck.FromID(componentTypeID);

            _mapping = new bool[world.Capacity];
        }
        void IEcsPoolImplementation.OnWorldDestroy() { }
        #endregion

        #region Method
        public void Add(int entityID)
        {
#if DEBUG
            if (entityID == EcsConsts.NULL_ENTITY_ID) { EcsPoolThrowHelper.ThrowEntityIsNotAlive(_world, entityID); }
            if (_world.IsUsed(entityID) == false) { EcsPoolThrowHelper.ThrowEntityIsNotAlive(_world, entityID); }
            if (Has(entityID)) { EcsPoolThrowHelper.ThrowAlreadyHasComponent<T>(entityID); }
            if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
            if (Has(entityID) | _world.IsUsed(entityID) == false | _isLocked) { return; }
#endif
            _count++;
            _mapping[entityID] = true;
            _mediator.RegisterComponent(entityID, _componentTypeID, _maskBit);
#if !DRAGONECS_DISABLE_POOLS_EVENTS
            if (_hasAnyListener) { _listeners.InvokeOnAdd(entityID); }
#endif
        }
        public void TryAdd(int entityID)
        {
            if (Has(entityID) == false)
            {
                Add(entityID);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            return _mapping[entityID];
        }
        public void Del(int entityID)
        {
#if DEBUG
            if (entityID == EcsConsts.NULL_ENTITY_ID) { EcsPoolThrowHelper.ThrowEntityIsNotAlive(_world, entityID); }
            if (!Has(entityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
            if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
            if (!Has(entityID) || _isLocked) { return; }
#endif
            _mapping[entityID] = false;
            _count--;
            _mediator.UnregisterComponent(entityID, _componentTypeID, _maskBit);
#if !DRAGONECS_DISABLE_POOLS_EVENTS
            if (_hasAnyListener) { _listeners.InvokeOnDel(entityID); }
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            TryAdd(toEntityID);
        }
        public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
#if DEBUG
            if (!Has(fromEntityID)) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(fromEntityID); }
#elif DRAGONECS_STABILITY_MODE
            if (!Has(fromEntityID)) { return; }
#endif
            toWorld.GetPool<T>().TryAdd(toEntityID);
        }
        public void Set(int entityID, bool isHas)
        {
            if (isHas != Has(entityID))
            {
                if (isHas)
                {
                    Add(entityID);
                }
                else
                {
                    Del(entityID);
                }
            }
        }
        public void Toggle(int entityID)
        {
            if (Has(entityID))
            {
                Del(entityID);
            }
            else
            {
                Add(entityID);
            }
        }

        public void ClearAll()
        {
#if DEBUG
            if (_isLocked) { EcsPoolThrowHelper.ThrowPoolLocked(); }
#elif DRAGONECS_STABILITY_MODE
            if (_isLocked) { return; }
#endif
            if (_count <= 0) { return; }
            var span = _world.Where(out SingleTagAspect<T> _);
            _count = 0;
            foreach (var entityID in span)
            {
                _mapping[entityID] = false;
                _mediator.UnregisterComponent(entityID, _componentTypeID, _maskBit);
#if !DRAGONECS_DISABLE_POOLS_EVENTS
                if (_hasAnyListener) { _listeners.InvokeOnDel(entityID); }
#endif
            }
        }
        #endregion

        #region Callbacks

        void IEcsPoolImplementation.OnWorldResize(int newSize)
        {
            Array.Resize(ref _mapping, newSize);
        }

        void IEcsPoolImplementation.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
        {
            if (_count <= 0)
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
        void IEcsPool.AddRaw(int entityID, object dataRaw) { Add(entityID); }
        object IEcsReadonlyPool.GetRaw(int entityID)
        {
#if DEBUG
            if (Has(entityID) == false) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return _fakeComponent;
        }
        void IEcsPool.SetRaw(int entityID, object dataRaw)
        {
#if DEBUG
            if (Has(entityID) == false) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
#endif
        }
        ref T IEcsStructPool<T>.Add(int entityID)
        {
            Add(entityID);
            return ref _fakeComponent;
        }
        ref readonly T IEcsStructPool<T>.Read(int entityID)
        {
#if DEBUG
            if (Has(entityID) == false) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return ref _fakeComponent;
        }
        ref T IEcsStructPool<T>.Get(int entityID)
        {
#if DEBUG
            if (Has(entityID) == false) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return ref _fakeComponent;
        }
        #endregion

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
        public static implicit operator EcsTagPool<T>(IncludeMarker a) { return a.GetInstance<EcsTagPool<T>>(); }
        public static implicit operator EcsTagPool<T>(ExcludeMarker a) { return a.GetInstance<EcsTagPool<T>>(); }
        public static implicit operator EcsTagPool<T>(AnyMarker a) { return a.GetInstance<EcsTagPool<T>>(); }
        public static implicit operator EcsTagPool<T>(OptionalMarker a) { return a.GetInstance<EcsTagPool<T>>(); }
        public static implicit operator EcsTagPool<T>(EcsWorld.GetPoolInstanceMarker a) { return a.GetInstance<EcsTagPool<T>>(); }
        #endregion

        #region Apply
        public static void Apply(ref T component, int entityID, short worldID)
        {
            EcsWorld.GetPoolInstance<EcsTagPool<T>>(worldID).TryAdd(entityID);
        }
        public static void Apply(ref T component, int entityID, EcsTagPool<T> pool)
        {
            pool.TryAdd(entityID);
        }
        #endregion
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
#endif
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct ReadonlyEcsTagPool<T> : IEcsReadonlyPool //IEnumerable<T> - IntelliSense hack
    where T : struct, IEcsTagComponent
    {
        private readonly EcsTagPool<T> _pool;

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
        public bool this[int entityID]
        {
            get { return _pool.Has(entityID); }
        }
        #endregion

        #region Constructors
        internal ReadonlyEcsTagPool(EcsTagPool<T> pool)
        {
            _pool = pool;
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) { return _pool.Has(entityID); }
        object IEcsReadonlyPool.GetRaw(int entityID)
        {
#if DEBUG
            if (Has(entityID) == false) { EcsPoolThrowHelper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return default;
        }

#if !DRAGONECS_DISABLE_POOLS_EVENTS
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddListener(IEcsPoolEventListener listener) { _pool.AddListener(listener); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveListener(IEcsPoolEventListener listener) { _pool.AddListener(listener); }
#endif
        #endregion

        #region Convertors
        public static implicit operator ReadonlyEcsTagPool<T>(EcsTagPool<T> a) { return new ReadonlyEcsTagPool<T>(a); }
        public static implicit operator ReadonlyEcsTagPool<T>(IncludeMarker a) { return a.GetInstance<EcsTagPool<T>>(); }
        public static implicit operator ReadonlyEcsTagPool<T>(ExcludeMarker a) { return a.GetInstance<EcsTagPool<T>>(); }
        public static implicit operator ReadonlyEcsTagPool<T>(AnyMarker a) { return a.GetInstance<EcsTagPool<T>>(); }
        public static implicit operator ReadonlyEcsTagPool<T>(OptionalMarker a) { return a.GetInstance<EcsTagPool<T>>(); }
        public static implicit operator ReadonlyEcsTagPool<T>(EcsWorld.GetPoolInstanceMarker a) { return a.GetInstance<EcsTagPool<T>>(); }
        #endregion
    }
    public static class EcsTagPoolExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> GetPool<TTagComponent>(this EcsWorld self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.GetPoolInstance<EcsTagPool<TTagComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> GetPoolUnchecked<TTagComponent>(this EcsWorld self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.GetPoolInstanceUnchecked<EcsTagPool<TTagComponent>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> Inc<TTagComponent>(this EcsAspect.Builder self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.IncludePool<EcsTagPool<TTagComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> Exc<TTagComponent>(this EcsAspect.Builder self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.ExcludePool<EcsTagPool<TTagComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> Any<TTagComponent>(this EcsAspect.Builder self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.AnyPool<EcsTagPool<TTagComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> Opt<TTagComponent>(this EcsAspect.Builder self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.OptionalPool<EcsTagPool<TTagComponent>>();
        }
    }
}