using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public abstract class EcsPoolBase
    {
        #region Properties
        public abstract Type ComponentType { get; }
        public abstract EcsWorld World { get; }
        #endregion

        #region Methods
        public abstract bool Has(int entityID);

        protected abstract void Init(EcsWorld world);
        protected abstract void OnWorldResize(int newSize);
        protected abstract void OnDestroy();
        #endregion

        #region Internal
        internal void InvokeInit(EcsWorld world) => Init(world);
        internal void InvokeOnWorldResize(int newSize) => OnWorldResize(newSize);
        internal void InvokeOnDestroy() => OnDestroy();
        #endregion
    }
    public abstract class EcsPoolBase<T> : EcsPoolBase, IEnumerable<T>
    {
        public sealed override Type ComponentType => typeof(T);
        //–елазиаци€ интерфейса IEnumerator не работает, нужно только чтобы IntelliSense предлагала названи€ на основе T. Ќе нашел другого способа
        #region IEnumerable 
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
        #endregion
    }

    public struct NullComponent { }
    public sealed class EcsNullPool : EcsPoolBase
    {
        public static EcsNullPool instance => new EcsNullPool(null);
        private EcsWorld _source;
        private EcsNullPool(EcsWorld source) => _source = source;

        #region Properties
        public sealed override Type ComponentType => typeof(NullComponent);
        public sealed override EcsWorld World => _source;
        #endregion

        #region Methods
        public sealed override bool Has(int index) => false;
        #endregion

        #region Callbacks
        protected override void Init(EcsWorld world) { }
        protected override void OnWorldResize(int newSize) { }
        protected override void OnDestroy() { }
        #endregion
    }
    public sealed class EcsPool<T> : EcsPoolBase<T>
        where T : struct, IEcsComponent
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
            _poolRunners.write.OnComponentWrite<T>(entityID);
            return ref _items[itemIndex];
            // }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Write(int entityID)
        {
            //   using (_writeMark.Auto())
            _poolRunners.write.OnComponentWrite<T>(entityID);
            return ref _items[_mapping[entityID]];
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

    public interface IEcsComponent { }
    public static class EcsPoolExt
    {
        public static EcsPool<TComponent> GetPool<TComponent>(this EcsWorld self) where TComponent : struct, IEcsComponent
        {
            return self.GetPool<TComponent, EcsPool<TComponent>>();
        }

        public static EcsPool<TComponent> Include<TComponent>(this EcsQueryBuilderBase self) where TComponent : struct, IEcsComponent
        {
            return self.Include<TComponent, EcsPool<TComponent>>();
        }
        public static EcsPool<TComponent> Exclude<TComponent>(this EcsQueryBuilderBase self) where TComponent : struct, IEcsComponent
        {
            return self.Exclude<TComponent, EcsPool<TComponent>>();
        }
        public static EcsPool<TComponent> Optional<TComponent>(this EcsQueryBuilderBase self) where TComponent : struct, IEcsComponent
        {
            return self.Optional<TComponent, EcsPool<TComponent>>();
        }
    }
}
