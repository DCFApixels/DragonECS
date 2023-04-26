using System;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    //не влияет на счетчик компонентов на сущности
    public sealed class EcsAttachPool<T> : EcsPoolBase<T>
        where T : struct, IEcsAttachComponent
    {
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
        #endregion

        #region Init
        protected override void Init(EcsWorld world)
        {
            _poolRunners = new PoolRunners(world.Pipeline);

            _entities = EcsGroup.New(world);

            _entityFlags = new bool[world.Capacity];
            _items = new T[world.Capacity];
            _count = 0;
        }
        #endregion

        #region Write/Read/Has/Del
        private ProfilerMarker _addMark = new ProfilerMarker("EcsPoo.Add");
        private ProfilerMarker _writeMark = new ProfilerMarker("EcsPoo.Write");
        private ProfilerMarker _readMark = new ProfilerMarker("EcsPoo.Read");
        private ProfilerMarker _hasMark = new ProfilerMarker("EcsPoo.Has");
        private ProfilerMarker _delMark = new ProfilerMarker("EcsPoo.Del");
        public void Add(int entityID, EcsEntity target)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (_sanitizeTargetWorld >= 0 && target.world != _sanitizeTargetWorld)
            {
                throw new EcsRelationException();
            }
            _sanitizeTargetWorld = target.world;
#endif
            // using (_addMark.Auto())
            //  {
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
            // }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int entityID, EcsEntity target)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (_sanitizeTargetWorld >= 0 && target.world != _sanitizeTargetWorld)
            {
                throw new EcsRelationException();
            }
            _sanitizeTargetWorld = target.world;
#endif
            //   using (_writeMark.Auto())
            _poolRunners.write.OnComponentWrite<T>(entityID);
            _items[entityID].Target = target;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID)
        {
            //  using (_readMark.Auto())
            return ref _items[entityID];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sealed override bool Has(int entityID)
        {
            //  using (_hasMark.Auto())
            return _entityFlags[entityID];
        }
        public void Del(int entityID)
        {
            //  using (_delMark.Auto())
            //   {
            _entities.Remove(entityID);
            _entityFlags[entityID] = false;
            _count--;
            _poolRunners.del.OnComponentDel<T>(entityID);
            //   }
        }
        #endregion

        #region WorldCallbacks
        protected override void OnWorldResize(int newSize)
        {
            Array.Resize(ref _entityFlags, newSize);
            Array.Resize(ref _items, newSize);
        }
        protected override void OnDestroy() { }
        #endregion
    }

    public interface IEcsAttachComponent
    {
        public EcsEntity Target { get; set; }
    }
    public static class EcsAttachComponentPoolExt
    {
        public static EcsAttachPool<TAttachComponent> GetPool<TAttachComponent>(this EcsWorld self) where TAttachComponent : struct, IEcsAttachComponent
        {
            return self.GetPool<TAttachComponent, EcsAttachPool<TAttachComponent>>();
        }

        public static EcsAttachPool<TAttachComponent> Include<TAttachComponent>(this EcsQueryBuilderBase self) where TAttachComponent : struct, IEcsAttachComponent
        {
            return self.Include<TAttachComponent, EcsAttachPool<TAttachComponent>>();
        }
        public static EcsAttachPool<TAttachComponent> Exclude<TAttachComponent>(this EcsQueryBuilderBase self) where TAttachComponent : struct, IEcsAttachComponent
        {
            return self.Exclude<TAttachComponent, EcsAttachPool<TAttachComponent>>();
        }
        public static EcsAttachPool<TAttachComponent> Optional<TAttachComponent>(this EcsQueryBuilderBase self) where TAttachComponent : struct, IEcsAttachComponent
        {
            return self.Optional<TAttachComponent, EcsAttachPool<TAttachComponent>>();
        }
    }
}
