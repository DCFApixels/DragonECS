using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    using static EcsPoolThrowHalper;
    public sealed class EcsSinglePool<T> : IEcsPoolImplementation<T>, IEnumerable<T> //IntelliSense hack
         where T : struct, IEcsSingleComponent
    {
        private EcsWorld _source;
        private int _id;

        private int[] _mapping;

        private int _count;
        private T _component;

        private List<IEcsPoolEventListener> _listeners;

        #region Properites
        public ref T Instance
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _component;
        }
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

            _mapping = new int[world.Capacity];
            _count = 0;
            _listeners = new List<IEcsPoolEventListener>();
        }
        #endregion

        #region Methods
        public ref T Add(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (Has(entityID)) ThrowAlreadyHasComponent<T>(entityID);
#endif
            _mapping[entityID] = ++_count;
            this.IncrementEntityComponentCount(entityID);
            _listeners.InvokeOnAddAndWrite(entityID);
            return ref _component;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Write(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!Has(entityID)) ThrowNotHaveComponent<T>(entityID);
#endif
            _listeners.InvokeOnWrite(entityID);
            return ref _component;
        }
        public ref T TryAddOrWrite(int entityID)
        {
            if (!Has(entityID))
                return ref Add(entityID);
            return ref Write(entityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!Has(entityID)) ThrowNotHaveComponent<T>(entityID);
#endif
            return ref _component;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            return _mapping[entityID] > 0;
        }
        public void Del(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!Has(entityID)) ThrowNotHaveComponent<T>(entityID);
#endif
            _mapping[entityID] = 0;
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
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!Has(fromEntityID)) ThrowNotHaveComponent<T>(fromEntityID);
#endif
            TryAddOrWrite(toEntityID);
        }
        public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!Has(fromEntityID)) ThrowNotHaveComponent<T>(fromEntityID);
#endif
            toWorld.GetPool<T>().TryAddOrWrite(toEntityID);
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
        void IEcsPool.AddRaw(int entityID, object dataRaw) => Instance = (T)dataRaw;
        object IEcsPool.GetRaw(int entityID) => Instance;
        void IEcsPool.SetRaw(int entityID, object dataRaw) => Instance = (T)dataRaw;
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
    /// <summary>Singleton component</summary>
    public interface IEcsSingleComponent { }
    public static class EcsSinglePoolExt
    {
        public static EcsSinglePool<TSingleComponent> GetPool<TSingleComponent>(this EcsWorld self)
            where TSingleComponent : struct, IEcsSingleComponent
        {
            return self.GetPool<TSingleComponent, EcsSinglePool<TSingleComponent>>();
        }

        public static EcsSinglePool<TSingleComponent> Include<TSingleComponent>(this EcsSubjectBuilderBase self) where TSingleComponent : struct, IEcsSingleComponent
        {
            return self.Include<TSingleComponent, EcsSinglePool<TSingleComponent>>();
        }
        public static EcsSinglePool<TSingleComponent> Exclude<TSingleComponent>(this EcsSubjectBuilderBase self) where TSingleComponent : struct, IEcsSingleComponent
        {
            return self.Exclude<TSingleComponent, EcsSinglePool<TSingleComponent>>();
        }
        public static EcsSinglePool<TSingleComponent> Optional<TSingleComponent>(this EcsSubjectBuilderBase self) where TSingleComponent : struct, IEcsSingleComponent
        {
            return self.Optional<TSingleComponent, EcsSinglePool<TSingleComponent>>();
        }
    }
}
