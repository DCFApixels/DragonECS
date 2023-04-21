using System;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public sealed class EcsSinglePool<T> : EcsPoolBase
         where T : struct, IEcsSingleComponent
    {
        private EcsWorld _source;

        private int[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID
        //private T[] _items; //dense
        //private int _count;
        //private int[] _recycledItems;
        //private int _recycledItemsCount;

        private int _count;
        private T _component;

        private PoolRunners _poolRunners;

        #region Properites
        public int Count => _count;
        public sealed override EcsWorld World => _source;
        public sealed override Type ComponentType => typeof(T);
        #endregion

        #region Init
        protected override void Init(EcsWorld world)
        {
            _source = world;
            _mapping = new int[world.Capacity];
            _count = 0;
            _poolRunners = new PoolRunners(world.Pipeline);
        }
        #endregion

        #region Write/Read/Has/Del
        private ProfilerMarker _addMark = new ProfilerMarker("EcsPoo.Add");
        private ProfilerMarker _writeMark = new ProfilerMarker("EcsPoo.Write");
        private ProfilerMarker _readMark = new ProfilerMarker("EcsPoo.Read");
        private ProfilerMarker _hasMark = new ProfilerMarker("EcsPoo.Has");
        private ProfilerMarker _delMark = new ProfilerMarker("EcsPoo.Del");
        public ref T Add(int entityID)
        {
            // using (_addMark.Auto())
            //  {
            if (_mapping[entityID] <= 0)
            {
                _mapping[entityID] = ++_count;
                _poolRunners.add.OnComponentAdd<T>(entityID);
            }
            return ref _component;
            // }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Write(int entityID)
        {
            //   using (_writeMark.Auto())
            _poolRunners.write.OnComponentWrite<T>(entityID);
            return ref _component;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID)
        {
            //  using (_readMark.Auto())
            return ref _component;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sealed override bool Has(int entityID)
        {
            //  using (_hasMark.Auto())
            return _mapping[entityID] > 0;
        }
        public void Del(int entityID)
        {
            //  using (_delMark.Auto())
            //   {
            _mapping[entityID] = 0;
            _count--;
            _poolRunners.del.OnComponentDel<T>(entityID);
            //   }
        }
        #endregion

        #region WorldCallbacks
        protected override void OnWorldResize(int newSize)
        {
            Array.Resize(ref _mapping, newSize);
        }
        protected override void OnDestroy() { }
        #endregion
    }

    public interface IEcsSingleComponent { }
    public static class EcsSinglePoolExt
    {
        public static EcsSinglePool<TSingleComponent> GetPool<TSingleComponent>(this EcsWorld self)
            where TSingleComponent : struct, IEcsSingleComponent
        {
            return self.GetPool<TSingleComponent, EcsSinglePool<TSingleComponent>>();
        }

        public static EcsSinglePool<TSingleComponent> Include<TSingleComponent>(this EcsQueryBuilderBase self) where TSingleComponent : struct, IEcsSingleComponent
        {
            return self.Include<TSingleComponent, EcsSinglePool<TSingleComponent>>();
        }
        public static EcsSinglePool<TSingleComponent> Exclude<TSingleComponent>(this EcsQueryBuilderBase self) where TSingleComponent : struct, IEcsSingleComponent
        {
            return self.Exclude<TSingleComponent, EcsSinglePool<TSingleComponent>>();
        }
        public static EcsSinglePool<TSingleComponent> Optional<TSingleComponent>(this EcsQueryBuilderBase self) where TSingleComponent : struct, IEcsSingleComponent
        {
            return self.Optional<TSingleComponent, EcsSinglePool<TSingleComponent>>();
        }
    }
}
