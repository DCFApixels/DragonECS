using System;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public sealed class EcsRelationPool<T> : EcsPoolBase<T>
        where T : struct, IEcsRelationComponent
    {
        private EcsWorld _source;

        private bool[] _entityFlags;// index = entityID / value = entityFlag;/ value = 0 = no entityID
        private T[] _items; //sparse
        private int _count;
        private PoolRunners _poolRunners;

        private EcsGroup _entities;
        public EcsReadonlyGroup Entities => _entities.Readonly;

#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
        private short _sanitizeFirstWorld = -1;
        private short _sanitizeSecondWorld = -1;
#endif

        #region Properites
        public int Count => _count;
        public int Capacity => _items.Length;
        public sealed override EcsWorld World => _source;
        #endregion

        #region Init
        protected override void Init(EcsWorld world)
        {
            _source = world;
            _poolRunners = new PoolRunners(world.Pipeline);

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
        public void Add(int entityID, EcsEntity first, EcsEntity second)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if((_sanitizeFirstWorld >= 0 && first.world != _sanitizeFirstWorld) &&
                (_sanitizeSecondWorld >= 0 && second.world != _sanitizeSecondWorld))
            {
                throw new EcsRelationException();
            }
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
            _items[entityID].Set(first, second);
            // }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int entityID, EcsEntity first, EcsEntity second)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if ((_sanitizeFirstWorld >= 0 && first.world != _sanitizeFirstWorld) &&
                (_sanitizeSecondWorld >= 0 && second.world != _sanitizeSecondWorld))
            {
                throw new EcsRelationException();
            }
            _sanitizeFirstWorld = first.world;
            _sanitizeSecondWorld = second.world;
#endif
            //   using (_writeMark.Auto())
            //{
            _poolRunners.write.OnComponentWrite<T>(entityID);
                _items[entityID].Set(first, second);
            //}
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

    public interface IEcsRelationComponent
    {
        public EcsEntity First { get; set; }
        public EcsEntity Second { get; set; }
    }
    public static class IEcsRelationComponentExt
    {
        public static void Set<T>(this ref T self, EcsEntity first, EcsEntity second) where T : struct, IEcsRelationComponent
        {
            self.First = first;
            self.Second = second;
        }
    }
    public static class EcsRelationPoolExt
    {
        public static EcsRelationPool<TRelationComponent> GetPool<TRelationComponent>(this EcsWorld self) where TRelationComponent : struct, IEcsRelationComponent
        {
            return self.GetPool<TRelationComponent, EcsRelationPool<TRelationComponent>>();
        }

        public static EcsRelationPool<TRelationComponent> Include<TRelationComponent>(this EcsQueryBuilderBase self) where TRelationComponent : struct, IEcsRelationComponent
        {
            return self.Include<TRelationComponent, EcsRelationPool<TRelationComponent>>();
        }
        public static EcsRelationPool<TRelationComponent> Exclude<TRelationComponent>(this EcsQueryBuilderBase self) where TRelationComponent : struct, IEcsRelationComponent
        {
            return self.Exclude<TRelationComponent, EcsRelationPool<TRelationComponent>>();
        }
        public static EcsRelationPool<TRelationComponent> Optional<TRelationComponent>(this EcsQueryBuilderBase self) where TRelationComponent : struct, IEcsRelationComponent
        {
            return self.Optional<TRelationComponent, EcsRelationPool<TRelationComponent>>();
        }
    }
}
