using System;
using System.Runtime.CompilerServices;
using Unity.Profiling;
using UnityEngine;

namespace DCFApixels.DragonECS
{
    public interface IEcsPool
    {
        public Type ComponentType { get; }
        public int ComponentID { get; }
        public IEcsWorld World { get; }
        public EcsReadonlyGroup Entities { get; }
        public int Count { get; }
        public int Capacity { get; }
        public bool Has(int entityID);
        public void Write(int entityID);
        public void Del(int entityID);
        internal void OnWorldResize(int newSize);
    }
    public interface IEcsPool<T> : IEcsPool where T : struct
    {
        public ref T Read(int entity);
        public new ref T Write(int entity);
    }

    public struct NullComponent { }
    public sealed class EcsNullPool : EcsPool, IEcsPool<NullComponent>
    {
        public static EcsNullPool instance => new EcsNullPool(null);
        private IEcsWorld _source;
        private EcsNullPool(IEcsWorld source) => _source = source;
        private NullComponent fakeComponent;
        public Type ComponentType => typeof(NullComponent);
        public int ComponentID => -1;
        public IEcsWorld World => _source;
        public EcsReadonlyGroup Entities => default;
        public int Count => 0;
        public int Capacity => 1;
        public void Del(int index) { }
        public override bool Has(int index) => false;
        void IEcsPool.Write(int entityID) { }
        public ref NullComponent Read(int entity) => ref fakeComponent;
        public ref NullComponent Write(int entity) => ref fakeComponent;
        void IEcsPool.OnWorldResize(int newSize) { }
        internal override void OnWorldResize(int newSize) { }
    }
    public abstract class EcsPool
    {
        internal EcsGroup entities;
        public abstract bool Has(int entityID);
        internal abstract void OnWorldResize(int newSize);
    }
    public sealed class EcsPool<T> : EcsPool, IEcsPool<T>
        where T : struct
    {
        private readonly int _componentID;
        private readonly IEcsWorld _source;

        private int[] _mapping;// index = entity / value = itemIndex;/ value = 0 = no entity
        private T[] _items; //dense
        private int _itemsCount;
        private int[] _recycledItems;
        private int _recycledItemsCount;

        private IEcsComponentReset<T> _componentResetHandler;
        private PoolRunnres _poolRunnres;
        #region Properites
        public EcsReadonlyGroup Entities => entities.Readonly;
        public int Count => _itemsCount;
        public int Capacity => _items.Length;
        public IEcsWorld World => _source;
        public Type ComponentType => typeof(T);
        public int ComponentID => _componentID;
        #endregion

        #region Constructors
        internal EcsPool(IEcsWorld source, int id, int capacity, PoolRunnres poolRunnres)
        {
            entities = new EcsGroup(source);
            _source = source;
            _componentID = id;

            _mapping = new int[source.EntitesCapacity];
            _recycledItems = new int[128];
            _recycledItemsCount = 0;
            _items = new T[capacity];
            _itemsCount = 0;

            _componentResetHandler = IEcsComponentReset<T>.Handler;
            _poolRunnres = poolRunnres;
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
                    entities.Add(entityID);
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
                    _componentResetHandler.Reset(ref _items[itemIndex]);
                    _poolRunnres.add.OnComponentAdd<T>(entityID);
                }
                _poolRunnres.write.OnComponentWrite<T>(entityID);
                return ref _items[itemIndex];
           // }
        }
        public ref T Write(int entityID)
        {
           //   using (_writeMark.Auto())
                return ref _items[_mapping[entityID]];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Read(int entityID)
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
            entities.Remove(entityID);

            if (_recycledItemsCount >= _recycledItems.Length)
                Array.Resize(ref _recycledItems, _recycledItems.Length << 1);
            _recycledItems[_recycledItemsCount++] = _mapping[entityID];
            _mapping[entityID] = 0;
            _itemsCount--;
            _poolRunnres.del.OnComponentDel<T>(entityID);
          //   }
        } 
        #endregion

        #region IEcsPool
        void IEcsPool.Write(int entityID)
        {
            Write(entityID);
        }
        #endregion

        #region Object
        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => _source.GetHashCode() + ~ComponentID;
        #endregion

        #region Internal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void IEcsPool.OnWorldResize(int newSize)
        {
            Array.Resize(ref _mapping, newSize);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal sealed override void OnWorldResize(int newSize)
        {
            Array.Resize(ref _mapping, newSize);
        }
        #endregion
    }
}
