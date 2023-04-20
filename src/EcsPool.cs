using System;
using System.Runtime.CompilerServices;
using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public interface IEcsPoolBase
    {
        #region Properties
        public Type ComponentType { get; }
        public EcsWorld World { get; }
        public int Count { get; }
        public int Capacity { get; }
        #endregion

        #region Methods
        public bool Has(int entityID);
        #endregion
    }
    public interface IEcsReadonlyPool : IEcsPoolBase
    {
        #region Methods
        public object Get(int entityID);
        #endregion
    }
    public interface IEcsPool : IEcsReadonlyPool
    {
        #region Methods
        public void AddOrWrite(int entityID, object data);
        public void Del(int entityID);
        #endregion
    }
    public interface IEcsReadonlyPool<T> : IEcsReadonlyPool where T : struct
    {
        public ref readonly T Read(int entityID);
    }
    public interface IEcsPool<T> : IEcsPool, IEcsReadonlyPool<T> where T : struct
    {
        public ref T Add(int entityID);
        public ref T Write(int entityID);

    }

    public abstract class EcsPoolBase<T> : IEcsPoolBase
        where T : struct
    {
        #region Properties
        public abstract Type ComponentType { get; }
        public abstract EcsWorld World { get; }
        public abstract int Count { get; }
        public abstract int Capacity { get; }
        #endregion

        #region Methods
        public abstract bool Has(int entityID);

        protected abstract void OnWorldResize(int newSize);
        protected abstract void OnDestroy();
        #endregion

        #region Internal
        internal void InvokeOnWorldResize(int newSize) => OnWorldResize(newSize);
        internal void InvokeOnDestroy() => OnDestroy();
        #endregion
    }

    public struct NullComponent { }
    public sealed class EcsNullPool : EcsPoolBase<NullComponent>
    {
        public static EcsNullPool instance => new EcsNullPool(null);
        private EcsWorld _source;
        private NullComponent fakeComponent;
        private EcsNullPool(EcsWorld source) => _source = source;

        #region Properties
        public sealed override Type ComponentType => typeof(NullComponent);
        public sealed override EcsWorld World => _source;
        public sealed override int Count => 0;
        public sealed override int Capacity => 1;
        #endregion

        #region Methods
        public sealed override ref NullComponent Add(int entity) => ref fakeComponent;
        public sealed override bool Has(int index) => false;
        public sealed override ref readonly NullComponent Read(int entity) => ref fakeComponent;
        public sealed override ref NullComponent Write(int entity) => ref fakeComponent;
        public sealed override void Del(int index) { }
        #endregion

        #region WorldCallbacks
        protected override void OnWorldResize(int newSize) { }
        protected override void OnDestroy() { }
        #endregion
    }

    public sealed class EcsPool<T> : EcsPoolBase<T>
        where T : struct
    {
        public static EcsPool<T> Builder(EcsWorld source)
        {
            return new EcsPool<T>(source, 512, new PoolRunners(source.Pipeline));
        }

        private readonly EcsWorld _source;

        private int[] _mapping;// index = entityID / value = itemIndex;/ value = 0 = no entityID
        private T[] _items; //dense
        private int _itemsCount;
        private int[] _recycledItems;
        private int _recycledItemsCount;

        private IEcsComponentReset<T> _componentResetHandler;
        private PoolRunners _poolRunners;

        #region Properites
        public sealed override int Count => _itemsCount;
        public sealed override int Capacity => _items.Length;
        public sealed override EcsWorld World => _source;
        public sealed override Type ComponentType => typeof(T);
        #endregion

        #region Constructors
        internal EcsPool(EcsWorld source, int capacity, PoolRunners poolRunners)
        {
            _source = source;

            _mapping = new int[source.Capacity];
            _recycledItems = new int[128];
            _recycledItemsCount = 0;
            _items = new T[capacity];
            _itemsCount = 0;

            _componentResetHandler = EcsComponentResetHandler<T>.instance;
            _poolRunners = poolRunners;
        }
        #endregion

        #region Write/Read/Has/Del
        private ProfilerMarker _addMark = new ProfilerMarker("EcsPoo.Add");
        private ProfilerMarker _writeMark = new ProfilerMarker("EcsPoo.Write");
        private ProfilerMarker _readMark = new ProfilerMarker("EcsPoo.Read");
        private ProfilerMarker _hasMark = new ProfilerMarker("EcsPoo.Has");
        private ProfilerMarker _delMark = new ProfilerMarker("EcsPoo.Del");
        public sealed override ref T Add(int entityID)
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
        public sealed override ref T Write(int entityID)
        {
            //   using (_writeMark.Auto())
            _poolRunners.write.OnComponentWrite<T>(entityID);
            return ref _items[_mapping[entityID]];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sealed override ref readonly T Read(int entityID)
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
        public sealed override void Del(int entityID)
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
}
