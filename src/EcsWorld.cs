#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.PoolsCore;
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
        public EcsWorldConfig() : this(512) { }
        public EcsWorldConfig(int entitiesCapacity = 512, int groupCapacity = 512, int poolsCapacity = 512, int poolComponentsCapacity = 512, int poolRecycledComponentsCapacity = 512 / 2)
        {
            EntitiesCapacity = entitiesCapacity;
            GroupCapacity = groupCapacity;
            PoolsCapacity = poolsCapacity;
            PoolComponentsCapacity = poolComponentsCapacity;
            PoolRecycledComponentsCapacity = poolRecycledComponentsCapacity;
        }
    }

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

        #region Properties
        EcsWorld IEntityStorage.World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return this; }
        }
        public IConfigContainer Configs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _configs; }
        }
        public string Name
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _name; }
        }
        public long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _version; }
        }
        public bool IsDestroyed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isDestroyed; }
        }
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _entitiesCount; }
        }
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _entitiesCapacity; }
        }
        public int DelEntBufferCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _delEntBufferCount; }
        }
        public bool IsEnableReleaseDelEntBuffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isEnableAutoReleaseDelEntBuffer; }
        }

        public EcsSpan Entities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ReleaseDelEntityBufferAll();
                return GetCurrentEntities_Internal();
            }
        }
        public int PoolsCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _poolsCount; }
        }
        public ReadOnlySpan<IEcsPool> AllPools
        {
            // new ReadOnlySpan<IEcsPoolImplementation>(pools, 0, _poolsCount);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _pools; }
        }
        #endregion

        #region Constructors/Destroy
        public EcsWorld() : this(ConfigContainer.Empty, null, -1) { }
        public EcsWorld(EcsWorldConfig config = null, short worldID = -1) : this(config == null ? ConfigContainer.Empty : new ConfigContainer().Set(config), null, worldID) { }
        public EcsWorld(IConfigContainer configs, short worldID = -1) : this(configs, null, worldID) { }
        public EcsWorld(EcsWorldConfig config = null, string name = null, short worldID = -1) : this(config == null ? ConfigContainer.Empty : new ConfigContainer().Set(config), name, worldID) { }
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
                if (controllersCount < _allWorldComponentPools.Capacity)
                {
                    _allWorldComponentPools.Capacity = controllersCount;
                }

                if (worldID < 0 || (worldID == NULL_WORLD_ID && nullWorld == false))
                {
                    worldID = (short)_worldIdDispenser.UseFree();
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

                _poolsMediator = new PoolsMediator(this);

                int poolsCapacity = ArrayUtility.NextPow2(config.PoolsCapacity);
                _pools = new IEcsPoolImplementation[poolsCapacity];
                _poolSlots = new PoolSlot[poolsCapacity];
                ArrayUtility.Fill(_pools, _nullPool);

                int entitiesCapacity = ArrayUtility.NextPow2(config.EntitiesCapacity);
                _entityDispenser = new IdDispenser(entitiesCapacity, 0, OnEntityDispenserResized);

                _executorCoures = new Dictionary<(Type, object), IQueryExecutorImplementation>(config.PoolComponentsCapacity);

                GetComponentTypeID<NullComponent>();
            }
        }
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
                _listeners.InvokeOnWorldDestroy();
                _entityDispenser = null;
                _pools = null;
                _nullPool = null;
                _worlds[ID] = null;
                ReleaseData(ID);
                _worldIdDispenser.Release(ID);
                _isDestroyed = true;
                _poolTypeCode_2_CmpTypeIDs = null;
                _cmpTypeCode_2_CmpTypeIDs = null;

                foreach (var item in _executorCoures)
                {
                    item.Value.Destroy();
                }
                //_entities - не обнуляется для работы entlong.IsAlive
            }
        }
        //public void Clear() { }
        #endregion

        #region Getters
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TAspect GetAspect<TAspect>() where TAspect : new()
        {
            return Get<AspectCache<TAspect>>().Instance;
        }
        public void GetAspects<TAspect0>(out TAspect0 a0)
            where TAspect0 : new()
        {
            a0 = GetAspect<TAspect0>();
        }
        public void GetAspects<TAspect0, TAspect1>(out TAspect0 a0, out TAspect1 a1)
            where TAspect0 : new()
            where TAspect1 : new()
        {
            a0 = GetAspect<TAspect0>();
            a1 = GetAspect<TAspect1>();
        }
        public void GetAspects<TAspect0, TAspect1, TAspect2>(out TAspect0 a0, out TAspect1 a1, out TAspect2 a2)
            where TAspect0 : new()
            where TAspect1 : new()
            where TAspect2 : new()
        {
            a0 = GetAspect<TAspect0>();
            a1 = GetAspect<TAspect1>();
            a2 = GetAspect<TAspect2>();
        }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TAspect GetAspect<TAspect>(out EcsMask mask) where TAspect : new()
        {
            var result = Get<AspectCache<TAspect>>();
            mask = result.Mask;
            return result.Instance;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetQueryCache<TExecutor, TAspect>(out TExecutor executor, out TAspect aspect)
            where TExecutor : MaskQueryExecutor, new()
            where TAspect : new()
        {
            ref var cmp = ref Get<WhereQueryCache<TExecutor, TAspect>>();
            executor = cmp.Executor;
            aspect = cmp.Aspcet;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get<T>() where T : struct
        {
            return ref WorldComponentPool<T>.GetForWorld(ID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has<T>() where T : struct
        {
            return WorldComponentPool<T>.Has(ID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetUnchecked<T>() where T : struct
        {
            return ref WorldComponentPool<T>.GetForWorldUnchecked(ID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Get<T>(short worldID) where T : struct
        {
            return ref WorldComponentPool<T>.GetForWorld(worldID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has<T>(short worldID) where T : struct
        {
            return WorldComponentPool<T>.Has(worldID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetUnchecked<T>(short worldID) where T : struct
        {
            return ref WorldComponentPool<T>.GetForWorldUnchecked(worldID);
        }
        #endregion

        #region Entity

        #region New/Del
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong NewEntityLong()
        {
            int entityID = NewEntity();
            return GetEntityLong(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong NewEntityLong(int entityID)
        {
            NewEntity(entityID);
            return GetEntityLong(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NewEntity()
        {
            int entityID = _entityDispenser.UseFree();
            CreateConcreteEntity(entityID);
            return entityID;
        }
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
            _entityListeners.InvokeOnNewEntity(entityID);
            MoveToEmptyEntities(entityID);
        }


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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DelEntity(entlong entity)
        {
            DelEntity(entity.ID);
        }
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
            _entities[entityID].isUsed = false;
            _entitiesCount--;
            _entityListeners.InvokeOnDelEntity(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MoveToEmptyEntities(int entityID)
        {
            _emptyEntities[_emptyEntitiesCount++] = entityID;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool RemoveFromEmptyEntities(int entityID)
        {
            for (int i = _emptyEntitiesCount - 1; i >= 0; i--)
            {
                if(_emptyEntities[i] == entityID)
                {
                    _emptyEntities[i] = _emptyEntities[--_emptyEntitiesCount];
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ToSpan()
        {
            if (_isEnableAutoReleaseDelEntBuffer)
            {
                ReleaseDelEntityBufferAll();
            }
            return _entityDispenser.UsedToEcsSpan(ID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe entlong GetEntityLong(int entityID)
        {
            long x = (long)ID << 48 | (long)GetGen(entityID) << 32 | (long)entityID;
            return *(entlong*)&x;
        }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntitySlotInfo GetEntitySlotInfoDebug(int entityID)
        {
            return new EntitySlotInfo(entityID, _entities[entityID].gen, ID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(int entityID, short gen)
        {
            ref var slot = ref _entities[entityID];
            return slot.gen == gen && slot.isUsed;
        }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUsed(int entityID)
        {
            return _entities[entityID].isUsed;
        }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetComponentsCount(int entityID)
        {
            return _entities[entityID].componentsCount;
        }

        public bool IsMatchesMask(IComponentMask mask, int entityID)
        {
            return IsMatchesMask(mask.ToMask(this), entityID);
        }
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
                return true;
            }
            bool deepDebug = IsMatchesMaskDeepDebug(mask, entityID);
#endif

            var incChuncks = mask._incChunckMasks;
            var excChuncks = mask._excChunckMasks;
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
            if (delCount > 0)
            {
                EcsDebug.PrintWarning($"Detected and deleted {delCount} leaking entities.");
            }
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

        //TODO протестить Copy Clone Move Remove

        #region CopyEntity
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
                poolIdsPtr = UnmanagedArrayUtility.New<int>(count);
            }

            UnsafeArray<int> ua = UnsafeArray<int>.Manual(poolIdsPtr, count);

            GetComponentTypeIDsFor_Internal(fromEntityID, poolIdsPtr, count);
            for (int i = 0; i < count; i++)
            {
                _pools[poolIdsPtr[i]].Copy(fromEntityID, toEntityID);
            }

            if (count >= BUFFER_THRESHOLD)
            {
                UnmanagedArrayUtility.Free(poolIdsPtr);
            }


            //foreach (var pool in _pools)
            //{
            //    if (pool.Has(fromEntityID))
            //    {
            //        pool.Copy(fromEntityID, toEntityID);
            //    }
            //}
        }
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
                poolIdsPtr = UnmanagedArrayUtility.New<int>(count);
            }

            GetComponentTypeIDsFor_Internal(fromEntityID, poolIdsPtr, count);
            for (int i = 0; i < count; i++)
            {
                _pools[poolIdsPtr[i]].Copy(fromEntityID, toWorld, toEntityID);
            }

            if (count >= BUFFER_THRESHOLD)
            {
                UnmanagedArrayUtility.Free(poolIdsPtr);
            }

            //foreach (var pool in _pools)
            //{
            //    if (pool.Has(fromEntityID))
            //    {
            //        pool.Copy(fromEntityID, toWorld, toEntityID);
            //    }
            //}
        }
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
        public int CloneEntity(int entityID)
        {
            int newEntity = NewEntity();
            CopyEntity(entityID, newEntity);
            return newEntity;
        }
        public int CloneEntity(int entityID, ReadOnlySpan<int> componentTypeIDs)
        {
            int newEntity = NewEntity();
            CopyEntity(entityID, newEntity, componentTypeIDs);
            return newEntity;
        }
        public int CloneEntity(int entityID, EcsWorld toWorld)
        {
            int newEntity = NewEntity();
            CopyEntity(entityID, toWorld, newEntity);
            return newEntity;
        }
        public int CloneEntity(int entityID, EcsWorld toWorld, ReadOnlySpan<int> componentTypeIDs)
        {
            int newEntity = NewEntity();
            CopyEntity(entityID, toWorld, newEntity, componentTypeIDs);
            return newEntity;
        }

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
        public IsEnableAutoReleaseDelEntBufferScope DisableAutoReleaseDelEntBuffer()
        {
            return new IsEnableAutoReleaseDelEntBufferScope(this, false);
        }
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
        public void ReleaseDelEntityBufferAll()
        {
            ReleaseDelEntityBuffer(-1);
        }
        public unsafe void ReleaseDelEntityBuffer(int count)
        {
            if (_emptyEntitiesCount <= 0 && _delEntBufferCount <= 0) { return; }
            unchecked { _version++; }

            for (int i = 0; i < _emptyEntitiesCount; i++)
            {
                TryDelEntity(_emptyEntities[i]);
            }
            _emptyEntitiesCount = 0;

            if(count < 0)
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
        public void AddListener(IEcsWorldEventListener worldEventListener)
        {
            _listeners.Add(worldEventListener);
        }
        public void RemoveListener(IEcsWorldEventListener worldEventListener)
        {
            _listeners.Remove(worldEventListener);
        }
        public void AddListener(IEcsEntityEventListener entityEventListener)
        {
            _entityListeners.Add(entityEventListener);
        }
        public void RemoveListener(IEcsEntityEventListener entityEventListener)
        {
            _entityListeners.Remove(entityEventListener);
        }
        #endregion

        #region Other
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
            unchecked
            {
                _version++;
            }
        }
        #endregion

        #region Debug Components
        [ThreadStatic]
        private static int[] _componentIDsBuffer;
        [ThreadStatic]
        private static object[] _componentsBuffer;
        public ReadOnlySpan<int> GetComponentTypeIDsFor(int entityID)
        {
            int count = GetComponentTypeIDsFor_Internal(entityID, ref _componentIDsBuffer);
            return new ReadOnlySpan<int>(_componentIDsBuffer, 0, count);
        }
        public unsafe void GetComponentPoolsFor(int entityID, List<IEcsPool> list)
        {
            const int BUFFER_THRESHOLD = 100;

            var count = GetComponentsCount(entityID);

            if (count <= 0)
            {
                list.Clear();
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
                poolIdsPtr = UnmanagedArrayUtility.New<int>(count);
            }

            GetComponentTypeIDsFor_Internal(entityID, poolIdsPtr, count);

            if (list.Count == count)
            {
                for (int i = 0; i < count; i++)
                {
                    list[i] = _pools[poolIdsPtr[i]];
                }
            }
            else
            {
                list.Clear();
                for (int i = 0; i < count; i++)
                {
                    list.Add(_pools[poolIdsPtr[i]]);
                }
            }
        }
        public ReadOnlySpan<object> GetComponentsFor(int entityID)
        {
            int count = GetComponentTypeIDsFor_Internal(entityID, ref _componentIDsBuffer);
            ArrayUtility.UpsizeWithoutCopy(ref _componentIDsBuffer, count);

            for (int i = 0; i < count; i++)
            {
                _componentsBuffer[i] = _pools[_componentIDsBuffer[i]].GetRaw(entityID);
            }
            return new ReadOnlySpan<object>(_componentsBuffer, 0, count);
        }
        public void GetComponentsFor(int entityID, List<object> list)
        {
            list.Clear();
            int count = GetComponentTypeIDsFor_Internal(entityID, ref _componentIDsBuffer);
            for (int i = 0; i < count; i++)
            {
                list.Add(_pools[_componentIDsBuffer[i]].GetRaw(entityID));
            }
        }
        public void GetComponentTypesFor(int entityID, HashSet<Type> typeSet)
        {
            typeSet.Clear();
            int count = GetComponentTypeIDsFor_Internal(entityID, ref _componentIDsBuffer);
            for (int i = 0; i < count; i++)
            {
                typeSet.Add(_pools[_componentIDsBuffer[i]].ComponentType);
            }
        }
        private unsafe int GetComponentTypeIDsFor_Internal(int entityID, ref int[] componentIDs)
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
            public EntitySlot(short gen, short componentsCount, bool isUsed)
            {
                this.gen = gen;
                this.componentsCount = componentsCount;
                this.isUsed = isUsed;
            }
        }
        #endregion

        #region DebuggerProxy
        protected partial class DebuggerProxy
        {
            private EcsWorld _world;
            private List<MaskQueryExecutor> _queries;
            public string Name { get { return _world.Name; } }
            public EntitySlotInfo[] Entities
            {
                get
                {
                    EntitySlotInfo[] result = new EntitySlotInfo[_world.Count];
                    int i = 0;
                    using (_world.DisableAutoReleaseDelEntBuffer())
                    {
                        foreach (var e in _world.ToSpan())
                        {
                            result[i++] = _world.GetEntitySlotInfoDebug(e);
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
    public static class IntExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static entlong ToEntityLong(this int self, EcsWorld world)
        {
            return world.GetEntityLong(self);
        }
    }
    #endregion
}