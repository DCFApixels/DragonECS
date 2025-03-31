#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.PoolsCore;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public partial class EcsWorld
    {
        private SparseArray<int> _poolTypeCode_2_CmpTypeIDs = new SparseArray<int>();
        private SparseArray<int> _cmpTypeCode_2_CmpTypeIDs = new SparseArray<int>();

        internal IEcsPoolImplementation[] _pools;
        internal PoolSlot[] _poolSlots;
        private int _poolsCount;

#if DEBUG || DRAGONECS_STABILITY_MODE
        private int _lockedPoolCount = 0;
#endif

        private readonly PoolsMediator _poolsMediator;

        private EcsNullPool _nullPool = EcsNullPool.instance;

        #region FindPoolInstance
        public IEcsPool FindPoolInstance(int componentTypeID)
        {
            if (IsComponentTypeDeclared(componentTypeID))
            {
                return FindPoolInstance_Internal(componentTypeID);
            }
            return null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEcsPool FindPoolInstance(Type componentType)
        {
            return FindPoolInstance_Internal(GetComponentTypeID(componentType));
        }
        public bool TryFindPoolInstance(int componentTypeID, out IEcsPool pool)
        {
            pool = FindPoolInstance(componentTypeID);
            return pool.IsNullOrDummy() == false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFindPoolInstance(Type componentType, out IEcsPool pool)
        {
            pool = FindPoolInstance(componentType);
            return pool.IsNullOrDummy() == false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEcsPool FindPoolInstance_Internal(int componentTypeID)
        {
            ref var result = ref _pools[componentTypeID];
            if (result != _nullPool)
            {
#if DEBUG
                if (result.ComponentTypeID != componentTypeID) { Throw.UndefinedException(); }
#endif
                return result;
            }
            return null;
        }
        #endregion

        #region GetPoolInstance
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TPool GetPoolInstance<TPool>() where TPool : IEcsPoolImplementation, new()
        {
            return Get<PoolCache<TPool>>().Instance;
        }
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TPool GetPoolInstanceUnchecked<TPool>() where TPool : IEcsPoolImplementation, new()
        {
            return GetUnchecked<PoolCache<TPool>>().Instance;
        }
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool GetPoolInstance<TPool>(short worldID) where TPool : IEcsPoolImplementation, new()
        {
            return Get<PoolCache<TPool>>(worldID).Instance;
        }
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool GetPoolInstanceUnchecked<TPool>(short worldID) where TPool : IEcsPoolImplementation, new()
        {
            return GetUnchecked<PoolCache<TPool>>(worldID).Instance;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GetPoolInstanceMarker GetPoolInstance()
        {
            return new GetPoolInstanceMarker(this);
        }
        #endregion

        #region ComponentInfo
        public int GetComponentTypeID<TComponent>()
        {
            return DeclareOrGetComponentTypeID(EcsTypeCodeManager.Get<TComponent>());
        }
        public int GetComponentTypeID(Type componentType)
        {
            return DeclareOrGetComponentTypeID(EcsTypeCodeManager.Get(componentType));
        }
        public Type GetComponentType(int componentTypeID)
        {
            return _pools[componentTypeID].ComponentType;
        }
        public bool IsComponentTypeDeclared<TComponent>()
        {
            return _cmpTypeCode_2_CmpTypeIDs.Contains((int)EcsTypeCodeManager.Get<TComponent>());
        }
        public bool IsComponentTypeDeclared(Type componentType)
        {
            return _cmpTypeCode_2_CmpTypeIDs.Contains((int)EcsTypeCodeManager.Get(componentType));
        }
        //TODO пересмотреть нейминг или функцию
        public bool IsComponentTypeDeclared(int componentTypeID)
        {
            if (componentTypeID >= 0 && componentTypeID < _pools.Length)
            {
                return _pools[componentTypeID] != _nullPool;
            }
            return false;
        }
        #endregion

        #region Declare
        internal int DeclareOrGetComponentTypeID(EcsTypeCode componentTypeCode)
        {
            if (_cmpTypeCode_2_CmpTypeIDs.TryGetValue((int)componentTypeCode, out int ComponentTypeID) == false)
            {
                ComponentTypeID = _poolsCount++;
                _cmpTypeCode_2_CmpTypeIDs.Add((int)componentTypeCode, ComponentTypeID);
            }
            return ComponentTypeID;
        }
        internal bool TryDeclareComponentTypeID(EcsTypeCode componentTypeCode, out int componentTypeID)
        {
            if (_cmpTypeCode_2_CmpTypeIDs.TryGetValue((int)componentTypeCode, out componentTypeID) == false)
            {
                componentTypeID = _poolsCount++;
                _cmpTypeCode_2_CmpTypeIDs.Add((int)componentTypeCode, componentTypeID);
                return true;
            }
            return false;
        }
        #endregion

        #region FindOrAutoCreatePool/InitPool
        public void InitPool(IEcsPoolImplementation poolImplementation)
        {
#if DEBUG
            if (Count > 0) { Throw.World_MethodCalledAfterEntityCreation(nameof(InitEntitySlot)); }
#elif DRAGONECS_STABILITY_MODE
            if (Count > 0) { return; }
#endif
            InitPool_Internal(poolImplementation);
        }
        private TPool FindOrAutoCreatePool<TPool>() where TPool : IEcsPoolImplementation, new()
        {
            lock (_worldLock)
            {
                int poolTypeCode = (int)EcsTypeCodeManager.Get<TPool>();
                if (_poolTypeCode_2_CmpTypeIDs.TryGetValue(poolTypeCode, out int cmpTypeID))
                {
                    var pool = _pools[cmpTypeID];
#if DEBUG || DRAGONECS_STABILITY_MODE
                    if ((pool is TPool) == false) { Throw.UndefinedException(); }
#endif
                    return (TPool)pool;
                }
                TPool newPool = new TPool();
                InitPool_Internal(newPool);
                return newPool;
            }
        }
        private void InitPool_Internal(IEcsPoolImplementation newPool)
        {
            lock (_worldLock)
            {
                int poolTypeCode = (int)EcsTypeCodeManager.Get(newPool.GetType());
                if (_poolTypeCode_2_CmpTypeIDs.Contains(poolTypeCode))
                {
                    Throw.World_PoolAlreadyCreated();
                }

                Type componentType = newPool.ComponentType;
#if DEBUG //проверка соответсвия типов
#pragma warning disable IL2090 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The generic parameter of the source method or type does not have matching annotations.
                if (componentType != newPool.GetType().GetInterfaces()
                    .First(o => o.IsGenericType && o.GetGenericTypeDefinition() == typeof(IEcsPoolImplementation<>))
                    .GetGenericArguments()[0])
                {
                    Throw.Exception("A custom pool must implement the interface IEcsPoolImplementation<T> where T is the type that stores the pool.");
                }
#pragma warning restore IL2090 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The generic parameter of the source method or type does not have matching annotations.
#endif
                int componentTypeCode = (int)EcsTypeCodeManager.Get(componentType);

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
                    Array.Resize(ref _poolSlots, _pools.Length);
                    ArrayUtility.Fill(_pools, _nullPool, oldCapacity, oldCapacity - _pools.Length);

                    int newEntityComponentMaskLength = CalcEntityComponentMaskLength(); //_pools.Length / COMPONENT_MASK_CHUNK_SIZE + 1;
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
                        SetEntityComponentMaskLength(newEntityComponentMaskLength);
                        _entityComponentMasks = newEntityComponentMasks;
                    }

                }

                var oldPool = _pools[componentTypeID];

                if (oldPool != _nullPool)
                {
                    Throw.Exception("Attempt to initialize a pool with the indetifier of an already existing pool.");
                }

                _pools[componentTypeID] = newPool;
                newPool.OnInit(this, _poolsMediator, componentTypeID);

                OnPoolInitialized?.Invoke(newPool);
            }
        }
        #endregion

        #region Pools mediation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RegisterEntityComponent(int entityID, int componentTypeID, EcsMaskChunck maskBit)
        {
            UpVersion();
            ref PoolSlot slot = ref _poolSlots[componentTypeID];
            slot.count++;
            slot.version++;
            var count = _entities[entityID].componentsCount++;
            if (count == 0 && IsUsed(entityID))
            {
                RemoveFromEmptyEntities(entityID);
            }
            _entityComponentMasks[(entityID << _entityComponentMaskLengthBitShift) + maskBit.chunkIndex] |= maskBit.mask;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UnregisterEntityComponent(int entityID, int componentTypeID, EcsMaskChunck maskBit)
        {
            UpVersion();
            ref PoolSlot slot = ref _poolSlots[componentTypeID];
            slot.count--;
            slot.version++;
            var count = --_entities[entityID].componentsCount;
            _entityComponentMasks[(entityID << _entityComponentMaskLengthBitShift) + maskBit.chunkIndex] &= ~maskBit.mask;

            if (count == 0 && IsUsed(entityID))
            {
                MoveToEmptyEntities(entityID);
            }
            CheckUnregisterValid(count, entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryRegisterEntityComponent(int entityID, int componentTypeID, EcsMaskChunck maskBit)
        {
            ref int entityLineStartIndex = ref _entityComponentMasks[(entityID << _entityComponentMaskLengthBitShift) + maskBit.chunkIndex];
            int newChunk = entityLineStartIndex | maskBit.mask;
            if (entityLineStartIndex != newChunk)
            {
                UpVersion();
                entityLineStartIndex = newChunk;
                ref PoolSlot slot = ref _poolSlots[componentTypeID];
                slot.count++;
                slot.version++;
                var count = _entities[entityID].componentsCount++;
                if(count == 0 && IsUsed(entityID))
                {
                    RemoveFromEmptyEntities(entityID);
                }
                return true;
            }
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryUnregisterEntityComponent(int entityID, int componentTypeID, EcsMaskChunck maskBit)
        {
            ref int entityLineStartIndex = ref _entityComponentMasks[(entityID << _entityComponentMaskLengthBitShift) + maskBit.chunkIndex];
            int newChunk = entityLineStartIndex & ~maskBit.mask;
            if (entityLineStartIndex != newChunk)
            {
                UpVersion();
                ref PoolSlot slot = ref _poolSlots[componentTypeID];
                slot.count--;
                slot.version++;
                var count = --_entities[entityID].componentsCount;
                entityLineStartIndex = newChunk;

                if (count == 0 && IsUsed(entityID))
                {
                    MoveToEmptyEntities(entityID);
                }
                CheckUnregisterValid(count, entityID);
                return true;
            }
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckUnregisterValid(int count, int entityID)
        {
#if DEBUG
            if (count < 0) { Throw.World_InvalidIncrementComponentsBalance(); }
#elif DRAGONECS_STABILITY_MODE
            if (count < 0)
            {
                for (int i = entityID << _entityComponentMaskLengthBitShift, iMax = i + _entityComponentMaskLength; i < iMax; i++)
                { 
                    _entityComponentMasks[i] = 0; 
                }
                //TODO добавить очистку пулов
                _entities[entityID].componentsCount = 0;
            }
#endif
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetPoolComponentCount(int componentTypeID)
        {
            return _poolSlots[componentTypeID].count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetPoolVersion(int componentTypeID)
        {
            return _poolSlots[componentTypeID].version;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasEntityComponent(int entityID, EcsMaskChunck maskBit)
        {
            return (_entityComponentMasks[(entityID << _entityComponentMaskLengthBitShift) + maskBit.chunkIndex] & maskBit.mask) == maskBit.mask;
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
            public long GetVersion(int componentTypeID)
            {
                return World.GetPoolVersion(componentTypeID);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool HasComponent(int entityID, EcsMaskChunck maskBit)
            {
                return World.HasEntityComponent(entityID, maskBit);
            }
        }
        #endregion

        #region LockPool/UnLockPool
        public void LockPool_Debug(int componentTypeID)
        {
#if DEBUG || DRAGONECS_STABILITY_MODE
            ref var slot = ref _poolSlots[componentTypeID];
            if (slot.lockedCounter == 0)
            {
                //очистка буффера, чтобы она рандомно не сработала в блоке блоикровки пула
                ReleaseDelEntityBufferAll();
                _lockedPoolCount++;
            }
            slot.lockedCounter++;
            _pools[componentTypeID].OnLockedChanged_Debug(true);
#endif
        }
        public void UnlockPool_Debug(int componentTypeID)
        {
#if DEBUG || DRAGONECS_STABILITY_MODE
            ref var slot = ref _poolSlots[componentTypeID];
            slot.lockedCounter--;
            if (slot.lockedCounter <= 0)
            {
                _lockedPoolCount--;
                if (_lockedPoolCount < 0 || slot.lockedCounter < 0)
                {
                    _lockedPoolCount = 0;
                    slot.lockedCounter = 0;
                    Throw.OpeningClosingMethodsBalanceError();
                }
            }
            _pools[componentTypeID].OnLockedChanged_Debug(false);
#endif
        }
        public bool CheckPoolLocked_Debug(int componentTypeID)
        {
#if DEBUG || DRAGONECS_STABILITY_MODE
            return _poolSlots[componentTypeID].lockedCounter != 0;
#else
            return false;
#endif
        }
        #endregion

        #region Utils
        internal struct PoolSlot
        {
            public long version;
            public int count;
#if DEBUG || DRAGONECS_STABILITY_MODE
            public int lockedCounter;
#endif
        }
        public readonly ref struct GetPoolInstanceMarker
        {
            public readonly EcsWorld World;
            public GetPoolInstanceMarker(EcsWorld world)
            {
                World = world;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TPool GetInstance<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                return World.GetPoolInstance<TPool>();
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TPool GetInstanceUnchecked<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                return World.GetPoolInstanceUnchecked<TPool>();
            }
        }
        #endregion

        #region Events
        public delegate void OnPoolInitializedHandler(IEcsPool pool);
        public event OnPoolInitializedHandler OnPoolInitialized;
        #endregion
    }
}
