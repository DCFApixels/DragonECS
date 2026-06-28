#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Core.Internal;
using DCFApixels.DragonECS.Core.Unchecked;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS
{
    /// <summary>
    /// Configuration for an <c>EcsWorld</c> instance. Defines initial capacities for entities, groups, pools, pool components and recycled components.
    /// </summary>
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [Serializable]
    [DataContract]
    public class EcsWorldConfig
    {
        public static readonly EcsWorldConfig Default = new EcsWorldConfig();
        [DataMember] public int EntitiesCapacity;
        [DataMember] public int GroupCapacity;
        [DataMember] public int PoolsCapacity;
        [DataMember] public int PoolComponentsCapacity;
        [DataMember] public int PoolRecycledComponentsCapacity;

        /// <summary>
        /// Create a new configuration.
        /// </summary>
        /// <param name="entitiesCapacity">Initial capacity for entities.</param>
        /// <param name="groupCapacity">Initial capacity for groups.</param>
        /// <param name="poolsCapacity">Initial capacity for pools.</param>
        /// <param name="poolComponentsCapacity">Initial capacity for components inside each pool.</param>
        /// <param name="poolRecycledComponentsCapacity">Initial capacity for recycled component slots.</param>
        public EcsWorldConfig(int entitiesCapacity = 512, int groupCapacity = 512, int poolsCapacity = 512, int poolComponentsCapacity = 512, int poolRecycledComponentsCapacity = -1)
        {
            if (poolRecycledComponentsCapacity < 0)
            {
                poolRecycledComponentsCapacity = poolComponentsCapacity / 4;
            }

            EntitiesCapacity = entitiesCapacity;
            GroupCapacity = groupCapacity;
            PoolsCapacity = poolsCapacity;
            PoolComponentsCapacity = poolComponentsCapacity;
            PoolRecycledComponentsCapacity = poolRecycledComponentsCapacity;
        }
    }

    /// <summary>
    /// Primary container for entities and components. Manages entity ids, component pools, queries, masks and other world-scoped resources. 
    /// Use it to create and destroy entities, access component pools, run queries and manage world lifecycle.
    /// </summary>
    /// <remarks>
    /// Worlds are not thread-safe for structural modifications (entity creation/deletion, component add/remove) without external synchronization;
    /// however, different worlds can be processed independently in separate threads.
    /// </remarks>
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.WORLDS_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Container for entities and components.")]
    [MetaID("DragonECS_AEF3557C92019C976FC48F90E95A9DA6")]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public partial class EcsWorld : IEntityStorage, IEcsMember, INamedMember
    {
        public readonly short ID;
        private readonly IConfigContainer _configs;
        private readonly string _name;

        private bool _isDestroyed = false;

        private IdDispenser _entityDispenser;
        private int _entitiesCount = 0;
        private int _entitiesCapacity = 0;
        private EntitySlot[] _entities = Array.Empty<EntitySlot>();

        private int[] _delEntBuffer = Array.Empty<int>();
        private int _delEntBufferCount = 0;
        private int[] _emptyEntities = Array.Empty<int>();
        private int _emptyEntitiesLength = 0;
        private int _emptyEntitiesCount = 0;
        private bool _isEnableAutoReleaseDelEntBuffer = true;

        internal int _entityComponentMaskLength;
        internal int _entityComponentMaskLengthBitShift;
        internal int[] _entityComponentMasks = Array.Empty<int>();
        private const int COMPONENT_MASK_CHUNK_SIZE = 32;

        //"лениво" обновляется только для NewEntity
        private long _deleteLeakedEntitesLastVersion = 0;
        //обновляется в NewEntity и в DelEntity
        private long _version = 0;

        private StructList<IEcsWorldEventListener> _listeners = new StructList<IEcsWorldEventListener>(2);
        private StructList<IEcsEntityEventListener> _entityListeners = new StructList<IEcsEntityEventListener>(2);
        private bool _hasAnyEntityListener = false;

        #region Properties
        EcsWorld IEntityStorage.World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return this; }
        }

        /// <summary>
        /// Configuration container. Contains <c>EcsWorldConfig</c> and other runtime settings.
        /// </summary>
        public IConfigContainer Configs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _configs; }
        }

        /// <summary>
        /// World name. May be empty. Useful for identification and debugging.
        /// </summary>
        public string Name
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _name; }
        }

        /// <summary>
        /// Monotonic version counter, incremented on structural changes (entity add/remove, mask updates).
        /// </summary>
        public long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _version; }
        }

        /// <summary>
        /// True after Destroy() was called. After destruction, most operations are invalid.
        /// </summary>
        public bool IsDestroyed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isDestroyed; }
        }

        /// <summary>
        /// Current number of alive (used) entities.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _entitiesCount; }
        }

        /// <summary>
        /// Current entity storage capacity.
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _entitiesCapacity; }
        }

        /// <summary>
        /// Number of entities in the deferred-delete buffer.
        /// </summary>
        public int DelEntBufferCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _delEntBufferCount; }
        }

        /// <summary>
        /// If true, operations that return entity spans will automatically release and process the deferred-delete buffer.
        /// </summary>
        public bool IsEnableReleaseDelEntBuffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isEnableAutoReleaseDelEntBuffer; }
        }

        /// <summary>
        /// EcsSpan of alive entities; may process deferred deletes before returning.
        /// </summary>
        public EcsSpan Entities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ToSpan(); }
        }

        /// <summary>
        /// Number of registered component pools.
        /// </summary>
        public int PoolsCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _poolsCount; }
        }

        /// <summary>
        /// Read-only span over all registered pool instances for this world. Use to inspect or operate on pools.
        /// </summary>
        public ReadOnlySpan<IEcsPool> AllPools
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _pools; } //new ReadOnlySpan<IEcsPool>(pools, 0, _poolsCount);
        }
        #endregion

        #region Constructors/Destroy
        /// <summary>
        /// Initializes a new world with default configuration and an automatically assigned world ID.
        /// </summary>
        public EcsWorld() : this(ConfigContainer.Empty, null, -1) { }

        /// <summary>
        /// Initializes a new world with the specified configuration and optional world ID. If no configuration is provided, default settings are used.
        /// </summary>
        /// <param name="config">Optional configuration object that controls initial capacities and buffer sizes. If <c>null</c>, default values are applied.</param>
        /// <param name="worldID">Optional explicit world identifier. Use <c>-1</c> (default) to let the system assign a unique ID automatically.</param>
        public EcsWorld(EcsWorldConfig config = null, short worldID = -1) : this(config == null ? ConfigContainer.Empty : new ConfigContainer().Set(config), null, worldID) { }

        /// <summary>
        /// Initializes a new world with a custom configuration container and optional world ID. This allows for advanced configuration beyond the standard <see cref="EcsWorldConfig"/>.
        /// </summary>
        /// <param name="configs">Custom configuration container that can hold multiple settings (e.g., pool sizes, debug flags). Must not be <c>null</c>.</param>
        /// <param name="worldID">Optional explicit world ID. Pass <c>-1</c> to have the system generate a unique ID automatically.</param>
        public EcsWorld(IConfigContainer configs, short worldID = -1) : this(configs, null, worldID) { }

        /// <summary>
        /// Initializes a new world with the specified configuration, a human‑readable name, and optional world ID. The name is useful for debugging and logging.
        /// </summary>
        /// <param name="config">Optional configuration object. If <c>null</c>, default capacities are used.</param>
        /// <param name="name">Optional name for the world (e.g., "GameWorld", "PhysicsWorld"). May be empty.</param>
        /// <param name="worldID">Optional explicit world ID. Default <c>-1</c> triggers automatic ID assignment.</param>
        public EcsWorld(EcsWorldConfig config = null, string name = null, short worldID = -1) : this(config == null ? ConfigContainer.Empty : new ConfigContainer().Set(config), name, worldID) { }

        /// <summary>
        /// Initializes a new world with a custom configuration container, a human‑readable name, and optional world ID. This is the most flexible constructor, allowing full control over configuration and identification.
        /// </summary>
        /// <param name="configs">Custom configuration container. Must be provided (non‑null).</param>
        /// <param name="name">Optional name for debugging and diagnostics. Can be <c>null</c> or empty.</param>
        /// <param name="worldID">Optional explicit world ID. If <c>-1</c>, the system will assign a unique ID.</param>
        public EcsWorld(IConfigContainer configs, string name = null, short worldID = -1)
        {
            lock (_worldLock)
            {
                if (configs == null) { configs = ConfigContainer.Empty; }
                if (name == null) { name = string.Empty; }
                _name = name;
                bool nullWorld = this is NullWorld;
                if (nullWorld == false && worldID == NULL_WORLD_ID)
                {
                    EcsDebug.PrintWarning($"The world identifier cannot be {NULL_WORLD_ID}");
                }
                _configs = configs;
                EcsWorldConfig config = configs.GetWorldConfigOrDefault();

                // тут сложно однозначно посчитать, так как нужно еще место под аспекты и запросы
                int controllersCount = config.PoolsCapacity * 4;
                _worldComponentPools = new StructList<WorldComponentPoolAbstract>(controllersCount);

                if (worldID < 0 || (worldID == NULL_WORLD_ID && nullWorld == false))
                {
                    int newID = _worldIdDispenser.UseFree();
#if DEBUG && DRAGONECS_DEEP_DEBUG
                    if (newID > short.MaxValue) { Throw.DeepDebugException(); }
#endif
                    worldID = (short)newID;
                }
                else
                {
                    if (worldID != _worldIdDispenser.NullID)
                    {
                        _worldIdDispenser.Use(worldID);
                    }
                    if (_worlds[worldID] != null)
                    {
                        _worldIdDispenser.Release(worldID);
                        Throw.Exception("The world with the specified ID has already been created\r\n");
                    }
                }
                ID = worldID;
                _worlds[worldID] = this;

                int poolsCapacity = ArrayUtility.CeilPow2Safe(config.PoolsCapacity);
                _pools = new IEcsPoolImplementation[poolsCapacity];
                _poolSlots = new PoolSlot[poolsCapacity];
                ArrayUtility.Fill(_pools, _nullPool);

                int entitiesCapacity = ArrayUtility.CeilPow2Safe(config.EntitiesCapacity);
                _entityDispenser = new IdDispenser(entitiesCapacity, 0, OnEntityDispenserResized);

                _executorCoures = new Dictionary<(Type, object), IQueryExecutorImplementation>(config.PoolComponentsCapacity);

                GetComponentTypeID<NullComponent>();
                OnWorldCreated?.Invoke(this);
            }
        }

        /// <summary>
        /// Destroy the world: removes all entities, components, notifies listeners and releases resources.
        /// Always call when the world is no longer needed, otherwise occupied resources will leak.
        /// After this call the world is considered destroyed and most operations are invalid.
        /// </summary>
        public void Destroy()
        {
            lock (_worldLock)
            {
                if (_isDestroyed)
                {
                    EcsDebug.PrintWarning("The world is already destroyed");
                    return;
                }
                if (ID == NULL_WORLD_ID)
                {
#if DEBUG
                    Throw.World_WorldCantBeDestroyed();
#endif
                    return;
                }

                ReleaseDelEntityBufferAll();
                using (DisableAutoReleaseDelEntBuffer())
                {
                    for (int i = Entities.Count - 1; i >= 0; i--)
                    {
                        if (IsUsed(Entities[i]))
                        {
                            DelEntity(Entities[i]);
                        }
                    }
                }
                ReleaseDelEntityBufferAll();

                _isDestroyed = true;
                _listeners.InvokeOnWorldDestroy();
                _entityDispenser = null;
                _pools = null;
                _nullPool = null;
                _worlds[ID] = null;
                ReleaseData(ID);
                _worldIdDispenser.Release(ID);
                _worldIdDispenser.Sort();
                _poolTypeCode_2_CmpTypeIDs = null;
                _cmpTypeCode_2_CmpTypeIDs = null;
                DisposeGroups();

                foreach (var item in _executorCoures)
                {
                    item.Value.Destroy();
                }
                //_entities - не обнуляется для работы entlong.IsAlive
            }
        }
        #endregion

        #region Getters
        /// <summary>
        /// Returns cached aspect instance of type <typeparamref name="TAspect"/> for use with queries and direct pool access.
        /// </summary>
        /// <typeparam name="TAspect">Aspect type (must have parameterless constructor).</typeparam>
        /// <returns>Cached aspect instance of type <typeparamref name="TAspect"/>.</returns>
        [UnityEngine.Scripting.Preserve]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TAspect GetAspect<TAspect>() where TAspect : new()
        {
            return Get<AspectCache<TAspect>>().Instance;
        }

        /// <summary>
        /// Shortcut: obtain one cached aspect instance by out parameter. See GetAspect for details.
        /// </summary>
        /// <typeparam name="TAspect0">Aspect type.</typeparam>
        /// <param name="a0">Out parameter receiving the cached aspect instance.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetAspects<TAspect0>(out TAspect0 a0)
            where TAspect0 : new()
        {
            a0 = GetAspect<TAspect0>();
        }

        /// <summary>
        /// Shortcut: obtain two cached aspect instances by out parameters. See GetAspect for details.
        /// </summary>
        /// <typeparam name="TAspect0">First aspect type.</typeparam>
        /// <typeparam name="TAspect1">Second aspect type.</typeparam>
        /// <param name="a0">Out parameter receiving the first cached aspect instance.</param>
        /// <param name="a1">Out parameter receiving the second cached aspect instance.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetAspects<TAspect0, TAspect1>(out TAspect0 a0, out TAspect1 a1)
            where TAspect0 : new()
            where TAspect1 : new()
        {
            a0 = GetAspect<TAspect0>();
            a1 = GetAspect<TAspect1>();
        }

        /// <summary>
        /// Shortcut: obtain three cached aspect instances by out parameters. See GetAspect for details.
        /// </summary>
        /// <typeparam name="TAspect0">First aspect type.</typeparam>
        /// <typeparam name="TAspect1">Second aspect type.</typeparam>
        /// <typeparam name="TAspect2">Third aspect type.</typeparam>
        /// <param name="a0">Out parameter receiving the first cached aspect instance.</param>
        /// <param name="a1">Out parameter receiving the second cached aspect instance.</param>
        /// <param name="a2">Out parameter receiving the third cached aspect instance.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetAspects<TAspect0, TAspect1, TAspect2>(out TAspect0 a0, out TAspect1 a1, out TAspect2 a2)
            where TAspect0 : new()
            where TAspect1 : new()
            where TAspect2 : new()
        {
            a0 = GetAspect<TAspect0>();
            a1 = GetAspect<TAspect1>();
            a2 = GetAspect<TAspect2>();
        }

        /// <summary>
        /// Shortcut: obtain four cached aspect instances by out parameters. See GetAspect for details.
        /// </summary>
        /// <typeparam name="TAspect0">First aspect type.</typeparam>
        /// <typeparam name="TAspect1">Second aspect type.</typeparam>
        /// <typeparam name="TAspect2">Third aspect type.</typeparam>
        /// <typeparam name="TAspect3">Fourth aspect type.</typeparam>
        /// <param name="a0">Out parameter receiving the first cached aspect instance.</param>
        /// <param name="a1">Out parameter receiving the second cached aspect instance.</param>
        /// <param name="a2">Out parameter receiving the third cached aspect instance.</param>
        /// <param name="a3">Out parameter receiving the fourth cached aspect instance.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetAspects<TAspect0, TAspect1, TAspect2, TAspect3>(out TAspect0 a0, out TAspect1 a1, out TAspect2 a2, out TAspect3 a3)
            where TAspect0 : new()
            where TAspect1 : new()
            where TAspect2 : new()
            where TAspect3 : new()
        {
            a0 = GetAspect<TAspect0>();
            a1 = GetAspect<TAspect1>();
            a2 = GetAspect<TAspect2>();
            a3 = GetAspect<TAspect3>();
        }

        /// <summary>
        /// Returns cached aspect instance and its compiled <c>EcsMask</c>. The mask describes the include/exclude/any component sets.
        /// </summary>
        /// <typeparam name="TAspect">Aspect type.</typeparam>
        /// <param name="mask">Out parameter receiving the compiled mask used by the returned aspect.</param>
        /// <returns>Cached aspect instance of type <typeparamref name="TAspect"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TAspect GetAspect<TAspect>(out EcsMask mask) where TAspect : new()
        {
            var result = Get<AspectCache<TAspect>>();
            mask = result.Mask;
            return result.Instance;
        }

        /// <summary>
        /// Returns cached Where-query executor and aspect.
        /// </summary>
        /// <typeparam name="TExecutor">Executor type derived from MaskQueryExecutor.</typeparam>
        /// <typeparam name="TAspect">Aspect type.</typeparam>
        /// <param name="executor">Out parameter receiving the cached executor instance.</param>
        /// <param name="aspect">Out parameter receiving the cached aspect instance.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetQueryCache<TExecutor, TAspect>(out TExecutor executor, out TAspect aspect)
            where TExecutor : MaskQueryExecutor, new()
            where TAspect : new()
        {
            ref var cmp = ref Get<WhereQueryCache<TExecutor, TAspect>>();
            executor = cmp.Executor;
            aspect = cmp.Aspcet;
        }

        /// <summary>
        /// Access to world-scoped component of type <typeparamref name="T"/>. Use for global per-world singletons and caches.
        /// </summary>
        /// <returns>Reference to the world-scoped component instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get<T>() where T : struct
        {
            return ref WorldComponentPool<T>.GetForWorld(ID);
        }

        /// <summary>
        /// Checks whether world-scoped component <typeparamref name="T"/> exists.
        /// </summary>
        /// <returns>True if the component is present; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has<T>() where T : struct
        {
            return WorldComponentPool<T>.Has(ID);
        }

        /// <summary>
        /// Access to a world-scoped component of type <typeparamref name="T"/> without runtime checks. 
        /// Use only when caller guarantees existence and correctness.
        /// </summary>
        /// <returns>Reference to the world-scoped component instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetUnchecked<T>() where T : struct
        {
            return ref WorldComponentPool<T>.GetForWorldUnchecked(ID);
        }

        /// <summary>
        /// Static access to a world-scoped component of type <typeparamref name="T"/>. Use for global per-world singletons and caches.
        /// </summary>
        /// <param name="worldID">Target world identifier.</param>
        /// <returns>Reference to the world-scoped component instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Get<T>(short worldID) where T : struct
        {
            return ref WorldComponentPool<T>.GetForWorld(worldID);
        }

        /// <summary>
        /// Static check whether world-scoped component <typeparamref name="T"/> exists.
        /// </summary>
        /// <param name="worldID">Target world identifier.</param>
        /// <returns>True if the component is present; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has<T>(short worldID) where T : struct
        {
            return WorldComponentPool<T>.Has(worldID);
        }

        /// <summary>
        /// Static access to a world-scoped component of type <typeparamref name="T"/> without runtime checks. 
        /// Use only when caller guarantees existence and correctness.
        /// </summary>
        /// <param name="worldID">Target world identifier.</param>
        /// <returns>Reference to the world-scoped component instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetUnchecked<T>(short worldID) where T : struct
        {
            return ref WorldComponentPool<T>.GetForWorldUnchecked(worldID);
        }
        #endregion

        #region Entity

        #region New/Del
        /// <summary>
        /// Create entity and return its packed entlong identifier (world id + generation + entity id), a stable handle
        /// that includes world and generation information for detecting stale references.
        /// </summary>
        /// <returns>Packed entlong identifier for the created entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong NewEntityLong()
        {
            int entityID = NewEntity();
            return GetEntityLong(entityID);
        }

        /// <summary>
        /// Create entity with a requested integer id and return its packed entlong identifier (world id + generation + entity id), a stable handle
        /// that includes world and generation information for detecting stale references.
        /// </summary>
        /// <param name="entityID">Requested integer entity id to allocate.</param>
        /// <returns>Packed entlong identifier for the created entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong NewEntityLong(int entityID)
        {
            NewEntity(entityID);
            return GetEntityLong(entityID);
        }

        /// <summary>
        /// Create entity and return new entity id.
        /// </summary>
        /// <returns>Created entity id.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NewEntity()
        {
            int entityID = _entityDispenser.UseFree();
            CreateConcreteEntity(entityID);
            return entityID;
        }

        /// <summary>
        /// Create a new entity with the specified integer id.
        /// </summary>
        /// <param name="entityID">Requested integer entity id to create.</param>
        /// <returns>Integer id of the created entity.</returns>
        /// <returns>Created entity id (same as requested).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NewEntity(int entityID)
        {
            _entityDispenser.Upsize(entityID + 1);
#if DEBUG
            if (IsUsed(entityID)) { Throw.World_EntityIsAlreadyСontained(entityID); }
#elif DRAGONECS_STABILITY_MODE
            if (IsUsed(entityID)) { return 0; }
#endif
            _entityDispenser.Use(entityID);
            CreateConcreteEntity(entityID);
            return entityID;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateConcreteEntity(int entityID)
        {
            UpVersionLeaked();
            _entitiesCount++;
            ref var slot = ref _entities[entityID];
            slot.isUsed = true;
            if (slot.gen >= GEN_STATUS_SEPARATOR)
            {
                slot.gen |= GEN_SLEEP_MASK;
            }
            if (_hasAnyEntityListener)
            {
                _entityListeners.InvokeOnNewEntity(entityID);
            }
            MoveToEmptyEntities(entityID, false);
        }

        /// <summary>
        /// Attempts to mark the entity for deletion and enqueue it into the DelEntityBuffer for deferred processing (automatically or manually).
        /// Returns true if the entity exists and was scheduled; otherwise, false.
        /// Also updates the version and notifies registered listeners.
        /// </summary>
        /// <param name="entity">Packed entlong identifying the entity to delete.</param>
        /// <returns>True if deletion was scheduled; false if the entlong was invalid or entity not alive.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDelEntity(entlong entity)
        {
            if (entity.TryGetID(out int entityID))
            {
                TryDelEntity(entityID);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to mark the entity for deletion and enqueue it into the DelEntityBuffer for deferred processing (automatically or manually).
        /// Returns true if the entity exists and was scheduled; otherwise, false.
        /// Also updates the version and notifies registered listeners.
        /// </summary>
        /// <param name="entityID">Integer id of the entity to delete.</param>
        /// <returns>True if deletion was scheduled; false if entity id was not used.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDelEntity(int entityID)
        {
            if (IsUsed(entityID))
            {
                DelEntity(entityID);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Marks the entity for deletion and enqueues it into the DelEntityBuffer for deferred processing (automatically or manually).
        /// Also updates the version and notifies registered listeners.
        /// </summary>
        /// <param name="entity">Packed entlong identifying the entity to delete.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DelEntity(entlong entity)
        {
            DelEntity(entity.ID);
        }

        /// <summary>
        /// Marks the entity for deletion and enqueues it into the DelEntityBuffer for deferred processing (automatically or manually).
        /// Also updates the version and notifies registered listeners.
        /// </summary>
        /// <param name="entityID">Integer id of the entity to delete.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DelEntity(int entityID)
        {
#if DEBUG
            if (IsUsed(entityID) == false) { Throw.World_EntityIsNotContained(entityID); }
#elif DRAGONECS_STABILITY_MODE
            if (IsUsed(entityID) == false) { return; }
#endif
            UpVersion();
            _delEntBuffer[_delEntBufferCount++] = entityID;
            ref var slot = ref _entities[entityID];
            slot.isUsed = false;
            slot.metaName = null;
            slot.metaColor = new MetaColor(0, 0, 0, 0);
            _entitiesCount--;
            if (_hasAnyEntityListener)
            {
                _entityListeners.InvokeOnDelEntity(entityID);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveToEmptyEntities(int entityID, bool readyToRemove)
        {
            if (readyToRemove)
            {
                entityID |= int.MinValue;
            }
            _emptyEntities[_emptyEntitiesLength++] = entityID;
            _emptyEntitiesCount++;
            if (_emptyEntitiesLength == _emptyEntities.Length)
            {
                ReleaseEmptyEntitiesBuffer_OnlyReadyToRemove();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveFromEmptyEntities(int entityID)
        {
            const int THRESHOLD = 16;
            _emptyEntitiesCount--;

            if (_emptyEntitiesLength < THRESHOLD)
            {
                for (int i = _emptyEntitiesLength - 1; i >= 0; i--)
                {
                    if ((_emptyEntities[i] & int.MaxValue) == entityID)
                    {
                        _emptyEntities[i] = _emptyEntities[--_emptyEntitiesLength];
                    }
                }
            }

#if DRAGONECS_DEEP_DEBUG
            if (_emptyEntitiesCount < 0)
            {
                Throw.DeepDebugException();
            }
#endif
            if (_emptyEntitiesCount == 0)
            {
                _emptyEntitiesCount = 0;
                _emptyEntitiesLength = 0;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseEmptyEntitiesBuffer()
        {
            for (int i = 0; i < _emptyEntitiesLength; i++)
            {
                var entityID = _emptyEntities[i] & int.MaxValue;
                if (IsUsed(entityID) && _entities[entityID].componentsCount == 0)
                {
                    DelEntity(entityID);
                }
            }
            _emptyEntitiesCount = 0;
            _emptyEntitiesLength = 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReleaseEmptyEntitiesBuffer_OnlyReadyToRemove()
        {
            var newCount = 0;
            for (int i = 0; i < _emptyEntitiesLength; i++)
            {
                var entityID = _emptyEntities[i];
                bool isReady = entityID < 0;
                entityID &= int.MaxValue;

                if (IsUsed(entityID) && _entities[entityID].componentsCount == 0)
                {
                    if (isReady)
                    {
                        DelEntity(entityID);
                    }
                    else
                    {
                        _emptyEntities[newCount++] = entityID;
                    }
                }
            }
            _emptyEntitiesCount = newCount;
            _emptyEntitiesLength = newCount;
        }
        #endregion

        #region Other
        /// <summary>
        /// Return a span-like collection (EcsSpan) of currently alive entities.
        /// Automatically processes deferred deletes if enabled.
        /// Use for efficient iteration over entity ids.
        /// </summary>
        /// <returns>EcsSpan representing the current alive entities in the world.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ToSpan()
        {
            if (_isEnableAutoReleaseDelEntBuffer)
            {
                ReleaseDelEntityBufferAll();
            }
            return GetCurrentEntities_Internal();
        }

        /// <summary>
        /// Pack the given entity id with world id and generation into entlong. Useful for creating stable handles.
        /// </summary>
        /// <param name="entityID">Integer entity id to pack.</param>
        /// <returns>Packed entlong representing the entity in this world.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe entlong GetEntityLong(int entityID)
        {
            long x = (long)ID << 48 | (long)GetGen(entityID) << 32 | (long)entityID;
            return *(entlong*)&x;
        }

        /// <summary>
        /// Initialize an entity slot's generation before any entity is created. Intended for low-level pre-initialization.
        /// </summary>
        /// <param name="entityID">Entity slot id to initialize.</param>
        /// <param name="gen">Initial generation value to store in the slot.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InitEntitySlot(int entityID, short gen)
        {
#if DEBUG
            if (Count > 0) { Throw.World_MethodCalledAfterEntityCreation(nameof(InitEntitySlot)); }
#elif DRAGONECS_STABILITY_MODE
            if (Count > 0) { return; }
#endif
            _entityDispenser.Upsize(entityID);
            _entities[entityID].gen = gen;
        }

        /// <summary>
        /// Return a RawEntLong containing the raw entity id, generation and world id without packing into entlong.
        /// </summary>
        /// <param name="entityID">Integer entity id to pack.</param>
        /// <returns>RawEntLong representing the entity in this world.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawEntLong GetRawEntLong(int entityID)
        {
            return new RawEntLong(entityID, _entities[entityID].gen, ID);
        }

        /// <summary>
        /// Check whether the specified entity id with the given generation is alive in this world.
        /// </summary>
        /// <param name="entityID">Entity id to check.</param>
        /// <param name="gen">Generation value to compare with the slot.</param>
        /// <returns>True if the slot's generation matches <paramref name="gen"/> and the slot is marked used.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(int entityID, short gen)
        {
            ref var slot = ref _entities[entityID];
            return slot.gen == gen && slot.isUsed;
        }

        /// <summary>
        /// Check whether the entlong represents an alive entity in this world.
        /// </summary>
        /// <param name="entity">Packed entlong to check.</param>
        /// <returns>True if entity belongs to this world and represents an alive entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(entlong entity)
        {
#if DEBUG
            if (entity.GetWorldIDUnchecked() != ID) { Throw.World_MaskDoesntBelongWorld(); }
#elif DRAGONECS_STABILITY_MODE
            if (entity.GetWorldIDUnchecked() != ID) { return false; }
#endif
            ref var slot = ref _entities[entity.GetIDUnchecked()];
            return slot.gen == entity.GetIDUnchecked() && slot.isUsed;
        }

        /// <summary>
        /// Returns true if the internal slot for the given entity id is marked as used (alive).
        /// </summary>
        /// <param name="entityID">Entity id to query.</param>
        /// <returns>True when the slot for <paramref name="entityID"/> is currently used.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUsed(int entityID)
        {
            return _entities[entityID].isUsed;
        }
        /// <summary>
        /// Return generation for the entity slot.
        /// </summary>
        /// <param name="entityID">Entity id to query.</param>
        /// <returns>Current generation value stored in the entity slot.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetGen(int entityID)
        {
            unchecked
            {
                ref var slotGen = ref _entities[entityID].gen;
                if (slotGen < GEN_STATUS_SEPARATOR)
                { //если gen меньше 0 значит он спящий, спящие нужно инкремировать перед выдачей
                    slotGen++;
                    slotGen &= GEN_WAKEUP_MASK;
                }
                return slotGen;
            }
        }
        /// <summary>
        /// Return number of components currently attached to the entity.
        /// </summary>
        /// <param name="entityID">Entity id to query.</param>
        /// <returns>Short integer count of components attached to the entity.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetComponentsCount(int entityID)
        {
            return _entities[entityID].componentsCount;
        }

        /// <summary>
        /// Checks whether the entity's component composition matches the given mask, 
        /// converting it to an <see cref="EcsMask"/> first.
        /// </summary>
        /// <param name="mask">Component mask to test against.</param>
        /// <param name="entityID">Entity id to check.</param>
        /// <returns>True if the entity satisfies all include, exclude, and any conditions of the mask.</returns>
        public bool IsMatchesMask(IComponentMask mask, int entityID)
        {
            return IsMatchesMask(mask.ToMask(this), entityID);
        }

        /// <summary>
        /// Checks whether the entity's component composition matches the EcsMask.
        /// </summary>
        /// <param name="mask">Component mask to test against.</param>
        /// <param name="entityID">Entity id to check.</param>
        /// <returns>True if the entity satisfies all include, exclude, and any conditions of the mask.</returns>
        public bool IsMatchesMask(EcsMask mask, int entityID)
        {
#if DEBUG
            if (mask.WorldID != ID) { Throw.World_MaskDoesntBelongWorld(); }
#elif DRAGONECS_STABILITY_MODE
            if (mask.WorldID != ID) { return false; }
#endif

#if DEBUG && DRAGONECS_DEEP_DEBUG
            bool IsMatchesMaskDeepDebug(EcsMask mask_, int entityID_)
            {
                for (int i = 0, iMax = mask_._incs.Length; i < iMax; i++)
                {
                    if (!_pools[mask_._incs[i]].Has(entityID_))
                    {
                        return false;
                    }
                }
                for (int i = 0, iMax = mask_._excs.Length; i < iMax; i++)
                {
                    if (_pools[mask_._excs[i]].Has(entityID_))
                    {
                        return false;
                    }
                }
                if (mask_._anys.Length != 0)
                {
                    int count = 0;
                    for (int i = 0, iMax = mask_._anys.Length; i < iMax; i++)
                    {
                        if (_pools[mask_._anys[i]].Has(entityID_))
                        {
                            count++;
                        }
                    }
                    if (count == 0)
                    {
                        return false;
                    }
                }

                return true;
            }
            bool deepDebug = IsMatchesMaskDeepDebug(mask, entityID);
#endif

            var incChuncks = mask._incChunckMasks;
            var excChuncks = mask._excChunckMasks;
            var anyChuncks = mask._anyChunckMasks;
            var componentMaskStartIndex = entityID << _entityComponentMaskLengthBitShift;

            for (int i = 0; i < incChuncks.Length; i++)
            {
                var bit = incChuncks[i];
                if ((_entityComponentMasks[componentMaskStartIndex + bit.chunkIndex] & bit.mask) != bit.mask)
                {
#if DEBUG && DRAGONECS_DEEP_DEBUG
                    if (false != deepDebug) { Throw.DeepDebugException(); }
#endif
                    return false;
                }
            }
            for (int i = 0; i < excChuncks.Length; i++)
            {
                var bit = excChuncks[i];
                if ((_entityComponentMasks[componentMaskStartIndex + bit.chunkIndex] & bit.mask) != 0)
                {
#if DEBUG && DRAGONECS_DEEP_DEBUG
                    if (false != deepDebug) { Throw.DeepDebugException(); }
#endif
                    return false;
                }
            }

            if (anyChuncks.Length > 0)
            {
                for (int i = 0; i < anyChuncks.Length; i++)
                {
                    var bit = anyChuncks[i];
                    if ((_entityComponentMasks[componentMaskStartIndex + bit.chunkIndex] & bit.mask) == bit.mask)
                    {
#if DEBUG && DRAGONECS_DEEP_DEBUG
                        if (true != deepDebug) { Throw.DeepDebugException(); }
#endif
                        return true;
                    }
                }
#if DEBUG && DRAGONECS_DEEP_DEBUG
                if (false != deepDebug) { Throw.DeepDebugException(); }
#endif
                return false;
            }


#if DEBUG && DRAGONECS_DEEP_DEBUG
            if (true != deepDebug) { Throw.DeepDebugException(); }
#endif
            return true;
        }
        #endregion

        #region Leaked
        public bool DeleteLeakedEntites()
        {
            if (_deleteLeakedEntitesLastVersion == _version)
            {
                return false;
            }
            int delCount = 0;
            foreach (var e in Entities)
            {
                ref var ent = ref _entities[e];
                if (ent.componentsCount <= 0 && ent.isUsed)
                {
                    DelEntity(e);
                    delCount++;
                }
            }
#if DEBUG
            if (delCount > 0)
            {
                EcsDebug.PrintWarning($"Detected and deleted {delCount} leaking entities.");
            }
#endif
            _deleteLeakedEntitesLastVersion = _version;
            return delCount > 0;
        }
        public int CountLeakedEntitesDebug()
        {
            if (_deleteLeakedEntitesLastVersion == _version)
            {
                return 0;
            }
            int delCount = 0;
            foreach (var e in Entities)
            {
                ref var ent = ref _entities[e];
                if (ent.componentsCount <= 0 && ent.isUsed)
                {
                    delCount++;
                }
            }
            return delCount;
        }
#endregion

        #region CopyEntity
        /// <summary>
        /// Copy all components from <paramref name="fromEntityID"/> to <paramref name="toEntityID"/> within this world.
        /// Efficient implementation uses a temporary buffer and calls pool.Copy for each component.
        /// </summary>
        /// <param name="fromEntityID">Source entity id.</param>
        /// <param name="toEntityID">Destination entity id.</param>
        /// <remarks>Components are copied by pool.Copy; destination will receive new component data but its prior components are not removed.</remarks>
        public unsafe void CopyEntity(int fromEntityID, int toEntityID)
        {
            const int BUFFER_THRESHOLD = 100;

            int count = GetComponentsCount(fromEntityID);

            int* poolIdsPtr;
            if (count < BUFFER_THRESHOLD)
            {
                int* ptr = stackalloc int[count];
                poolIdsPtr = ptr;
            }
            else
            {
                poolIdsPtr = MemoryAllocator.Alloc<int>(count).Ptr;
            }

            UnsafeArray<int> ua = UnsafeArray<int>.Manual(poolIdsPtr, count);

            GetComponentTypeIDsFor_Internal(fromEntityID, poolIdsPtr, count);
            for (int i = 0; i < count; i++)
            {
                _pools[poolIdsPtr[i]].Copy(fromEntityID, toEntityID);
            }

            if (count >= BUFFER_THRESHOLD)
            {
                MemoryAllocator.Free(poolIdsPtr);
            }
        }

        /// <summary>
        /// Copy selected components (by component type ids span) from source to destination entity in this world.
        /// </summary>
        /// <param name="fromEntityID">Source entity id.</param>
        /// <param name="toEntityID">Destination entity id.</param>
        /// <param name="componentTypeIDs">Span of component type ids to copy.</param>
        public void CopyEntity(int fromEntityID, int toEntityID, ReadOnlySpan<int> componentTypeIDs)
        {
            foreach (var poolID in componentTypeIDs)
            {
                var pool = _pools[poolID];
                if (pool.Has(fromEntityID))
                {
                    pool.Copy(fromEntityID, toEntityID);
                }
            }
        }
        /// <summary>
        /// Copy all components from this world entity to <paramref name="toEntityID"/> in <paramref name="toWorld"/>.
        /// Components are translated by pools and copied across worlds where supported.
        /// </summary>
        /// <param name="fromEntityID">Source entity id in this world.</param>
        /// <param name="toWorld">Target world where components will be copied.</param>
        /// <param name="toEntityID">Destination entity id in the target world.</param>
        public unsafe void CopyEntity(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
            const int BUFFER_THRESHOLD = 100;

            int count = GetComponentsCount(fromEntityID);

            int* poolIdsPtr;
            if (count < BUFFER_THRESHOLD)
            {
                int* ptr = stackalloc int[count];
                poolIdsPtr = ptr;
            }
            else
            {
                poolIdsPtr = MemoryAllocator.Alloc<int>(count).Ptr;
            }

            GetComponentTypeIDsFor_Internal(fromEntityID, poolIdsPtr, count);
            for (int i = 0; i < count; i++)
            {
                _pools[poolIdsPtr[i]].Copy(fromEntityID, toWorld, toEntityID);
            }

            if (count >= BUFFER_THRESHOLD)
            {
                MemoryAllocator.Free(poolIdsPtr);
            }
        }

        /// <summary>
        /// Copy selected components from this world entity to another world entity using provided component type ids.
        /// </summary>
        /// <param name="fromEntityID">Source entity id in this world.</param>
        /// <param name="toWorld">Target world.</param>
        /// <param name="toEntityID">Destination entity id in target world.</param>
        /// <param name="componentTypeIDs">Span of component type ids to copy across worlds.</param>
        public void CopyEntity(int fromEntityID, EcsWorld toWorld, int toEntityID, ReadOnlySpan<int> componentTypeIDs)
        {
            foreach (var poolID in componentTypeIDs)
            {
                var pool = _pools[poolID];
                if (pool.Has(fromEntityID))
                {
                    pool.Copy(fromEntityID, toWorld, toEntityID);
                }
            }
        }
        #endregion

        #region CloneEntity
        /// <summary>
        /// Create a new entity as a clone of <paramref name="entityID"/> inside this world and return its id.
        /// Copies all components from the source to the new entity.
        /// </summary>
        /// <param name="entityID">Source entity id to clone.</param>
        /// <returns>Newly created entity id that is a clone of the source.</returns>
        public int CloneEntity(int entityID)
        {
            int newEntity = NewEntity();
            CopyEntity(entityID, newEntity);
            return newEntity;
        }
        /// <summary>
        /// Create a new entity and copy only specified componentTypeIDs from source entity.
        /// </summary>
        /// <param name="entityID">Source entity id.</param>
        /// <param name="componentTypeIDs">Span of component type ids to copy.</param>
        /// <returns>Newly created entity id.</returns>
        public int CloneEntity(int entityID, ReadOnlySpan<int> componentTypeIDs)
        {
            int newEntity = NewEntity();
            CopyEntity(entityID, newEntity, componentTypeIDs);
            return newEntity;
        }
        /// <summary>
        /// Create a new entity in this world and copy components from source entity in another world.
        /// </summary>
        /// <param name="entityID">Source entity id in the source world.</param>
        /// <param name="toWorld">Target world to copy into (usually same world).</param>
        /// <returns>Newly created entity id in this world.</returns>
        public int CloneEntity(int entityID, EcsWorld toWorld)
        {
            int newEntity = NewEntity();
            CopyEntity(entityID, toWorld, newEntity);
            return newEntity;
        }
        /// <summary>
        /// Create a new entity in this world and copy specified components from a source entity in another world.
        /// </summary>
        /// <param name="entityID">Source entity id.</param>
        /// <param name="toWorld">Target world.</param>
        /// <param name="componentTypeIDs">Component type ids to copy.</param>
        /// <returns>New entity id in this world.</returns>
        public int CloneEntity(int entityID, EcsWorld toWorld, ReadOnlySpan<int> componentTypeIDs)
        {
            int newEntity = NewEntity();
            CopyEntity(entityID, toWorld, newEntity, componentTypeIDs);
            return newEntity;
        }

        /// <summary>
        /// Copy components from <paramref name="fromEntityID"/> to an existing <paramref name="toEntityID"/> and
        /// remove any extra components existing on the destination that are not present on the source.
        /// </summary>
        /// <param name="fromEntityID">Source entity id.</param>
        /// <param name="toEntityID">Destination entity id to overwrite/align with source.</param>
        public void CloneEntity(int fromEntityID, int toEntityID)
        {
            CopyEntity(fromEntityID, toEntityID);
            foreach (var pool in _pools)
            {
                if (!pool.Has(fromEntityID) && pool.Has(toEntityID))
                {
                    pool.Del(toEntityID);
                }
            }
        }
        //public void CloneEntity(int fromEntityID, EcsWorld toWorld, int toEntityID)
        #endregion

        #region MoveComponents
        /// <summary>
        /// Move specified components from one entity to another: copy then delete from source.
        /// </summary>
        /// <param name="fromEntityID">Source entity id.</param>
        /// <param name="toEntityID">Destination entity id.</param>
        /// <param name="componentTypeIDs">Span of component type ids to move.</param>
        public void MoveComponents(int fromEntityID, int toEntityID, ReadOnlySpan<int> componentTypeIDs)
        {
            foreach (var poolID in componentTypeIDs)
            {
                var pool = _pools[poolID];
                if (pool.Has(fromEntityID))
                {
                    pool.Copy(fromEntityID, toEntityID);
                    pool.Del(fromEntityID);
                }
            }
        }
        #endregion

        #region RemoveComponents
        /// <summary>
        /// Remove specified components from the given entity if they exist.
        /// </summary>
        /// <param name="fromEntityID">Entity id to remove components from.</param>
        /// <param name="componentTypeIDs">Span of component type ids to remove.</param>
        public void RemoveComponents(int fromEntityID, ReadOnlySpan<int> componentTypeIDs)
        {
            foreach (var poolID in componentTypeIDs)
            {
                var pool = _pools[poolID];
                if (pool.Has(fromEntityID))
                {
                    pool.Del(fromEntityID);
                }
            }
        }
        #endregion

#endregion

        #region DelEntBuffer
        /// <summary>
        /// Disable automatic processing of the deferred-delete buffer and return a scope object which will restore the previous value on dispose.
        /// Use in a using block to temporarily disable auto-release behavior.
        /// </summary>
        /// <returns>Scope object which will restore previous auto-release setting when disposed.</returns>
        public IsEnableAutoReleaseDelEntBufferScope DisableAutoReleaseDelEntBuffer()
        {
            return new IsEnableAutoReleaseDelEntBufferScope(this, false);
        }
        /// <summary>
        /// Enable automatic processing of the deferred-delete buffer and return a scope object which will restore the previous value on dispose.
        /// Use in a using block to temporarily enable auto-release behavior.
        /// </summary>
        /// <returns>Scope object which will restore previous auto-release setting when disposed.</returns>
        public IsEnableAutoReleaseDelEntBufferScope EnableAutoReleaseDelEntBuffer()
        {
            return new IsEnableAutoReleaseDelEntBufferScope(this, true);
        }
        private void SetEnableAutoReleaseDelEntBuffer(bool value)
        {
            _isEnableAutoReleaseDelEntBuffer = value;
        }
        public readonly struct IsEnableAutoReleaseDelEntBufferScope : IDisposable
        {
            private readonly EcsWorld _source;
            private readonly bool _lastValue;
            public IsEnableAutoReleaseDelEntBufferScope(EcsWorld source, bool value)
            {
                _lastValue = source._isEnableAutoReleaseDelEntBuffer;
                source.SetEnableAutoReleaseDelEntBuffer(value);
                _source = source;
            }
            public void End()
            {
                _source.SetEnableAutoReleaseDelEntBuffer(_lastValue);
            }
            void IDisposable.Dispose()
            {
                End();
            }
        }
        /// <summary>
        /// Release and process all entities currently stored in the deferred-delete buffer.
        /// Components will be removed and entity ids freed as part of processing.
        /// </summary>
        public void ReleaseDelEntityBufferAll()
        {
            ReleaseDelEntityBuffer(-1);
        }
        /// <summary>
        /// Release and process up to <paramref name="count"/> entities from the deferred-delete buffer. If count is negative, process all.
        /// </summary>
        /// <param name="count">Maximum number of deferred deletes to process; negative means all.</param>
        public void ReleaseDelEntityBuffer(int count)
        {
            if (_emptyEntitiesLength <= 0 && _delEntBufferCount <= 0) { return; }
            unchecked { _version++; }

            ReleaseEmptyEntitiesBuffer();

            if (count < 0)
            {
                count = _delEntBufferCount;
            }

            count = Math.Max(0, Math.Min(count, _delEntBufferCount));
            _delEntBufferCount -= count;
            int slisedCount = count;

            for (int i = 0; i < slisedCount; i++)
            {
                int e = _delEntBuffer[i];
                if (_entities[e].componentsCount <= 0)
                {
                    int tmp = _delEntBuffer[i];
                    _delEntBuffer[i] = _delEntBuffer[--slisedCount];
                    _delEntBuffer[slisedCount] = tmp;
                    i--;
                }
            }

            //если фулл очистка то _delEntBufferCount будет 0

            ReadOnlySpan<int> fullBuffer = new ReadOnlySpan<int>(_delEntBuffer, _delEntBufferCount, count);
            if (slisedCount > 0)
            {
                ReadOnlySpan<int> bufferSlised = new ReadOnlySpan<int>(_delEntBuffer, _delEntBufferCount, slisedCount);
                for (int i = 0; i < _poolsCount; i++)
                {
                    _pools[i].OnReleaseDelEntityBuffer(bufferSlised);
                }
            }
            for (int i = 0; i < _groups.Count; i++)
            {
                if (_groups[i].TryGetTarget(out EcsGroup group))
                {
                    if (group.IsReleased)
                    {
                        group.OnReleaseDelEntityBuffer_Internal(fullBuffer);
                    }
                }
                else
                {
                    RemoveGroupAt(i--);
                }
            }

            _listeners.InvokeOnReleaseDelEntityBuffer(fullBuffer);
            for (int i = 0; i < fullBuffer.Length; i++)
            {
                int e = fullBuffer[i];
                _entityDispenser.Release(e);
                _entities[e].gen |= GEN_SLEEP_MASK;
            }
            Densify();
        }
        private void Densify() //уплотнение свободных айдишников
        {
            _entityDispenser.Sort();
        }
        #endregion

        #region Upsize
        /// <summary>
        /// Ensure internal entity storage capacity is at least <paramref name="minSize"/>.
        /// </summary>
        /// <param name="minSize">Minimal required capacity for entity storage.</param>
        public void Upsize(int minSize)
        {
            _entityDispenser.Upsize(minSize);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private int CalcEntityComponentMaskLength()
        {
            int result = _pools.Length / COMPONENT_MASK_CHUNK_SIZE;
            return (result < 2 ? 2 : result);
        }
        private void SetEntityComponentMaskLength(int value)
        {
            _entityComponentMaskLength = value;
            _entityComponentMaskLengthBitShift = BitsUtility.GetHighBitNumber(value);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnEntityDispenserResized(int newSize)
        {
            SetEntityComponentMaskLength(CalcEntityComponentMaskLength()); //_pools.Length / COMPONENT_MASK_CHUNK_SIZE + 1;
            Array.Resize(ref _entities, newSize);
            Array.Resize(ref _delEntBuffer, newSize);
            Array.Resize(ref _emptyEntities, newSize);
            Array.Resize(ref _entityComponentMasks, newSize * _entityComponentMaskLength);

            ArrayUtility.Fill(_entities, EntitySlot.Empty, _entitiesCapacity);

            _entitiesCapacity = newSize;

            for (int i = 0; i < _groups.Count; i++)
            {
                if (_groups[i].TryGetTarget(out EcsGroup group))
                {
                    group.OnWorldResize_Internal(newSize);
                }
                else
                {
                    RemoveGroupAt(i--);
                }
            }
            foreach (var item in _pools)
            {
                item.OnWorldResize(newSize);
            }
            _listeners.InvokeOnWorldResize(newSize);
        }
        #endregion

        #region Listeners
        /// <summary>
        /// Add a listener for world-level events (resize, release DelEntityBuffer, destroy).
        /// </summary>
        /// <param name="worldEventListener">Listener to add.</param>
        public void AddListener(IEcsWorldEventListener worldEventListener)
        {
            _listeners.Add(worldEventListener);
        }
        /// <summary>
        /// Remove a previously added world-level event listener.
        /// </summary>
        /// <param name="worldEventListener">Listener to remove.</param>
        public void RemoveListener(IEcsWorldEventListener worldEventListener)
        {
            _listeners.Remove(worldEventListener);
        }
        /// <summary>
        /// Add an entity-level event listener (new, migrate, del notifications).
        /// </summary>
        /// <param name="entityEventListener">Listener to add.</param>
        public void AddListener(IEcsEntityEventListener entityEventListener)
        {
            _entityListeners.Add(entityEventListener);
            _hasAnyEntityListener = _entityListeners.Count > 0;
        }
        /// <summary>
        /// Remove a previously added entity-level event listener.
        /// </summary>
        /// <param name="entityEventListener">Listener to remove.</param>
        public void RemoveListener(IEcsEntityEventListener entityEventListener)
        {
            _entityListeners.Remove(entityEventListener);
            _hasAnyEntityListener = _entityListeners.Count > 0;
        }
        #endregion

        #region Other
        /// <summary>
        /// Force increment of the internal world version counter. Use for external operations that need to mark the world as changed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AggressiveUpVersion() { UpVersion(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpVersion()
        {
            unchecked
            {
                _version++;
                _deleteLeakedEntitesLastVersion++;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpVersionLeaked()
        {
            unchecked { _version++; }
        }
        #endregion

        #region Entity Debug Utils / Get Components
        public readonly ref struct EntitySlotMeta
        {
            public readonly EcsWorld World;
            public readonly int EntityID;
         
            public string Name
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return World._entities[EntityID].metaName; }
                set
                {
                    ref var slot = ref World._entities[EntityID];
                    slot.metaName = value;
                    World.EntityMetaChanged.Invoke(this);
                }
            }
            public MetaColor Color
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return World._entities[EntityID].metaColor; }
                set
                {
                    ref var slot = ref World._entities[EntityID];
                    slot.metaColor = value;
                    World.EntityMetaChanged.Invoke(this);
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public EntitySlotMeta(EcsWorld world, int entityID)
            {
                World = world;
                EntityID = entityID;
            }
        }
        /// <summary>
        /// Return an EntitySlotMeta helper for an entity id. Use to read and set debug metadata (Name, Color) for the slot.
        /// </summary>
        /// <param name="entityID">Entity id to get metadata for.</param>
        /// <returns>EntitySlotMeta struct bound to the specified entity id.</returns>
        public EntitySlotMeta GetEntitySlotMeta(int entityID)
        {
            return new EntitySlotMeta(this, entityID);
        }
        public delegate void EntityMetaChangedHandler(EntitySlotMeta meta);

        public event EntityMetaChangedHandler EntityMetaChanged = delegate { };

        [ThreadStatic]
        private static int[] _componentIDsBuffer;
        [ThreadStatic]
        private static object[] _componentsBuffer;

        /// <summary>
        /// Return a readonly span of component type ids attached to the given entity. Backed by a thread-static buffer.
        /// </summary>
        /// <param name="entityID">Entity id to query.</param>
        /// <returns>ReadOnlySpan of component type ids currently attached to the entity.</returns>
        public ReadOnlySpan<int> GetComponentTypeIDsFor(int entityID)
        {
            int count = GetComponentTypeIDsFor_Internal(entityID, ref _componentIDsBuffer);
            return new ReadOnlySpan<int>(_componentIDsBuffer, 0, count);
        }
        /// <summary>
        /// Return the first (lowest) component type id attached to the entity or -1 if none.
        /// </summary>
        /// <param name="entityID">Entity id to query.</param>
        /// <returns>First component type id or -1 when entity has no components.</returns>
        public int GetFirstComponentTypeIDFor(int entityID)
        {
            int poolIndex = 0;
            for (int chunkIndex = entityID << _entityComponentMaskLengthBitShift,
                chunkIndexMax = chunkIndex + _entityComponentMaskLength;
                chunkIndex < chunkIndexMax;
                chunkIndex++)
            {
                int chunk = _entityComponentMasks[chunkIndex];
                if (chunk == 0)
                {
                    poolIndex += COMPONENT_MASK_CHUNK_SIZE;
                }
                else
                {
                    return BitsUtility.GetHighBitNumber(chunk) + poolIndex;
                }
            }
            return -1;
        }

        /// <summary>
        /// Populate <paramref name="bufferList"/> with IEcsPool references for each component attached to the entity.
        /// Efficient for inspection or iteration when pool objects are required.
        /// </summary>
        /// <param name="entityID">Entity id to query.</param>
        /// <param name="bufferList">List to be filled with pool references (cleared or filled appropriately).</param>
        public unsafe void GetComponentPoolsFor(int entityID, List<IEcsPool> bufferList)
        {
            const int BUFFER_THRESHOLD = 256;

            var count = GetComponentsCount(entityID);

            if (count <= 0)
            {
                bufferList.Clear();
                return;
            }

            int* poolIdsPtr;
            if (count < BUFFER_THRESHOLD)
            {
                int* ptr = stackalloc int[count];
                poolIdsPtr = ptr;
            }
            else
            {
                poolIdsPtr = MemoryAllocator.Alloc<int>(count).Ptr;
            }

            GetComponentTypeIDsFor_Internal(entityID, poolIdsPtr, count);

            if (bufferList.Count == count)
            {
                for (int i = 0; i < count; i++)
                {
                    bufferList[i] = _pools[poolIdsPtr[i]];
                }
            }
            else
            {
                bufferList.Clear();
                for (int i = 0; i < count; i++)
                {
                    bufferList.Add(_pools[poolIdsPtr[i]]);
                }
            }

            if (count >= BUFFER_THRESHOLD)
            {
                MemoryAllocator.Free(poolIdsPtr);
            }
        }
        /// <summary>
        /// Return a readonly span of raw component objects attached to the specified entity. Uses a thread-static buffer.
        /// </summary>
        /// <param name="entityID">Entity id to query.</param>
        /// <returns>ReadOnlySpan of raw component objects for the entity.</returns>
        public ReadOnlySpan<object> GetComponentsFor(int entityID)
        {
            int count = GetComponentTypeIDsFor_Internal(entityID, ref _componentIDsBuffer);
            ArrayUtility.UpsizeWithoutCopy(ref _componentsBuffer, count);

            for (int i = 0; i < count; i++)
            {
                _componentsBuffer[i] = _pools[_componentIDsBuffer[i]].GetRaw(entityID);
            }
            return new ReadOnlySpan<object>(_componentsBuffer, 0, count);
        }
        /// <summary>
        /// Populate the provided list with raw component objects attached to the entity.
        /// </summary>
        /// <param name="entityID">Entity id to query.</param>
        /// <param name="bufferList">List to be filled with component objects.</param>
        public void GetComponentsFor(int entityID, List<object> bufferList)
        {
            bufferList.Clear();
            int count = GetComponentTypeIDsFor_Internal(entityID, ref _componentIDsBuffer);
            for (int i = 0; i < count; i++)
            {
                bufferList.Add(_pools[_componentIDsBuffer[i]].GetRaw(entityID));
            }
        }
        /// <summary>
        /// Fill the provided HashSet with System.Type objects for each component attached to the entity.
        /// </summary>
        /// <param name="entityID">Entity id to query.</param>
        /// <param name="typeBufferSet">HashSet to be filled with component System.Type objects.</param>
        public void GetComponentTypesFor(int entityID, HashSet<Type> typeBufferSet)
        {
            typeBufferSet.Clear();
            int count = GetComponentTypeIDsFor_Internal(entityID, ref _componentIDsBuffer);
            for (int i = 0; i < count; i++)
            {
                typeBufferSet.Add(_pools[_componentIDsBuffer[i]].ComponentType);
            }
        }
        private int GetComponentTypeIDsFor_Internal(int entityID, ref int[] componentIDs)
        {
            var itemsCount = GetComponentsCount(entityID);
            ArrayUtility.UpsizeWithoutCopy(ref componentIDs, itemsCount);

            if (itemsCount <= 0) { return 0; }

            const int LO_CHANK_HALF = 65535;
            const int HI_CHANK_HALF = -65536;
            const int COMPONENT_MASK_CHUNK_SIZE_HALF = COMPONENT_MASK_CHUNK_SIZE / 2;
            // проверка на itemsCount <= 0 не обяательна, алгоритм не ломается,
            // только впустую отрабатыват по всем чанкам,
            // но как правильно для пустых сущностей этот алгоритм не применим.
            int poolIndex = 0;
            int bit;
            int arrayIndex = 0;
            for (int chunkIndex = entityID << _entityComponentMaskLengthBitShift,
                    chunkIndexMax = chunkIndex + _entityComponentMaskLength;
                    chunkIndex < chunkIndexMax;
                    chunkIndex++)
            {
                int chunk = _entityComponentMasks[chunkIndex];
                if (chunk == 0)
                {
                    poolIndex += COMPONENT_MASK_CHUNK_SIZE;
                }
                else
                {
                    if ((chunk & LO_CHANK_HALF) != 0)
                    {
                        bit = 0x0000_0001;
                        while (bit < 0x0001_0000)
                        {
                            if ((chunk & bit) != 0)
                            {
                                componentIDs[arrayIndex++] = poolIndex;

                                itemsCount--;
                                if (itemsCount <= 0) { return arrayIndex; }
                            }
                            poolIndex++;
                            bit <<= 1;
                        }
                    }
                    else
                    {
                        poolIndex += COMPONENT_MASK_CHUNK_SIZE_HALF;
                    }
                    if ((chunk & HI_CHANK_HALF) != 0)
                    {
                        bit = 0x0001_0000;
                        while (bit != 0x0000_0000)
                        {
                            if ((chunk & bit) != 0)
                            {
                                componentIDs[arrayIndex++] = poolIndex;

                                itemsCount--;
                                if (itemsCount <= 0) { return arrayIndex; }
                            }
                            poolIndex++;
                            bit <<= 1;
                        }
                    }
                    else
                    {
                        poolIndex += COMPONENT_MASK_CHUNK_SIZE_HALF;
                    }
                }
            }

            return itemsCount;
        }
        private unsafe void GetComponentTypeIDsFor_Internal(int entityID, int* componentIDs, int itemsCount)
        {
            const int LO_CHANK_HALF = 65535;
            const int HI_CHANK_HALF = -65536;
            const int COMPONENT_MASK_CHUNK_SIZE_HALF = COMPONENT_MASK_CHUNK_SIZE / 2;
            // проверка на itemsCount <= 0 не обяательна, алгоритм не ломается,
            // только впустую отрабатыват по всем чанкам,
            // но как правильно для пустых сущностей этот алгоритм не применим.
            int poolIndex = 0;
            int bit;
            for (int chunkIndex = entityID << _entityComponentMaskLengthBitShift,
                    chunkIndexMax = chunkIndex + _entityComponentMaskLength;
                chunkIndex < chunkIndexMax;
                chunkIndex++)
            {
                int chunk = _entityComponentMasks[chunkIndex];
                if (chunk == 0)
                {
                    poolIndex += COMPONENT_MASK_CHUNK_SIZE;
                }
                else
                {
                    if ((chunk & LO_CHANK_HALF) != 0)
                    {
                        bit = 0x0000_0001;
                        while (bit < 0x0001_0000)
                        {
                            if ((chunk & bit) != 0)
                            {
                                *componentIDs = poolIndex;
                                componentIDs++;

                                itemsCount--;
                                if (itemsCount <= 0) { return; }
                            }
                            poolIndex++;
                            bit <<= 1;
                        }
                    }
                    else
                    {
                        poolIndex += COMPONENT_MASK_CHUNK_SIZE_HALF;
                    }
                    if ((chunk & HI_CHANK_HALF) != 0)
                    {
                        bit = 0x0001_0000;
                        while (bit != 0x0000_0000)
                        {
                            if ((chunk & bit) != 0)
                            {
                                *componentIDs = poolIndex;
                                componentIDs++;

                                itemsCount--;
                                if (itemsCount <= 0) { return; }
                            }
                            poolIndex++;
                            bit <<= 1;
                        }
                    }
                    else
                    {
                        poolIndex += COMPONENT_MASK_CHUNK_SIZE_HALF;
                    }
                }
            }
        }
        #endregion

        #region EntitySlot
        [StructLayout(LayoutKind.Sequential)]
        private struct EntitySlot
        {
            public static readonly EntitySlot Empty = new EntitySlot(GEN_SLEEP_MASK, 0, false);
            public short gen;
            public short componentsCount;
            public bool isUsed;
            public string metaName;
            public MetaColor metaColor;
            public EntitySlot(short gen, short componentsCount, bool isUsed)
            {
                this.gen = gen;
                this.componentsCount = componentsCount;
                this.isUsed = isUsed;
                metaName = string.Empty;
                metaColor = new MetaColor(0, 0, 0, 0);
            }
        }
        #endregion

        #region DebuggerProxy
        protected partial class DebuggerProxy
        {
            private EcsWorld _world;
            private List<MaskQueryExecutor> _queries;
            public string Name { get { return _world.Name; } }
            public RawEntLong[] Entities
            {
                get
                {
                    RawEntLong[] result = new RawEntLong[_world.Count];
                    int i = 0;
                    using (_world.DisableAutoReleaseDelEntBuffer())
                    {
                        foreach (var e in _world.ToSpan())
                        {
                            result[i++] = _world.GetRawEntLong(e);
                        }
                    }
                    return result;
                }
            }
            public long Version { get { return _world.Version; } }
            public IEcsPool[] Pools { get { return _world._pools; } }
            public short ID { get { return _world.ID; } }
            public bool IsDestroyed { get { return _world._isDestroyed; } }
            public List<MaskQueryExecutor> MaskQueries { get { return _queries; } }
            public DebuggerProxy(EcsWorld world)
            {
                _world = world;
                int v = 0;
                _queries = new List<MaskQueryExecutor>();
                world.GetMaskQueryExecutors(_queries, ref v);
            }
        }
        #endregion

        #region Internal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsSpan GetCurrentEntities_Internal()
        {
            return _entityDispenser.UsedToEcsSpan(ID);
        }
        #endregion

        public static event Action<EcsWorld> OnWorldCreated;
    }

    #region Callbacks Interface
    public interface IEcsWorldEventListener
    {
        void OnWorldResize(int newSize);
        void OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer);
        void OnWorldDestroy();
    }
    public interface IEcsEntityEventListener
    {
        void OnNewEntity(int entityID);
        void OnMigrateEntity(int entityID);
        void OnDelEntity(int entityID);
    }
    internal static class WorldEventListExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnWorldResize(this ref StructList<IEcsWorldEventListener> self, int newSize)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++)
            {
                self[i].OnWorldResize(newSize);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnReleaseDelEntityBuffer(this ref StructList<IEcsWorldEventListener> self, ReadOnlySpan<int> buffer)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++)
            {
                self[i].OnReleaseDelEntityBuffer(buffer);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnWorldDestroy(this ref StructList<IEcsWorldEventListener> self)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++)
            {
                self[i].OnWorldDestroy();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnNewEntity(this ref StructList<IEcsEntityEventListener> self, int entityID)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++)
            {
                self[i].OnNewEntity(entityID);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnMigrateEntity(this ref StructList<IEcsEntityEventListener> self, int entityID)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++)
            {
                self[i].OnMigrateEntity(entityID);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnDelEntity(this ref StructList<IEcsEntityEventListener> self, int entityID)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++)
            {
                self[i].OnDelEntity(entityID);
            }
        }
    }
    #endregion

    #region Extensions
    public static class EcsWorldExtenssions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNullOrDetroyed(this EcsWorld self)
        {
            return self == null || self.IsDestroyed;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReleaseDelEntityBufferAllAuto(this EcsWorld self)
        {
            if (self.IsEnableReleaseDelEntBuffer)
            {
                self.ReleaseDelEntityBufferAll();
            }
        }
    }
    #endregion
}