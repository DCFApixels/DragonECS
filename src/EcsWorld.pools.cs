using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.Utils;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public abstract partial class EcsWorld
    {
        private SparseArray<int> _poolIds = new SparseArray<int>();
        private SparseArray<int> _componentIds = new SparseArray<int>();
        private int _poolsCount;
        internal IEcsPoolImplementation[] _pools;
        //private int[] _sortedPoolIds;
        //private int[] _sortedPoolIdsMapping;
        internal int[] _poolComponentCounts;

        private static EcsNullPool _nullPool = EcsNullPool.instance;

        #region ComponentInfo
        public int GetComponentID<T>() => DeclareComponentType(EcsTypeCode.Get<T>());
        public int GetComponentID(Type type) => DeclareComponentType(EcsTypeCode.Get(type));
        public bool IsComponentTypeDeclared<T>() => _componentIds.Contains(EcsTypeCode.Get<T>());
        public bool IsComponentTypeDeclared(Type type) => _componentIds.Contains(EcsTypeCode.Get(type));
        public Type GetComponentType(int componentID) => _pools[componentID].ComponentType;
        #endregion

        #region Getters

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TPool TestGetPool<TPool>() where TPool : IEcsPoolImplementation, new()
        {
            return Get<PoolCache<TPool>>().instance;
        }


#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TPool GetPool<TPool>() where TPool : IEcsPoolImplementation, new()
        {
            return Get<PoolCache<TPool>>().instance;
        }
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TPool GetPoolUnchecked<TPool>() where TPool : IEcsPoolImplementation, new()
        {
            return GetUnchecked<PoolCache<TPool>>().instance;
        }
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool GetPool<TPool>(int worldID) where TPool : IEcsPoolImplementation, new()
        {
            return Get<PoolCache<TPool>>(worldID).instance;
        }
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool UncheckedGetPool<TPool>(int worldID) where TPool : IEcsPoolImplementation, new()
        {
            return GetUnchecked<PoolCache<TPool>>(worldID).instance;
        }
        #endregion

        #region Declare/Create
        private int DeclareComponentType(int typeCode)
        {
            if (!_componentIds.TryGetValue(typeCode, out int componentId))
            {
                componentId = _poolsCount++;
                _componentIds.Add(typeCode, componentId);
            }
            return componentId;
        }
        private TPool CreatePool<TPool>() where TPool : IEcsPoolImplementation, new()
        {
            int poolTypeCode = EcsTypeCode.Get<TPool>();
            if (_poolIds.Contains(poolTypeCode))
                throw new EcsFrameworkException("The pool has already been created.");

            Type componentType = typeof(TPool).GetInterfaces().First(o => o.IsGenericType && o.GetGenericTypeDefinition() == typeof(IEcsPoolImplementation<>)).GetGenericArguments()[0];
            int componentTypeCode = EcsTypeCode.Get(componentType);

            if (_componentIds.TryGetValue(componentTypeCode, out int componentTypeID))
            {
                _poolIds[poolTypeCode] = componentTypeID;
            }
            else
            {
                componentTypeID = _poolsCount++;
                _poolIds[poolTypeCode] = componentTypeID;
                _componentIds[componentTypeCode] = componentTypeID;
            }

            if (_poolsCount >= _pools.Length)
            {
                int oldCapacity = _pools.Length;
                Array.Resize(ref _pools, _pools.Length << 1);
                Array.Resize(ref _poolComponentCounts, _pools.Length);
                //Array.Resize(ref _sortedPoolIds, _pools.Length);
                //Array.Resize(ref _sortedPoolIdsMapping, _pools.Length);
                ArrayUtility.Fill(_pools, _nullPool, oldCapacity, oldCapacity - _pools.Length);

                for (int i = 0; i < _entitesCapacity; i++)
                    Array.Resize(ref _entitiesComponentMasks[i], _pools.Length / 32 + 1);
            }

            if (_pools[componentTypeID] == _nullPool)
            {
                var pool = new TPool();
                _pools[componentTypeID] = pool;
                pool.OnInit(this, _poolsMediator, componentTypeID);
            }
            return (TPool)_pools[componentTypeID];
        }
        #endregion


        #region Pools mediation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterEntityComponent(int entityID, int componentTypeID, EcsMaskBit maskBit)
        {
            _poolComponentCounts[componentTypeID]++;
            _componentCounts[entityID]++;
            _entitiesComponentMasks[entityID][maskBit.chankIndex] |= maskBit.mask;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UnregisterEntityComponent(int entityID, int componentTypeID, EcsMaskBit maskBit)
        {
            _poolComponentCounts[componentTypeID]--;
            var count = --_componentCounts[entityID];
            _entitiesComponentMasks[entityID][maskBit.chankIndex] &= ~maskBit.mask;

            if (count == 0 && _allEntites.Has(entityID))
                DelEntity(entityID);
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (count < 0) Throw.World_InvalidIncrementComponentsBalance();
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasEntityComponent(int entityID, EcsMaskBit maskBit)
        {
            return (_entitiesComponentMasks[entityID][maskBit.chankIndex] & maskBit.mask) != maskBit.mask;
        }
        #endregion

        #region PoolsMediator
        public readonly struct PoolsMediator
        {
            private readonly EcsWorld _world;
            internal PoolsMediator(EcsWorld world)
            {
                if (world == null || world._poolsMediator._world != null)
                {
                    throw new MethodAccessException();
                }
                _world = world;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RegisterComponent(int entityID, int componentTypeID, EcsMaskBit maskBit)
            {
                _world.RegisterEntityComponent(entityID, componentTypeID, maskBit);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnregisterComponent(int entityID, int componentTypeID, EcsMaskBit maskBit)
            {
                _world.UnregisterEntityComponent(entityID, componentTypeID, maskBit);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool HasComponent(int entityID, EcsMaskBit maskBit)
            {
                return _world.HasEntityComponent(entityID, maskBit);
            }
        }
        #endregion
    }
}
