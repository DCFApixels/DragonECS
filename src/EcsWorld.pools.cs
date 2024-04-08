using DCFApixels.DragonECS.Internal;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public partial class EcsWorld
    {
        private SparseArray<int> _poolTypeCode_2_CmpTypeIDs = new SparseArray<int>();
        private SparseArray<int> _cmpTypeCode_2_CmpTypeIDs = new SparseArray<int>();
        private int _poolsCount;
        internal IEcsPoolImplementation[] _pools;
        internal int[] _poolComponentCounts;

        private readonly PoolsMediator _poolsMediator;

        private EcsNullPool _nullPool = EcsNullPool.instance;

        #region Getters
        public IEcsPool GetPoolInstance(int componentTypeID)
        {
#if DEBUG
            if (_pools[componentTypeID].ComponentTypeID != componentTypeID) { Throw.UndefinedException(); }
#endif
            return _pools[componentTypeID];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEcsPool GetPoolInstance(Type componentType)
        {
            int componentTypeID = GetComponentTypeID(componentType);
            ref var pool = ref _pools[componentTypeID];
            if (pool == _nullPool)
            {
                return null;
            }
            return pool;
        }

#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TPool GetPoolInstance<TPool>() where TPool : IEcsPoolImplementation, new()
        {
            return Get<PoolCache<TPool>>().instance;
        }
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TPool GetPoolInstanceUnchecked<TPool>() where TPool : IEcsPoolImplementation, new()
        {
            return GetUnchecked<PoolCache<TPool>>().instance;
        }
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool GetPoolInstance<TPool>(int worldID) where TPool : IEcsPoolImplementation, new()
        {
            return Get<PoolCache<TPool>>(worldID).instance;
        }
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool GetPoolInstanceUnchecked<TPool>(int worldID) where TPool : IEcsPoolImplementation, new()
        {
            return GetUnchecked<PoolCache<TPool>>(worldID).instance;
        }
        #endregion

        #region ComponentInfo
        public int GetComponentTypeID<TComponent>()
        {
            return DeclareOrGetComponentTypeID(EcsTypeCode.Get<TComponent>());
        }
        public int GetComponentTypeID(Type componentType)
        {
            return DeclareOrGetComponentTypeID(EcsTypeCode.Get(componentType));
        }
        public bool IsComponentTypeDeclared<TComponent>()
        {
            return _cmpTypeCode_2_CmpTypeIDs.Contains(EcsTypeCode.Get<TComponent>());
        }
        public bool IsComponentTypeDeclared(Type componentType)
        {
            return _cmpTypeCode_2_CmpTypeIDs.Contains(EcsTypeCode.Get(componentType));
        }
        public bool IsComponentTypeDeclared(int componentTypeID)
        {
            if (componentTypeID >= 0 && componentTypeID < _pools.Length)
            {
                return _pools[componentTypeID] != _nullPool;
            }
            return false;
        }
        public Type GetComponentType(int componentTypeID)
        {
            return _pools[componentTypeID].ComponentType;
        }
        #endregion

        #region Declare
        private int DeclareOrGetComponentTypeID(int componentTypeCode)
        {
            if (_cmpTypeCode_2_CmpTypeIDs.TryGetValue(componentTypeCode, out int ComponentTypeID) == false)
            {
                ComponentTypeID = _poolsCount++;
                _cmpTypeCode_2_CmpTypeIDs.Add(componentTypeCode, ComponentTypeID);
            }
            return ComponentTypeID;
        }
        private bool TryDeclareComponentTypeID(int componentTypeCode, out int componentTypeID)
        {
            if (_cmpTypeCode_2_CmpTypeIDs.TryGetValue(componentTypeCode, out componentTypeID) == false)
            {
                componentTypeID = _poolsCount++;
                _cmpTypeCode_2_CmpTypeIDs.Add(componentTypeCode, componentTypeID);
                return true;
            }
            return false;
        }
        #endregion

        #region Create
        private TPool CreatePool<TPool>() where TPool : IEcsPoolImplementation, new()
        {
            int poolTypeCode = EcsTypeCode.Get<TPool>();
            if (_poolTypeCode_2_CmpTypeIDs.Contains(poolTypeCode))
            {
                Throw.World_PoolAlreadyCreated();
            }
            TPool newPool = new TPool();

            Type componentType = newPool.ComponentType;
#if DEBUG //проверка соответсвия типов
#pragma warning disable IL2090 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The generic parameter of the source method or type does not have matching annotations.
            if (componentType != typeof(TPool).GetInterfaces()
                .First(o => o.IsGenericType && o.GetGenericTypeDefinition() == typeof(IEcsPoolImplementation<>))
                .GetGenericArguments()[0])
            {
                Throw.UndefinedException();
            }
#pragma warning restore IL2090 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The generic parameter of the source method or type does not have matching annotations.
#endif
            int componentTypeCode = EcsTypeCode.Get(componentType);

            if (_cmpTypeCode_2_CmpTypeIDs.TryGetValue(componentTypeCode, out int componentTypeID))
            {
                _poolTypeCode_2_CmpTypeIDs[poolTypeCode] = componentTypeID;
            }
            else
            {
                componentTypeID = _poolsCount++;
                _poolTypeCode_2_CmpTypeIDs[poolTypeCode] = componentTypeID;
                _cmpTypeCode_2_CmpTypeIDs[componentTypeCode] = componentTypeID;
            }

            if (_poolsCount >= _pools.Length)
            {
                int oldCapacity = _pools.Length;
                Array.Resize(ref _pools, _pools.Length << 1);
                Array.Resize(ref _poolComponentCounts, _pools.Length);
                ArrayUtility.Fill(_pools, _nullPool, oldCapacity, oldCapacity - _pools.Length);

                int newEntityComponentMaskLength = CalcEntityComponentMaskLastIndex(); //_pools.Length / COMPONENT_MASK_CHUNK_SIZE + 1;
                int dif = newEntityComponentMaskLength - _entityComponentMaskLength;
                if (dif > 0)
                {
                    int[] newEntityComponentMasks = new int[_entitiesCapacity * newEntityComponentMaskLength];
                    int indxMax = _entityComponentMaskLength * _entitiesCapacity;
                    int indx = 0;
                    int newIndx = 0;
                    int nextIndx = _entityComponentMaskLength;
                    while (indx < indxMax)
                    {
                        while (indx < nextIndx)
                        {
                            newEntityComponentMasks[newIndx] = _entityComponentMasks[indx];
                            indx++;
                            newIndx++;
                        }
                        newIndx += dif;
                        nextIndx += _entityComponentMaskLength;
                    }
                    _entityComponentMaskLength = newEntityComponentMaskLength;
                    _entityComponentMasks = newEntityComponentMasks;
                }

            }

            var oldPool = _pools[componentTypeID];

            if (oldPool != _nullPool)
            {
                Throw.UndefinedException();
            }

            _pools[componentTypeID] = newPool;
            newPool.OnInit(this, _poolsMediator, componentTypeID);
            return newPool;
        }
        #endregion

        #region Pools mediation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterEntityComponent(int entityID, int componentTypeID, EcsMaskChunck maskBit)
        {
            UpVersion();
            _poolComponentCounts[componentTypeID]++;
            _entities[entityID].componentsCount++;
            _entityComponentMasks[entityID * _entityComponentMaskLength + maskBit.chankIndex] |= maskBit.mask;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UnregisterEntityComponent(int entityID, int componentTypeID, EcsMaskChunck maskBit)
        {
            UpVersion();
            _poolComponentCounts[componentTypeID]--;
            var count = --_entities[entityID].componentsCount;
            _entityComponentMasks[entityID * _entityComponentMaskLength + maskBit.chankIndex] &= ~maskBit.mask;

            if (count == 0 && IsUsed(entityID))
            {
                DelEntity(entityID);
            }
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (count < 0) Throw.World_InvalidIncrementComponentsBalance();
#endif
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryRegisterEntityComponent(int entityID, int componentTypeID, EcsMaskChunck maskBit)
        {
            ref int chunk = ref _entityComponentMasks[entityID * _entityComponentMaskLength + maskBit.chankIndex];
            int newChunk = chunk | maskBit.mask;
            if (chunk != newChunk)
            {
                chunk = newChunk;
                _poolComponentCounts[componentTypeID]++;
                _entities[entityID].componentsCount++;
                return true;
            }
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryUnregisterEntityComponent(int entityID, int componentTypeID, EcsMaskChunck maskBit)
        {
            ref int chunk = ref _entityComponentMasks[entityID * _entityComponentMaskLength + maskBit.chankIndex];
            int newChunk = chunk & ~maskBit.mask;
            if (chunk != newChunk)
            {
                _poolComponentCounts[componentTypeID]--;
                var count = --_entities[entityID].componentsCount;
                chunk = newChunk;

                if (count == 0 && IsUsed(entityID))
                {
                    DelEntity(entityID);
                }
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (count < 0) Throw.World_InvalidIncrementComponentsBalance();
#endif
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetPoolComponentCount(int componentTypeID)
        {
            return _poolComponentCounts[componentTypeID];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasEntityComponent(int entityID, EcsMaskChunck maskBit)
        {
            return (_entityComponentMasks[entityID * _entityComponentMaskLength + maskBit.chankIndex] & maskBit.mask) == maskBit.mask;
        }
        #endregion

        #region PoolsMediator
        public readonly struct PoolsMediator
        {
            public readonly EcsWorld World;
            internal PoolsMediator(EcsWorld world)
            {
                if (world == null || world._poolsMediator.World != null)
                {
                    throw new InvalidOperationException();
                }
                World = world;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RegisterComponent(int entityID, int componentTypeID, EcsMaskChunck maskBit)
            {
                World.RegisterEntityComponent(entityID, componentTypeID, maskBit);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnregisterComponent(int entityID, int componentTypeID, EcsMaskChunck maskBit)
            {
                World.UnregisterEntityComponent(entityID, componentTypeID, maskBit);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryRegisterComponent(int entityID, int componentTypeID, EcsMaskChunck maskBit)
            {
                return World.TryRegisterEntityComponent(entityID, componentTypeID, maskBit);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryUnregisterComponent(int entityID, int componentTypeID, EcsMaskChunck maskBit)
            {
                return World.TryUnregisterEntityComponent(entityID, componentTypeID, maskBit);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetComponentCount(int componentTypeID)
            {
                return World.GetPoolComponentCount(componentTypeID);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool HasComponent(int entityID, EcsMaskChunck maskBit)
            {
                return World.HasEntityComponent(entityID, maskBit);
            }
        }
        #endregion
    }
}
