using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static DCFApixels.DragonECS.EcsPoolThrowHalper;

namespace DCFApixels.DragonECS
{
    public sealed class EcsTagPool<T> : IEcsPoolImplementation<T>, IEnumerable<T> //IEnumerable<T> - IntelliSense hack
        where T : struct, IEcsTagComponent
    {
        private EcsWorld _source;
        private int _id;

        private bool[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID
        private int _count;

        private List<IEcsPoolEventListener> _listeners;

        private T _fakeComponent;

        #region Properites
        public int Count => _count;
        int IEcsPool.Capacity => -1;
        public int ComponentID => _id;
        public Type ComponentType => typeof(T);
        public EcsWorld World => _source;
        #endregion

        #region Init
        void IEcsPoolImplementation.OnInit(EcsWorld world, int componentID)
        {
            _source = world;
            _id = componentID;

            _mapping = new bool[world.Capacity];
            _count = 0;

            _listeners = new List<IEcsPoolEventListener>();
        }
        #endregion

        #region Method
        public void Add(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (Has(entityID)) ThrowAlreadyHasComponent<T>(entityID);
#endif
            _count++;
            _mapping[entityID] = true;
            this.IncrementEntityComponentCount(entityID);
            _listeners.InvokeOnAdd(entityID);
        }
        public void TryAdd(int entityID)
        {
            if (!_mapping[entityID])
            {
                _count++;
                _mapping[entityID] = true;
                this.IncrementEntityComponentCount(entityID);
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
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) ThrowNotHaveComponent<T>(entityID);
#endif
            _mapping[entityID] = false;
            _count--;
            this.DecrementEntityComponentCount(entityID);
            _listeners.InvokeOnDel(entityID);
        }
        public void TryDel(int entityID)
        {
            if (Has(entityID)) Del(entityID);
        }
        public void Copy(int fromEntityID, int toEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(fromEntityID)) ThrowNotHaveComponent<T>(fromEntityID);
#endif
            TryAdd(toEntityID);
        }
        public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(fromEntityID)) ThrowNotHaveComponent<T>(fromEntityID);
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
        ref T IEcsPool<T>.Add(int entityID)
        {
            Add(entityID);
            return ref _fakeComponent;
        }
        ref readonly T IEcsPool<T>.Read(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) ThrowNotHaveComponent<T>(entityID);
#endif
            return ref _fakeComponent;
        }
        ref T IEcsPool<T>.Write(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) ThrowNotHaveComponent<T>(entityID);
#endif
            return ref _fakeComponent;
        }
        void IEcsPool.AddRaw(int entityID, object dataRaw) => Add(entityID);
        object IEcsPool.GetRaw(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) ThrowNotHaveComponent<T>(entityID);
#endif
            return _fakeComponent;
        }
        void IEcsPool.SetRaw(int entityID, object dataRaw)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) ThrowNotHaveComponent<T>(entityID);
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
        public static EcsTagPool<TTagComponent> GetPool<TTagComponent>(this EcsWorld self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.GetPool<TTagComponent, EcsTagPool<TTagComponent>>();
        }

        public static EcsTagPool<TTagComponent> Include<TTagComponent>(this EcsSubjectBuilderBase self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Include<TTagComponent, EcsTagPool<TTagComponent>>();
        }
        public static EcsTagPool<TTagComponent> Exclude<TTagComponent>(this EcsSubjectBuilderBase self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Exclude<TTagComponent, EcsTagPool<TTagComponent>>();
        }
        public static EcsTagPool<TTagComponent> Optional<TTagComponent>(this EcsSubjectBuilderBase self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Optional<TTagComponent, EcsTagPool<TTagComponent>>();
        }
    }
}
