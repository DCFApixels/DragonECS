using System;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public sealed class EcsNotNullPool<T> : EcsPoolBase
        where T : struct
    {
        private EcsWorld _source;

        private bool[] _entityFlags;// index = entityID / value = entityFlag;/ value = 0 = no entityID
        private T[] _items; //sparse
        private int _count;

        private IEcsComponentReset<T> _componentResetHandler;
        private PoolRunners _poolRunners;

        #region Properites
        public int Count => _count;
        public int Capacity => _items.Length;
        public sealed override EcsWorld World => _source;
        public sealed override Type ComponentType => typeof(T);
        #endregion

        #region Init
        protected override void Init(EcsWorld world)
        {
            _source = world;

            _entityFlags = new bool[world.Capacity];
            _items = new T[world.Capacity];
            _count = 0;

            _componentResetHandler = EcsComponentResetHandler<T>.instance;
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
            ref bool entityFlag = ref _entityFlags[entityID];
            if (entityFlag == false)
            {
                entityFlag = true;
                _count++;
                _poolRunners.add.OnComponentAdd<T>(entityID);
            }
            _poolRunners.write.OnComponentWrite<T>(entityID);
            return ref _items[entityID];
            // }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Write(int entityID)
        {
            //   using (_writeMark.Auto())
            _poolRunners.write.OnComponentWrite<T>(entityID);
            return ref _items[entityID];
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
            _componentResetHandler.Reset(ref _items[entityID]);
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

    public interface INotNullComponent { }
    public static class INotNullComponentExt
    {
        public static EcsNotNullPool<TComponent> GetPool<TComponent>(this EcsWorld self)
            where TComponent : struct, INotNullComponent
        {
            return self.GetPool<TComponent, EcsNotNullPool<TComponent>>();
        } 
    }
}
