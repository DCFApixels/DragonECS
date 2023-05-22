using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    using static EcsPoolThrowHalper;
    //не влияет на счетчик компонентов на сущности
    /// <summary>Pool for IEcsAttachComponent components</summary>
    public sealed class EcsAttachPool<T> : IEcsPoolImplementation<T>, IEnumerable<T> //IntelliSense hack
        where T : struct, IEcsAttachComponent
    {
        private EcsWorld _source;
        private int _id;

        private bool[] _entityFlags;// index = entityID / value = entityFlag;/ value = 0 = no entityID
        private T[] _items; //sparse
        private int _count;
        private PoolRunners _poolRunners;

        private EcsGroup _entities;
        public EcsReadonlyGroup Entities
        {
            get
            {
                _entities.RemoveUnusedEntityIDs();
                return _entities.Readonly;
            }
        }

#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
        private short _sanitizeTargetWorld = -1;
#endif

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

            _poolRunners = new PoolRunners(world.Pipeline);

            _entities = EcsGroup.New(world);

            _entityFlags = new bool[world.Capacity];
            _items = new T[world.Capacity];
            _count = 0;
        }
        #endregion

        #region Methods
        public void Add(int entityID, entlong target)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (_sanitizeTargetWorld > 0 && target.world != _sanitizeTargetWorld) ThrowWorldDifferent<T>(entityID);
            _sanitizeTargetWorld = target.world;
            if (Has(entityID)) ThrowAlreadyHasComponent<T>(entityID);
#endif
            ref bool entityFlag = ref _entityFlags[entityID];
            if (entityFlag == false)
            {
                entityFlag = true;
                _count++;
                _entities.Add(entityID);
                _poolRunners.add.OnComponentAdd<T>(entityID);
            }
            _poolRunners.write.OnComponentWrite<T>(entityID);
            _items[entityID].Target = target;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int entityID, entlong target)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!Has(entityID)) ThrowNotHaveComponent<T>(entityID);
            if (_sanitizeTargetWorld >= 0 && target.world != _sanitizeTargetWorld) ThrowWorldDifferent<T>(entityID);
            _sanitizeTargetWorld = target.world;
#endif
            _poolRunners.write.OnComponentWrite<T>(entityID);
            _items[entityID].Target = target;
        }
        public void AddOrSet(int entityID, entlong target)
        {
            if (Has(entityID))
                Set(entityID, target);
            else
                Add(entityID, target);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!Has(entityID)) ThrowNotHaveComponent<T>(entityID);
#endif
            return ref _items[entityID];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            return _entityFlags[entityID];
        }
        public void Del(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!Has(entityID)) ThrowNotHaveComponent<T>(entityID);
#endif
            _entities.Remove(entityID);
            _entityFlags[entityID] = false;
            _count--;
            _poolRunners.del.OnComponentDel<T>(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryDel(int entityID)
        {
            if (Has(entityID)) Del(entityID);
        }
        public void Copy(int fromEntityID, int toEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!Has(fromEntityID)) ThrowNotHaveComponent<T>(fromEntityID);
#endif
            if (Has(toEntityID))
                Set(toEntityID, Read(fromEntityID).Target);
            else
                Add(toEntityID, Read(fromEntityID).Target);
        }
        public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!Has(fromEntityID)) ThrowNotHaveComponent<T>(fromEntityID);
#endif
            if (Has(toEntityID))
                toWorld.GetPool<T>().Set(toEntityID, Read(fromEntityID).Target);
            else
                toWorld.GetPool<T>().Add(toEntityID, Read(fromEntityID).Target);
        }
        #endregion

        #region WorldCallbacks
        void IEcsPoolImplementation.OnWorldResize(int newSize)
        {
            Array.Resize(ref _entityFlags, newSize);
            Array.Resize(ref _items, newSize);
        }
        void IEcsPoolImplementation.OnWorldDestroy() { }
        void IEcsPoolImplementation.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
        {
            foreach (var item in buffer)
                TryDel(item);
        }
        #endregion

        #region Other
        ref T IEcsPool<T>.Add(int entityID)
        {
            if (!Has(entityID))
                Add(entityID, entlong.NULL);
            return ref _items[entityID];
        }
        ref T IEcsPool<T>.Write(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!Has(entityID)) ThrowNotHaveComponent<T>(entityID);
#endif
            return ref _items[entityID];
        }
        void IEcsPool.AddRaw(int entityID, object dataRaw) => ((IEcsPool<T>)this).Add(entityID) = (T)dataRaw;
        object IEcsPool.GetRaw(int entityID) => Read(entityID);
        void IEcsPool.SetRaw(int entityID, object dataRaw) => ((IEcsPool<T>)this).Write(entityID) = (T)dataRaw;
        #endregion

        #region IEnumerator - IntelliSense hack
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        #endregion
    }

    public interface IEcsAttachComponent
    {
        public entlong Target { get; set; }
    }
    public static class EcsAttachComponentPoolExt
    {
        public static EcsAttachPool<TAttachComponent> GetPool<TAttachComponent>(this EcsWorld self) where TAttachComponent : struct, IEcsAttachComponent
        {
            return self.GetPool<TAttachComponent, EcsAttachPool<TAttachComponent>>();
        }

        public static EcsAttachPool<TAttachComponent> Include<TAttachComponent>(this EcsSubjectBuilderBase self) where TAttachComponent : struct, IEcsAttachComponent
        {
            return self.Include<TAttachComponent, EcsAttachPool<TAttachComponent>>();
        }
        public static EcsAttachPool<TAttachComponent> Exclude<TAttachComponent>(this EcsSubjectBuilderBase self) where TAttachComponent : struct, IEcsAttachComponent
        {
            return self.Exclude<TAttachComponent, EcsAttachPool<TAttachComponent>>();
        }
        public static EcsAttachPool<TAttachComponent> Optional<TAttachComponent>(this EcsSubjectBuilderBase self) where TAttachComponent : struct, IEcsAttachComponent
        {
            return self.Optional<TAttachComponent, EcsAttachPool<TAttachComponent>>();
        }
    }
}
