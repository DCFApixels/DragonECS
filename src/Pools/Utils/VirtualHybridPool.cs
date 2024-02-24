using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Internal
{
    internal class VirtualHybridPool : IEcsHybridPoolInternal
    {
        private EcsWorld _source;
        private Type _componentType;

        internal int[] _mapping;
        internal object[] _items;
        private int[] _entities;
        internal int _itemsCount = 0;

        internal int[] _recycledItems;
        internal int _recycledItemsCount;

        private bool _isDevirtualized = false;

        #region Properties
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
        
        #region Constructors
        public VirtualHybridPool(EcsWorld world, Type componentType)
        {
            _source = world;
            _componentType = componentType;

            _mapping = new int[world.Capacity];
            _recycledItems = new int[world.Config.Get_PoolRecycledComponentsCapacity()];
            _recycledItemsCount = 0;
            _items = new object[ArrayUtility.NormalizeSizeToPowerOfTwo(world.Config.Get_PoolComponentsCapacity())];
            _itemsCount = 0;
        }
        #endregion

        #region Callbacks
        public void OnWorldResize(int newSize)
        {
            Array.Resize(ref _mapping, newSize);
        }
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

        #region IEcsHybridPoolInternal
        public void AddRefInternal(int entityID, object component, bool isMain)
        {
            AddRaw(entityID, component);
        }
        public void DelInternal(int entityID, bool isMain)
        {
            Del(entityID);
        }
        #endregion

        #region Devirtualize
        void IEcsHybridPoolInternal.Devirtualize(VirtualHybridPool virtualHybridPool)
        {
            Throw.UndefinedException();
        }
        #endregion
    }
}