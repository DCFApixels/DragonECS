using System;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public sealed class EcsTagPool<T> : EcsPoolBase<T>
        where T : struct, IEcsTagComponent
    {
        private EcsWorld _source;

        private bool[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID
        private int _count;

        private PoolRunners _poolRunners;

        #region Properites
        public int Count => _count;
        public sealed override EcsWorld World => _source;
        #endregion

        #region Init
        protected override void Init(EcsWorld world)
        {
            _source = world;

            _mapping = new bool[world.Capacity];
            _count = 0;

            _poolRunners = new PoolRunners(world.Pipeline);
        }
        #endregion

        #region Add/Has/Del
        private ProfilerMarker _addMark = new ProfilerMarker("EcsPoo.Add");
        private ProfilerMarker _hasMark = new ProfilerMarker("EcsPoo.Has");
        private ProfilerMarker _delMark = new ProfilerMarker("EcsPoo.Del");
        public void Add(int entityID)
        {
            // using (_addMark.Auto())
            //  {
            if (_mapping[entityID] == false)
            {
                _count++;
                _mapping[entityID] = true;
                _poolRunners.add.OnComponentAdd<T>(entityID);
            }
            // }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sealed override bool Has(int entityID)
        {
            //  using (_hasMark.Auto())
            return _mapping[entityID];
        }
        public void Del(int entityID)
        {
            //  using (_delMark.Auto())
            //   {
            _mapping[entityID] = false;
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

    public interface IEcsTagComponent { }
    public static class EcsTagPoolExt
    {
        public static EcsTagPool<TTagComponent> GetPool<TTagComponent>(this EcsWorld self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.GetPool<TTagComponent, EcsTagPool<TTagComponent>>();
        }

        public static EcsTagPool<TTagComponent> Include<TTagComponent>(this EcsQueryBuilderBase self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Include<TTagComponent, EcsTagPool<TTagComponent>>();
        }
        public static EcsTagPool<TTagComponent> Exclude<TTagComponent>(this EcsQueryBuilderBase self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Exclude<TTagComponent, EcsTagPool<TTagComponent>>();
        }
        public static EcsTagPool<TTagComponent> Optional<TTagComponent>(this EcsQueryBuilderBase self) where TTagComponent : struct, IEcsTagComponent
        {
            return self.Optional<TTagComponent, EcsTagPool<TTagComponent>>();
        }
    }
}
