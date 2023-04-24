using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public sealed class EcsReadonlyPool<T> : EcsPoolBase<T>
        where T : struct, IEcsReadonlyComponent
    {
        public static string name = typeof(T).Name;

        private EcsWorld _source;

        private int[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID
        private T[] _items; //dense
        private int _itemsCount;
        private int[] _recycledItems;
        private int _recycledItemsCount;

        private IEcsComponentReset<T> _componentResetHandler;
        private PoolRunners _poolRunners;

        #region Properites
        public int Count => _itemsCount;
        public int Capacity => _items.Length;
        public sealed override EcsWorld World => _source;
        #endregion

        #region Init
        protected override void Init(EcsWorld world)
        {
            const int capacity = 512;
            _source = world;

            _mapping = new int[world.Capacity];
            _recycledItems = new int[128];
            _recycledItemsCount = 0;
            _items = new T[capacity];
            _itemsCount = 0;

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
            ref int itemIndex = ref _mapping[entityID];
            if (itemIndex <= 0)
            {
                if (_recycledItemsCount > 0)
                {
                    itemIndex = _recycledItems[--_recycledItemsCount];
                    _itemsCount++;
                }
                else
                {
                    itemIndex = ++_itemsCount;
                    if (itemIndex >= _items.Length)
                        Array.Resize(ref _items, _items.Length << 1);
                }

                //_mapping[entityID] = itemIndex; TODO проверить что это лишнее дейсвие
                _poolRunners.add.OnComponentAdd<T>(entityID);
            }
            else
            {
                _componentResetHandler.Reset(ref _items[itemIndex]);
            }
            _poolRunners.write.OnComponentWrite<T>(entityID);
            return ref _items[itemIndex];
            // }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID)
        {
            //  using (_readMark.Auto())
            return ref _items[_mapping[entityID]];
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
            ref int itemIndex = ref _mapping[entityID];
            _componentResetHandler.Reset(ref _items[itemIndex]);
            if (_recycledItemsCount >= _recycledItems.Length)
                Array.Resize(ref _recycledItems, _recycledItems.Length << 1);
            _recycledItems[_recycledItemsCount++] = itemIndex;
            itemIndex = 0;
            _itemsCount--;
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

    public interface IEcsReadonlyComponent { }
    public static class EcsReadonlyPoolExt
    {
        public static EcsReadonlyPool<TReadolnyComponent> GetPool<TReadolnyComponent>(this EcsWorld self) where TReadolnyComponent : struct, IEcsReadonlyComponent
        {
            return self.GetPool<TReadolnyComponent, EcsReadonlyPool<TReadolnyComponent>>();
        }

        public static EcsReadonlyPool<TReadolnyComponent> Include<TReadolnyComponent>(this EcsQueryBuilderBase self) where TReadolnyComponent : struct, IEcsReadonlyComponent
        {
            return self.Include<TReadolnyComponent, EcsReadonlyPool<TReadolnyComponent>>();
        }
        public static EcsReadonlyPool<TReadolnyComponent> Exclude<TReadolnyComponent>(this EcsQueryBuilderBase self) where TReadolnyComponent : struct, IEcsReadonlyComponent
        {
            return self.Exclude<TReadolnyComponent, EcsReadonlyPool<TReadolnyComponent>>();
        }
        public static EcsReadonlyPool<TReadolnyComponent> Optional<TReadolnyComponent>(this EcsQueryBuilderBase self) where TReadolnyComponent : struct, IEcsReadonlyComponent
        {
            return self.Optional<TReadolnyComponent, EcsReadonlyPool<TReadolnyComponent>>();
        }
    }
}
