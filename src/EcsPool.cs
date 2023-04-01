using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public interface IEcsPool
    {
        public int EntitiesCount { get; }
        public int Capacity { get; }

        public IEcsWorld World { get; }
        public Type DataType { get; }
        public int ID { get; }
        public bool Has(int entityID);
        public void Write(int entityID);
        public void Del(int entityID);

        internal void OnWorldResize(int newSize);
    }

    public interface IEcsPool<T> : IEcsPool
        where T : struct
    {
        public ref readonly T Read(int entity);
        public new ref T Write(int entity);
    }

    public class EcsNullPool : IEcsPool
    {
        private readonly IEcsWorld _source;

        public EcsNullPool(IEcsWorld source)
        {
            _source = source;
        }

        public IEcsWorld World => _source;
        public Type DataType => typeof(void);
        public int ID => -1;

        public int EntitiesCount => 0;
        public int Capacity => 1;

        public void Del(int index) { }
        public bool Has(int index) => false;
        public void Write(int index) { }

        void IEcsPool.OnWorldResize(int newSize) { }
    }

    public class EcsPool<T> : IEcsPool<T>
        where T : struct
    {
        private readonly int _id;
        private readonly IEcsWorld _source;

        private int[] _mapping;// index = entity / value = itemIndex;/ value = 0 = no entity
        private T[] _items; //dense
        private int _itemsCount;
        private int[] _recycledItems;
        private int _recycledItemsCount;

        private IEcsComponentReset<T> _componentResetHandler;

        private PoolRunnres _poolRunnres;

        #region Properites
        public int EntitiesCount => _itemsCount;
        public int Capacity => _items.Length;

        public IEcsWorld World => _source;
        public Type DataType => typeof(T);
        public int ID => _id;
        #endregion

        #region Constructors
        internal EcsPool(IEcsWorld source, int id, int capacity, PoolRunnres poolRunnres)
        {
            _source = source;
            _id = id;

            _mapping = new int[source.EntitesCapacity];
            _recycledItems = new int[128];
            _recycledItemsCount = 0;
            _items = new T[capacity];
            _itemsCount = 0;

            _componentResetHandler = ComponentResetHandler.New<T>();
            _poolRunnres = poolRunnres;
        }
        #endregion

        #region Write/Read/Has/Del

        private ProfilerMarker _writeMark = new ProfilerMarker("EcsPoo.Write");
        private ProfilerMarker _readMark = new ProfilerMarker("EcsPoo.Read");
        private ProfilerMarker _hasMark = new ProfilerMarker("EcsPoo.Has");
        private ProfilerMarker _delMark = new ProfilerMarker("EcsPoo.Del");
        public ref T Write(int entityID)
        {
            //  using (_writeMark.Auto())
            // {
            ref int itemIndex = ref _mapping[entityID];
            if (itemIndex <= 0) //если 0 то надо добавить
            {
                if (_recycledItemsCount > 0)
                {
                    itemIndex = _recycledItems[--_recycledItemsCount];
                    _itemsCount++;
                }
                else
                {
                    itemIndex = _itemsCount++;
                    if (itemIndex >= _items.Length)
                        Array.Resize(ref _items, _items.Length << 1);
                }
                _mapping[entityID] = itemIndex;
                _componentResetHandler.Reset(ref _items[itemIndex]);
                _source.OnEntityComponentAdded(entityID, _id);
                _poolRunnres.add.OnComponentAdd<T>(entityID);
            }

            _poolRunnres.write.OnComponentWrite<T>(entityID);
            return ref _items[itemIndex];
            // }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly T Read(int entityID)
        {
            // using (_readMark.Auto())
            return ref _items[_mapping[entityID]];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            // using (_hasMark.Auto())
            return _mapping[entityID] > 0;
        }
        public void Del(int entityID)
        {
            //using (_delMark.Auto())
            // {
            if (_recycledItemsCount >= _recycledItems.Length)
                Array.Resize(ref _recycledItems, _recycledItems.Length << 1);
            _recycledItems[_recycledItemsCount++] = _mapping[entityID];
            _mapping[entityID] = 0;
            _itemsCount--;
            _source.OnEntityComponentRemoved(entityID, _id);
            _poolRunnres.del.OnComponentDel<T>(entityID);
            // }
        } 
        #endregion

        #region IEcsPool
        void IEcsPool.Write(int entityID)
        {
            Write(entityID);
        }
        #endregion

        #region Equals/GetHashCode
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode() => _source.GetHashCode() + ID;
        #endregion

        void IEcsPool.OnWorldResize(int newSize)
        {
            Array.Resize(ref _mapping, newSize);
        }
    }
}
