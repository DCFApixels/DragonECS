using DCFApixels.DragonECS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public sealed class EcsTagPool<T> : IEcsPoolImplementation<T>, IEcsStructPool<T>, IEnumerable<T> //IEnumerable<T> - IntelliSense hack
        where T : struct, IEcsTagComponent
    {
        private EcsWorld _source;
        private int _componentTypeID;
        private EcsMaskChunck _maskBit;

        private bool[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID
        private int _count;

        private List<IEcsPoolEventListener> _listeners = new List<IEcsPoolEventListener>();

        private T _fakeComponent;
        private EcsWorld.PoolsMediator _mediator;

        #region CheckValide
#if DEBUG
        private static bool _isInvalidType;
        static EcsTagPool()
        {
#pragma warning disable IL2090 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The generic parameter of the source method or type does not have matching annotations.
            _isInvalidType = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length > 0;
#pragma warning restore IL2090
        }
        public EcsTagPool()
        {
            if (_isInvalidType)
            {
                throw new EcsFrameworkException($"{typeof(T).Name} type must not contain any data.");
            }
        }
#endif
        #endregion

        #region Properites
        public int Count
        {
            get { return _count; }
        }
        int IEcsPool.Capacity
        {
            get { return -1; }
        }
        public int ComponentID
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
        #endregion

        #region Method
        public void Add(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (Has(entityID)) { EcsPoolThrowHalper.ThrowAlreadyHasComponent<T>(entityID); }
#endif
            _count++;
            _mapping[entityID] = true;
            _mediator.RegisterComponent(entityID, _componentTypeID, _maskBit);
            _listeners.InvokeOnAdd(entityID);
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
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) { EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID); }
#endif
            _mapping[entityID] = false;
            _count--;
            _mediator.UnregisterComponent(entityID, _componentTypeID, _maskBit);
            _listeners.InvokeOnDel(entityID);
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
            TryAdd(toEntityID);
        }
        public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(fromEntityID)) { EcsPoolThrowHalper.ThrowNotHaveComponent<T>(fromEntityID); }
#endif
            toWorld.GetPool<T>().TryAdd(toEntityID);
        }
        public void Set(int entityID, bool isHas)
        {
            if (isHas)
            {
                if (!Has(entityID))
                {
                    Add(entityID);
                }
            }
            else
            {
                if (Has(entityID))
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
        #endregion

        #region Callbacks
        void IEcsPoolImplementation.OnInit(EcsWorld world, EcsWorld.PoolsMediator mediator, int componentTypeID)
        {
            _source = world;
            _mediator = mediator;
            _componentTypeID = componentTypeID;
            _maskBit = EcsMaskChunck.FromID(componentTypeID);

            _mapping = new bool[world.Capacity];
            _count = 0;
        }
        void IEcsPoolImplementation.OnDevirtualize(EcsAnonymousPool.Data data)
        {
            _count = data.ComponentsCount;
            foreach (var item in data.RawComponents)
            {
                _mapping[item.EntityID] = true;
            }
            foreach (var item in data.Listeners)
            {
                _listeners.Add(item);
            }
        }
        void IEcsPoolImplementation.OnWorldResize(int newSize)
        {
            Array.Resize(ref _mapping, newSize);
        }
        void IEcsPoolImplementation.OnWorldDestroy() { }

        void IEcsPoolImplementation.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
        {
            foreach (var entityID in buffer)
            {
                TryDel(entityID);
            }
        }
        #endregion

        #region Other
        void IEcsPool.AddRaw(int entityID, object dataRaw) { Add(entityID); }
        object IEcsPool.GetRaw(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (Has(entityID) == false) { EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return _fakeComponent;
        }
        void IEcsPool.SetRaw(int entityID, object dataRaw)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (Has(entityID) == false) { EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID); }
#endif
        }
        ref T IEcsStructPool<T>.Add(int entityID)
        {
            Add(entityID);
            return ref _fakeComponent;
        }
        ref readonly T IEcsStructPool<T>.Read(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (Has(entityID) == false) { EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return ref _fakeComponent;
        }
        ref T IEcsStructPool<T>.Get(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (Has(entityID) == false) { EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID); }
#endif
            return ref _fakeComponent;
        }
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

    /// <summary>Component without data</summary>
    public interface IEcsTagComponent { }
    public static class EcsTagPoolExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> GetPool<TTagComponent>(this EcsWorld self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.GetPool<EcsTagPool<TTagComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> GetPoolUnchecked<TTagComponent>(this EcsWorld self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.GetPoolUnchecked<EcsTagPool<TTagComponent>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> Include<TTagComponent>(this EcsAspect.Builder self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Include<EcsTagPool<TTagComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> Exclude<TTagComponent>(this EcsAspect.Builder self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Exclude<EcsTagPool<TTagComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> Optional<TTagComponent>(this EcsAspect.Builder self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Optional<EcsTagPool<TTagComponent>>();
        }

        //---------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> GetTagPool<TTagComponent>(this EcsWorld self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.GetPool<EcsTagPool<TTagComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> GetTagPoolUnchecked<TTagComponent>(this EcsWorld self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.GetPoolUnchecked<EcsTagPool<TTagComponent>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> IncludeTag<TTagComponent>(this EcsAspect.Builder self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Include<EcsTagPool<TTagComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> ExcludeTag<TTagComponent>(this EcsAspect.Builder self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Exclude<EcsTagPool<TTagComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> OptionalTag<TTagComponent>(this EcsAspect.Builder self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Optional<EcsTagPool<TTagComponent>>();
        }
    }
}
