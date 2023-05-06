using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    //не влияет на счетчик компонентов на сущности
    /// <summary>Pool for IEcsNotNullComponent components</summary>
    public sealed class EcsNotNullPool<T> : IEcsPoolImplementation<T>, IEnumerable<T> //IntelliSense hack
        where T : struct, IEcsNotNullComponent
    {
        private EcsWorld _source;
        private int _id;

        private T[] _items; //sparse
        private int _count;

        private IEcsComponentReset<T> _componentResetHandler;
        private IEcsComponentCopy<T> _componentCopyHandler;
        private PoolRunners _poolRunners;

        #region Properites
        public int Count => _count;
        public int Capacity => _items.Length;
        public int ComponentID => _id;
        public Type ComponentType => typeof(T);
        public EcsWorld World => _source;
        #endregion

        #region Init
        void IEcsPoolImplementation.OnInit(EcsWorld world, int componentID)
        {
            _source = world;
            _id = componentID;

            _items = new T[world.Capacity];
            _count = 0;

            _componentResetHandler = EcsComponentResetHandler<T>.instance;
            _poolRunners = new PoolRunners(world.Pipeline);
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Write(int entityID)
        {
            _poolRunners.write.OnComponentWrite<T>(entityID);
            return ref _items[entityID];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID)
        {
            return ref _items[entityID];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            return true;
        }
        public void Copy(int fromEntityID, int toEntityID)
        {
            _componentCopyHandler.Copy(ref Write(fromEntityID), ref Write(toEntityID));
        }
        #endregion

        #region Callbacks
        void IEcsPoolImplementation.OnWorldResize(int newSize)
        {
            Array.Resize(ref _items, newSize);
        }
        void IEcsPoolImplementation.OnWorldDestroy() { }
        void IEcsPoolImplementation.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
        {
            foreach (var entityID in buffer)
                _componentResetHandler.Reset(ref _items[entityID]);
        }
        #endregion

        #region Other
        ref T IEcsPool<T>.Add(int entityID) => ref Write(entityID);
        ref readonly T IEcsPool<T>.Read(int entityID) => ref Read(entityID);
        ref T IEcsPool<T>.Write(int entityID) => ref Write(entityID);
        void IEcsPool.Del(int entityID) { }
        void IEcsPool.AddRaw(int entityID, object dataRaw) => Write(entityID) = (T)dataRaw;
        object IEcsPool.GetRaw(int entityID) => Write(entityID);
        void IEcsPool.SetRaw(int entityID, object dataRaw) => Write(entityID) = (T)dataRaw;
        #endregion

        #region IEnumerator - IntelliSense hack
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        #endregion
    }
    /// <summary>
    /// Not null component. Is present on all entities, without explicit addition and cannot be deleted
    /// </summary>
    public interface IEcsNotNullComponent { }
    public static class EcsNotNullPoolExt
    {
        public static EcsNotNullPool<TNotNullComponent> GetPool<TNotNullComponent>(this EcsWorld self) where TNotNullComponent : struct, IEcsNotNullComponent
        {
            return self.GetPool<TNotNullComponent, EcsNotNullPool<TNotNullComponent>>();
        }

        public static EcsNotNullPool<TNotNullComponent> Include<TNotNullComponent>(this EcsSubjectBuilderBase self) where TNotNullComponent : struct, IEcsNotNullComponent
        {
            return self.Include<TNotNullComponent, EcsNotNullPool<TNotNullComponent>>();
        }
        public static EcsNotNullPool<TNotNullComponent> Exclude<TNotNullComponent>(this EcsSubjectBuilderBase self) where TNotNullComponent : struct, IEcsNotNullComponent
        {
            return self.Exclude<TNotNullComponent, EcsNotNullPool<TNotNullComponent>>();
        }
        public static EcsNotNullPool<TNotNullComponent> Optional<TNotNullComponent>(this EcsSubjectBuilderBase self) where TNotNullComponent : struct, IEcsNotNullComponent
        {
            return self.Optional<TNotNullComponent, EcsNotNullPool<TNotNullComponent>>();
        }
    }
}
