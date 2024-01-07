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

        #region Constructors
        private static bool _isInvalidType;
        static EcsTagPool()
        {
            _isInvalidType = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Length > 0;
        }
        public EcsTagPool()
        {
            if (_isInvalidType)
                throw new EcsFrameworkException($"{typeof(T).Name} type must not contain any data.");
        }
        #endregion

        #region Properites
        public int Count => _count;
        int IEcsPool.Capacity => -1;
        public int ComponentID => _componentTypeID;
        public Type ComponentType => typeof(T);
        public EcsWorld World => _source;
        #endregion

        #region Method
        public void Add(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (Has(entityID)) EcsPoolThrowHalper.ThrowAlreadyHasComponent<T>(entityID);
#endif
            _count++;
            _mapping[entityID] = true;
            _mediator.RegisterComponent(entityID, _componentTypeID, _maskBit);
            _listeners.InvokeOnAdd(entityID);
        }
        public void TryAdd(int entityID)
        {
            if (!_mapping[entityID])
            {
                _count++;
                _mapping[entityID] = true;
                _mediator.RegisterComponent(entityID, _componentTypeID, _maskBit);
                _listeners.InvokeOnAdd(entityID);
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
            if (!Has(entityID)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID);
#endif
            _mapping[entityID] = false;
            _count--;
            _mediator.UnregisterComponent(entityID, _componentTypeID, _maskBit);
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
            TryAdd(toEntityID);
        }
        public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(fromEntityID)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(fromEntityID);
#endif
            toWorld.GetPool<T>().TryAdd(toEntityID);
        }
        public void Set(int entityID, bool isHas)
        {
            if (isHas)
            {
                if (!Has(entityID))
                    Add(entityID);
            }
            else
            {
                if (Has(entityID))
                    Del(entityID);
            }
        }
        public void Toggle(int entityID)
        {
            if (Has(entityID))
                Del(entityID);
            else
                Add(entityID);
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
        ref T IEcsStructPool<T>.Add(int entityID)
        {
            Add(entityID);
            return ref _fakeComponent;
        }
        ref readonly T IEcsStructPool<T>.Read(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID);
#endif
            return ref _fakeComponent;
        }
        ref T IEcsStructPool<T>.Get(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID);
#endif
            return ref _fakeComponent;
        }
        void IEcsPool.AddRaw(int entityID, object dataRaw) => Add(entityID);
        object IEcsPool.GetRaw(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID);
#endif
            return _fakeComponent;
        }
        void IEcsPool.SetRaw(int entityID, object dataRaw)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) EcsPoolThrowHalper.ThrowNotHaveComponent<T>(entityID);
#endif
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
        public static EcsTagPool<TTagComponent> Include<TTagComponent>(this EcsAspectBuilderBase self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Include<EcsTagPool<TTagComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> Exclude<TTagComponent>(this EcsAspectBuilderBase self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Exclude<EcsTagPool<TTagComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> Optional<TTagComponent>(this EcsAspectBuilderBase self) where TTagComponent : struct, IEcsTagComponent
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
        public static EcsTagPool<TTagComponent> IncludeTag<TTagComponent>(this EcsAspectBuilderBase self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Include<EcsTagPool<TTagComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> ExcludeTag<TTagComponent>(this EcsAspectBuilderBase self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Exclude<EcsTagPool<TTagComponent>>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTagPool<TTagComponent> OptionalTag<TTagComponent>(this EcsAspectBuilderBase self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Optional<EcsTagPool<TTagComponent>>();
        }
    }
}
