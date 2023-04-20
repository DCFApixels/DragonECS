using System;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public interface IEcsPool
    {
        #region Properties
        public Type ComponentType { get; }
        public IEcsWorld World { get; }
        public int Count { get; }
        public int Capacity { get; }
        #endregion

        #region Methods
        public void Add(int entityID);
        public void Write(int entityID, object data);
        public bool Has(int entityID);
        public void Del(int entityID);
        #endregion

        #region Internal
        internal void OnWorldResize(int newSize);
        #endregion
    }
    public interface IEcsPool<T> : IEcsPool where T : struct
    {
        public new ref T Add(int entityID);
        public ref T Write(int entityID);

    }
    public struct NullComponent { }
    public sealed class EcsNullPool : IEcsPool<NullComponent>
    {
        public static EcsNullPool instance => new EcsNullPool(null);
        private IEcsWorld _source;
        private NullComponent fakeComponent;
        private EcsNullPool(IEcsWorld source) => _source = source;

        #region Properties
        public Type ComponentType => typeof(NullComponent);
        public int ComponentID => -1;
        public IEcsWorld World => _source;
        public int Count => 0;
        public int Capacity => 1;
        #endregion

        #region Methods
        public ref NullComponent Add(int entity) => ref fakeComponent;
        public bool Has(int index) => false;
        public ref NullComponent Read(int entity) => ref fakeComponent;
        public ref NullComponent Write(int entity) => ref fakeComponent;
        public void Del(int index) { }
        void IEcsPool.Write(int entityID, object data) { }
        void IEcsPool.Add(int entityID) { }
        void IEcsPool.OnWorldResize(int newSize) { }
        #endregion
    }

    public sealed class EcsPool<T> : IEcsPool<T>
        where T : struct
    {
        public static EcsPool<T> Builder(IEcsWorld source)
        {
            return new EcsPool<T>(source, 512, default);
        }

        private readonly IEcsWorld _source;

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
        public IEcsWorld World => _source;
        public Type ComponentType => typeof(T);
        #endregion

        #region Constructors
        internal EcsPool(IEcsWorld source, int capacity, PoolRunners poolRunnres)
        {
            _source = source;

            _mapping = new int[source.Capacity];
            _recycledItems = new int[128];
            _recycledItemsCount = 0;
            _items = new T[capacity];
            _itemsCount = 0;

            _componentResetHandler = EcsComponentResetHandler<T>.instance;
            _poolRunners = poolRunnres;
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

                _mapping[entityID] = itemIndex;
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
        public ref T Read(int entityID)
        {
            //  using (_readMark.Auto())
            return ref _items[_mapping[entityID]];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
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

        #region IEcsPool
        void IEcsPool.Write(int entityID, object data)
        {
            Write(entityID) = (T)data;
        }
        void IEcsPool.Add(int entityID)
        {
            Add(entityID);
        }
        #endregion

        #region Internal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IEcsPool.OnWorldResize(int newSize)
        {
            Array.Resize(ref _mapping, newSize);
        }
        #endregion
    }
}
