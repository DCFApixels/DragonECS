#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Core.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Linq;

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

        private EcsNullPool _nullPool = EcsNullPool.instance;

        #region FindPoolInstance
        /// <summary>
        /// Find and return a pool instance by component type id.
        /// </summary>
        /// <param name="componentTypeID">Component type identifier.</param>
        /// <returns>IEcsPool instance when declared; otherwise null.</returns>
        public IEcsPool FindPoolInstance(int componentTypeID)
        {
            if (IsComponentTypeDeclared(componentTypeID))
            {
                return FindPoolInstance_Internal(componentTypeID);
            }
            return null;
        }
        /// <summary>
        /// Find and return a pool instance for the given component type.
        /// </summary>
        /// <param name="componentType">Component System.Type to find.</param>
        /// <returns>IEcsPool instance when declared; otherwise null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEcsPool FindPoolInstance(Type componentType)
        {
            return FindPoolInstance_Internal(GetComponentTypeID(componentType));
        }
        /// <summary>
        /// Try to find a pool instance by component type id.
        /// </summary>
        /// <param name="componentTypeID">Component type identifier.</param>
        /// <param name="pool">Out parameter receiving the pool if found.</param>
        /// <returns>True when a valid pool was found; otherwise false.</returns>
        public bool TryFindPoolInstance(int componentTypeID, out IEcsPool pool)
        {
            pool = FindPoolInstance(componentTypeID);
            return pool.IsNullOrDummy() == false;
        }
        /// <summary>
        /// Try to find a pool instance for the given component type.
        /// </summary>
        /// <param name="componentType">Component System.Type to find.</param>
        /// <param name="pool">Out parameter receiving the pool if found.</param>
        /// <returns>True when a valid pool was found; otherwise false.</returns>
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
        /// <summary>
        /// Get or create a pool instance of type <typeparamref name="TPool"/> for this world.
        /// </summary>
        /// <typeparam name="TPool">Pool implementation type.</typeparam>
        /// <returns>Instance of the requested pool type.</returns>
        [UnityEngine.Scripting.Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TPool GetPoolInstance<TPool>() where TPool : IEcsPoolImplementation, new()
        {
            return Get<PoolCache<TPool>>().Instance;
        }

        /// <summary>
        /// Unchecked variant of GetPoolInstance; returns the pool instance without additional runtime checks.
        /// </summary>
        /// <typeparam name="TPool">Pool implementation type.</typeparam>
        /// <returns>Instance of the requested pool type.</returns>
        [UnityEngine.Scripting.Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TPool GetPoolInstanceUnchecked<TPool>() where TPool : IEcsPoolImplementation, new()
        {
            return GetUnchecked<PoolCache<TPool>>().Instance;
        }

        /// <summary>
        /// Static access to a pool instance for a specific world id. Get or create a pool instance of type <typeparamref name="TPool"/> for this world.
        /// </summary>
        /// <typeparam name="TPool">Pool implementation type.</typeparam>
        /// <param name="worldID">World identifier.</param>
        /// <returns>Instance of the requested pool type for the specified world.</returns>
        [UnityEngine.Scripting.Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool GetPoolInstance<TPool>(short worldID) where TPool : IEcsPoolImplementation, new()
        {
            return Get<PoolCache<TPool>>(worldID).Instance;
        }

        /// <summary>
        /// Static unchecked access to a pool instance for a specific world id; returns the pool instance without additional runtime checks.
        /// </summary>
        /// <typeparam name="TPool">Pool implementation type.</typeparam>
        /// <param name="worldID">World identifier.</param>
        /// <returns>Instance of the requested pool type for the specified world.</returns>
        [UnityEngine.Scripting.Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TPool GetPoolInstanceUnchecked<TPool>(short worldID) where TPool : IEcsPoolImplementation, new()
        {
            return GetUnchecked<PoolCache<TPool>>(worldID).Instance;
        }

        /// <summary>
        /// Return a marker object used for fluent pool instance lookup via implicit conversions.
        /// </summary>
        /// <returns>GetPoolInstanceMarker bound to this world.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GetPoolInstanceMarker GetPoolInstance()
        {
            return new GetPoolInstanceMarker(this);
        }
        #endregion

        #region ComponentInfo
        /// <summary>
        /// Declare or return the internal component type id for type <typeparamref name="TComponent"/>.
        /// </summary>
        /// <typeparam name="TComponent">Component System.Type.</typeparam>
        /// <returns>Internal component type identifier.</returns>
        public int GetComponentTypeID<TComponent>()
        {
            return DeclareOrGetComponentTypeID(EcsTypeCodeManager.Get<TComponent>());
        }

        /// <summary>
        /// Declare or return the internal component type id for the specified System.Type type.
        /// </summary>
        /// <param name="componentType">Component System.Type.</param>
        /// <returns>Internal component type identifier.</returns>
        public int GetComponentTypeID(Type componentType)
        {
            return DeclareOrGetComponentTypeID(EcsTypeCodeManager.Get(componentType));
        }

        /// <summary>
        /// Return the System.Type for the given internal component type id.
        /// </summary>
        /// <param name="componentTypeID">Internal component type identifier.</param>
        /// <returns>System.Type of the component stored in the pool.</returns>
        public Type GetComponentType(int componentTypeID)
        {
            return _pools[componentTypeID].ComponentType;
        }

        /// <summary>
        /// Check whether the component type <typeparamref name="TComponent"/> has been declared for this world.
        /// </summary>
        /// <typeparam name="TComponent">Component System.Type.</typeparam>
        /// <returns>True when declared; otherwise false.</returns>
        public bool IsComponentTypeDeclared<TComponent>()
        {
            return _cmpTypeCode_2_CmpTypeIDs.Contains((int)EcsTypeCodeManager.Get<TComponent>());
        }

        /// <summary>
        /// Check whether the specified CLR type has been declared as a component type in this world.
        /// </summary>
        /// <param name="componentType">Component System.Type.</param>
        /// <returns>True when declared; otherwise false.</returns>
        public bool IsComponentTypeDeclared(Type componentType)
        {
            return _cmpTypeCode_2_CmpTypeIDs.Contains((int)EcsTypeCodeManager.Get(componentType));
        }

        /// <summary>
        /// Check whether a component type id is declared in the current world.
        /// </summary>
        /// <param name="componentTypeID">Internal component type identifier.</param>
        /// <returns>True when declared and a pool exists; otherwise false.</returns>
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
        /// <summary>
        /// Manually initialize and register a custom pool implementation instance for this world, e.g., to set pool capacities.
        /// Must be called before entities are created when using custom pools.
        /// </summary>
        /// <param name="poolImplementation">Pool implementation instance to register.</param>
        [UnityEngine.Scripting.Preserve]
        public void InitPoolInstance(IEcsPoolImplementation poolImplementation)
        {
#if DEBUG
            if (Count > 0) { Throw.World_MethodCalledAfterEntityCreation(nameof(InitEntitySlot)); }
#elif DRAGONECS_STABILITY_MODE
            if (Count > 0) { return; }
#endif
            InitPoolInstance_Internal(poolImplementation);
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
                InitPoolInstance_Internal(newPool);
                return newPool;
            }
        }
        private void InitPoolInstance_Internal(IEcsPoolImplementation newPool)
        {
            lock (_worldLock)
            {
#if DEBUG
                AllowedInWorldsAttribute.CheckAllows(this, newPool.ComponentType);
#endif
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
                newPool.OnInit(ComponentsRegistrar.Create_Internal(this, componentTypeID));

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
            if (_hasAnyEntityListener)
            {
                _entityListeners.InvokeOnMigrateEntity(entityID);
            }
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
                MoveToEmptyEntities(entityID, true);
            }
            CheckUnregisterValid(count, entityID);
            if (_hasAnyEntityListener)
            {
                _entityListeners.InvokeOnMigrateEntity(entityID);
            }
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
                if (count == 0 && IsUsed(entityID))
                {
                    RemoveFromEmptyEntities(entityID);
                }
                if (_hasAnyEntityListener)
                {
                    _entityListeners.InvokeOnMigrateEntity(entityID);
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
                    MoveToEmptyEntities(entityID, true);
                }
                CheckUnregisterValid(count, entityID);
                if (_hasAnyEntityListener)
                {
                    _entityListeners.InvokeOnMigrateEntity(entityID);
                }
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

        #region ComponentsRegistrar
        /// <summary>
        /// Pool‑implementation utility that provides controlled access to the world's internal component registration API.
        /// Used to register/unregister components and inspect pool metadata (count, version, presence).
        /// </summary>
        public readonly struct ComponentsRegistrar
        {
            public readonly EcsWorld World;
            public readonly EcsMaskChunck MaskChunck;
            public readonly int ComponentTypeID;
            public readonly short WorldID;
            private ComponentsRegistrar(EcsWorld world, int componentTypeID)
            {
                World = world;
                WorldID = world.ID;
                ComponentTypeID = componentTypeID;
                MaskChunck = EcsMaskChunck.FromID(componentTypeID);
            }
            public static ComponentsRegistrar Create_Internal(EcsWorld world, int componentTypeID)
            {
                return new ComponentsRegistrar(world, componentTypeID);
            }
            /// <summary>
            /// Register the component for the specified entity id.
            /// </summary>
            /// <param name="entityID">Entity identifier.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RegisterComponent(int entityID)
            {
                World.RegisterEntityComponent(entityID, ComponentTypeID, MaskChunck);
            }
            /// <summary>
            /// Unregister the component for the specified entity id.
            /// </summary>
            /// <param name="entityID">Entity identifier.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnregisterComponent(int entityID)
            {
                World.UnregisterEntityComponent(entityID, ComponentTypeID, MaskChunck);
            }
            /// <summary>
            /// Try to register the component for the specified entity id.
            /// </summary>
            /// <param name="entityID">Entity identifier.</param>
            /// <returns>True if registration occurred; false if component was already present.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryRegisterComponent(int entityID)
            {
                return World.TryRegisterEntityComponent(entityID, ComponentTypeID, MaskChunck);
            }
            /// <summary>
            /// Try to unregister the component for the specified entity id.
            /// </summary>
            /// <param name="entityID">Entity identifier.</param>
            /// <returns>True if unregistration occurred; false if component was not present.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryUnregisterComponent(int entityID)
            {
                return World.TryUnregisterEntityComponent(entityID, ComponentTypeID, MaskChunck);
            }

            /// <summary>
            /// Gets the number of entities in the world that currently have this component registered.
            /// </summary>
            /// <returns>Count of entities with this component.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetComponentCount()
            {
                return World.GetPoolComponentCount(ComponentTypeID);
            }
            /// <summary>
            /// Gets the current version of this component's registration state in the world.
            /// </summary>
            /// <returns>Version number that increments on each registration or unregistration.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public long GetVersion()
            {
                return World.GetPoolVersion(ComponentTypeID);
            }
            /// <summary>
            /// Checks whether the specified entity has this component registered in the world..
            /// </summary>
            /// <param name="entityID">Entity identifier.</param>
            /// <returns>True if the component is registered for the entity; otherwise, false.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool HasComponent(int entityID)
            {
                return World.HasEntityComponent(entityID, MaskChunck);
            }
        }
        #endregion

        #region LockPool/UnLockPool
        /// <summary>
        /// Locks the pool for structural changes (component add/remove) in debug mode.
        /// While locked, any attempt to add or remove a component will throw an exception,
        /// helping to detect unsafe concurrent modifications. Also flushes pending deferred deletions
        /// to prevent interference during the locked section.
        /// </summary>
        /// <remarks>
        /// The lock is implemented as a reference counter, allowing nested locks.
        /// Each call to LockPool_Debug increments the counter; UnlockPool_Debug decrements it.
        /// The pool is considered locked when the counter is greater than zero.
        /// </remarks>
        public void LockPool_Debug<T>()
        {
            LockPool_Debug(GetComponentTypeID<T>());
        }

        /// <summary>
        /// Locks the pool for structural changes (component add/remove) in debug mode.
        /// While locked, any attempt to add or remove a component will throw an exception,
        /// helping to detect unsafe concurrent modifications. Also flushes pending deferred deletions
        /// to prevent interference during the locked section.
        /// </summary>
        /// <remarks>
        /// The lock is implemented as a reference counter, allowing nested locks.
        /// Each call to LockPool_Debug increments the counter; UnlockPool_Debug decrements it.
        /// The pool is considered locked when the counter is greater than zero.
        /// </remarks>
        /// <param name="componentTypeID">Internal component type identifier.</param>
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

        /// <summary>
        /// Releases the pool lock acquired by <see cref="LockPool_Debug"/>.
        /// Must be called in a balanced pair. After release, structural changes become allowed again
        /// </summary>
        /// <remarks>
        /// This method decrements the reference counter. If the counter drops to zero,
        /// the lock is fully released. An imbalance (calling Unlock more times than Lock)
        /// will trigger a debug assertion.
        /// </remarks>
        public void UnlockPool_Debug<T>()
        {
            UnlockPool_Debug(GetComponentTypeID<T>());
        }

        /// <summary>
        /// Releases the pool lock acquired by <see cref="LockPool_Debug"/>.
        /// Must be called in a balanced pair. After release, structural changes become allowed again
        /// </summary>
        /// <remarks>
        /// This method decrements the reference counter. If the counter drops to zero,
        /// the lock is fully released. An imbalance (calling Unlock more times than Lock)
        /// will trigger a debug assertion.
        /// </remarks>
        /// <param name="componentTypeID">Internal component type identifier.</param>
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
        /// <summary>
        /// Returns whether the pool is currently locked for structural changes.
        /// True means that add/remove component operations would throw exceptions.
        /// </summary>
        /// <param name="componentTypeID">Internal component type identifier.</param>
        /// <remarks>
        /// Internally checks if the locks counter for this pool is non‑zero.
        /// </remarks>
        /// <returns>True if locked; otherwise false.</returns>
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
        /// <summary>
        /// Marker type returned by EcsWorld.GetPoolInstance() to support fluent pool instance lookup.
        /// Allows calling GetInstance/GetInstanceUnchecked to resolve a pool for this world.
        /// </summary>
        public readonly ref struct GetPoolInstanceMarker
        {
            public readonly EcsWorld World;
            public GetPoolInstanceMarker(EcsWorld world)
            {
                World = world;
            }
            /// <summary>
            /// Resolve or create a pool instance of type TPool for the marker's world.
            /// </summary>
            /// <typeparam name="TPool">Pool implementation type.</typeparam>
            /// <returns>Resolved pool instance.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TPool GetInstance<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                return World.GetPoolInstance<TPool>();
            }
            /// <summary>
            /// Resolve a pool instance without additional runtime checks.
            /// </summary>
            /// <typeparam name="TPool">Pool implementation type.</typeparam>
            /// <returns>Resolved pool instance (unchecked).</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TPool GetInstanceUnchecked<TPool>() where TPool : IEcsPoolImplementation, new()
            {
                return World.GetPoolInstanceUnchecked<TPool>();
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Delegate invoked when a new pool is initialized in the world.
        /// </summary>
        /// <param name="pool">Initialized pool instance.</param>
        public delegate void OnPoolInitializedHandler(IEcsPool pool);
        /// <summary>
        /// Event raised when a new pool instance is created and initialized for this world.
        /// </summary>
        public event OnPoolInitializedHandler OnPoolInitialized;
        #endregion
    }
}