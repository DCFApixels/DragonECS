using DCFApixels.DragonECS;
using DCFApixels.DragonECS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class EcsVirtualPool : IEcsPoolImplementation, IEnumerable
{
    private EcsWorld _source;
    private Type _componentType;
    private int _componentTypeID;
    private EcsMaskChunck _maskBit;

    private int[] _mapping;
    private object[] _items;
    private int _itemsCount = 0;
    private int[] _recycledItems;
    private int _recycledItemsCount;

    private List<IEcsPoolEventListener> _listeners = new List<IEcsPoolEventListener>();

    private EcsWorld.PoolsMediator _mediator;

    private bool _isDevirtualized = false;

    #region Properties
    public int ComponentID
    {
        get { return _componentTypeID; }
    }
    public Type ComponentType
    {
        get { return _componentType; }
    }
    public EcsWorld World
    {
        get { return _source; }
    }
    public int Count
    {
        get { return _itemsCount; }
    }
    public int Capacity
    {
        get { return _mapping.Length; }
    }
    public bool IsDevirtualized
    {
        get { return _isDevirtualized; }
    }
    #endregion

    #region Callbacks
    void IEcsPoolImplementation.OnInit(EcsWorld world, EcsWorld.PoolsMediator mediator, int componentTypeID)
    {
        _componentType = world.GetComponentType(componentTypeID);

        _source = world;
        _mediator = mediator;
        _componentTypeID = componentTypeID;
        _maskBit = EcsMaskChunck.FromID(componentTypeID);

        _mapping = new int[world.Capacity];
        _recycledItems = new int[world.Config.Get_PoolRecycledComponentsCapacity()];
        _recycledItemsCount = 0;
        _items = new object[ArrayUtility.NormalizeSizeToPowerOfTwo(world.Config.Get_PoolComponentsCapacity())];
        _itemsCount = 0;
    }
    void IEcsPoolImplementation.OnDevirtualize(Data data)
    {
        Throw.UndefinedException();
    }
    void IEcsPoolImplementation.OnWorldResize(int newSize)
    {
        Array.Resize(ref _mapping, newSize);
    }
    void IEcsPoolImplementation.OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer)
    {
        foreach (var entityID in buffer)
        {
            TryDel(entityID);
        }
    }
    void IEcsPoolImplementation.OnWorldDestroy() { }
    #endregion

    #region Methods
    public void AddRaw(int entityID, object dataRaw)
    {
        ref int itemIndex = ref _mapping[entityID];
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
        if (itemIndex > 0) { EcsPoolThrowHalper.ThrowAlreadyHasComponent(_componentType, entityID); }
#endif
        if (_recycledItemsCount > 0)
        {
            itemIndex = _recycledItems[--_recycledItemsCount];
            _itemsCount++;
        }
        else
        {
            itemIndex = ++_itemsCount;
            if (_itemsCount >= _items.Length)
            {
                Array.Resize(ref _items, _items.Length << 1);
            }
        }
        _items[itemIndex] = dataRaw;
        _mediator.RegisterComponent(entityID, _componentTypeID, _maskBit);
        _listeners.InvokeOnAddAndGet(entityID);
    }
    public bool TryAddRaw(int entityID, object dataRaw)
    {
        if (Has(entityID))
        {
            return false;
        }
        AddRaw(entityID, dataRaw);
        return true;
    }
    public void SetRaw(int entityID, object dataRaw)
    {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
        if (!Has(entityID)) { EcsPoolThrowHalper.ThrowNotHaveComponent(_componentType, entityID); }
#endif
        _items[_mapping[entityID]] = dataRaw;
    }
    public object GetRaw(int entityID)
    {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
        if (!Has(entityID)) { EcsPoolThrowHalper.ThrowNotHaveComponent(_componentType, entityID); }
#endif
        _listeners.InvokeOnGet(entityID);
        return _items[_mapping[entityID]];
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(int entityID)
    {
        return _mapping[entityID] > 0;
    }
    public void Del(int entityID)
    {
        ref int itemIndex = ref _mapping[entityID];
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
        if (itemIndex <= 0) EcsPoolThrowHalper.ThrowNotHaveComponent(_componentType, entityID);
#endif
        _items[itemIndex] = null;
        if (_recycledItemsCount >= _recycledItems.Length)
        {
            Array.Resize(ref _recycledItems, _recycledItems.Length << 1);
        }
        _recycledItems[_recycledItemsCount++] = itemIndex;
        itemIndex = 0;
        _itemsCount--;
        _mediator.UnregisterComponent(entityID, _componentTypeID, _maskBit);
        _listeners.InvokeOnDel(entityID);
    }
    public bool TryDel(int entityID)
    {
        if (Has(entityID))
        {
            Del(entityID);
            return true;
        }
        return false;
    }

    public void Copy(int fromEntityID, int toEntityID)
    {
        throw new NotImplementedException();
    }
    public void Copy(int fromEntityID, EcsWorld toWorld, int toEntityID)
    {
        Throw.Exception("Copying data to another world is not supported for virtual pools, devirtualize the pool first.");
    }
    #endregion

    #region Listeners
    public void AddListener(IEcsPoolEventListener listener)
    {
        if (listener == null) { throw new ArgumentNullException("listener is null"); }
        _listeners.Add(listener);
    }
    public void RemoveListener(IEcsPoolEventListener listener)
    {
        if (listener == null) { throw new ArgumentNullException("listener is null"); }
        _listeners.Remove(listener);
    }
    #endregion

    #region IEnumerator - IntelliSense hack
    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    #endregion

    #region Devirtualization
    public Data GetDevirtualizationData()
    {
        return new Data(this);
    }
    public readonly ref struct Data
    {
        private readonly EcsVirtualPool _target;
        
        public int ComponentsCount
        {
            get { return _target.Count; }
        }
        public RawDataIterator RawComponents
        {
            get { return new RawDataIterator(_target); }
        }
        public ListenersIterator Listeners
        {
            get { return new ListenersIterator(_target); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Data(EcsVirtualPool target)
        {
            _target = target;
        }

        public readonly ref struct ListenersIterator
        {
            private readonly EcsVirtualPool _target;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ListenersIterator(EcsVirtualPool target)
            {
                _target = target;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public List<IEcsPoolEventListener>.Enumerator GetEnumerator() { return _target._listeners.GetEnumerator(); }
        }

        public readonly ref struct RawDataIterator
        {
            private readonly EcsVirtualPool _target;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public RawDataIterator(EcsVirtualPool target)
            {
                _target = target;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator() { return new Enumerator(this); }
            public ref struct Enumerator
            {
                private readonly int[] _mapping;
                private readonly object[] _items;
                private readonly int _entitesCount;
                private int _entityID;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Enumerator(RawDataIterator devirtualizator)
                {
                    _mapping = devirtualizator._target._mapping;
                    _items = devirtualizator._target._items;
                    _entitesCount = devirtualizator._target.World.Count + 1;
                    if (_entitesCount > _mapping.Length)
                    {
                        _entitesCount = _mapping.Length;
                    }
                    _entityID = 0;
                }
                public EntityRawDataPair Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get { return new EntityRawDataPair(_entityID, _items[_entityID]); }
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    while (_entityID++ < _entitesCount)
                    {
                        if (_mapping[_entityID] != 0)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
        }
    }
    public readonly struct EntityRawDataPair
    {
        public readonly int EntityID;
        public readonly object RawData;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityRawDataPair(int entityID, object rawData)
        {
            EntityID = entityID;
            RawData = rawData;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out int entityID, out object rawData)
        {
            entityID = EntityID;
            rawData = RawData;
        }
    }
    #endregion
}



public static class VirtualPoolExtensions
{
    public static bool IsVirtual(this IEcsPool self)
    {
        return self is EcsVirtualPool;
    }
}