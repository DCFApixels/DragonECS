﻿using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.PoolsCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    public class EcsWorldConfig
    {
        public static readonly EcsWorldConfig Default = new EcsWorldConfig();
        public readonly int EntitiesCapacity;
        public readonly int GroupCapacity;
        public readonly int PoolsCapacity;
        public readonly int PoolComponentsCapacity;
        public readonly int PoolRecycledComponentsCapacity;
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
    [MetaDescription(EcsConsts.AUTHOR, "It is a container for entities and components.")]
    [MetaID("AEF3557C92019C976FC48F90E95A9DA6")]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public partial class EcsWorld : IEntityStorage, IEcsMember
    {
        public readonly short ID;
        private IConfigContainer _configs;

        private bool _isDestroyed = false;

        private IdDispenser _entityDispenser;
        private int _entitiesCount = 0;
        private int _entitiesCapacity = 0;
        private EntitySlot[] _entities = Array.Empty<EntitySlot>();

        private int[] _delEntBuffer = Array.Empty<int>();
        private int _delEntBufferCount = 0;
        private bool _isEnableAutoReleaseDelEntBuffer = true;

        internal int _entityComponentMaskLength;
        internal int _entityComponentMaskLengthBitShift;
        internal int[] _entityComponentMasks = Array.Empty<int>();
        private const int COMPONENT_MASK_CHUNK_SIZE = 32;

        //"лениво" обновляется только для NewEntity
        private long _deleteLeakedEntitesLastVersion = 0;
        //обновляется в NewEntity и в DelEntity
        private long _version = 0;

        private List<WeakReference<EcsGroup>> _groups = new List<WeakReference<EcsGroup>>();
        private Stack<EcsGroup> _groupsPool = new Stack<EcsGroup>(64);

        private List<IEcsWorldEventListener> _listeners = new List<IEcsWorldEventListener>();
        private List<IEcsEntityEventListener> _entityListeners = new List<IEcsEntityEventListener>();

        #region Properties
        EcsWorld IEntityStorage.World
        {
            get { return this; }
        }
        public IConfigContainer Configs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _configs; }
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
                return _entityDispenser.UsedToEcsSpan(ID);
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
        public EcsWorld(EcsWorldConfig config, short worldID = -1) : this(config == null ? ConfigContainer.Empty : new ConfigContainer().Set(config), worldID) { }
        public EcsWorld(IConfigContainer configs = null, short worldID = -1)
        {
            lock (_worldLock)
            {
                if (configs == null) { configs = ConfigContainer.Empty; }
                bool nullWorld = this is NullWorld;
                if (nullWorld == false && worldID == NULL_WORLD_ID)
                {
                    EcsDebug.PrintWarning($"The world identifier cannot be {NULL_WORLD_ID}");
                }
                _configs = configs;
                EcsWorldConfig config = configs.GetWorldConfigOrDefault();

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

                int poolsCapacity = ArrayUtility.NormalizeSizeToPowerOfTwo(config.PoolsCapacity);
                _pools = new IEcsPoolImplementation[poolsCapacity];
                _poolSlots = new PoolSlot[poolsCapacity];
                ArrayUtility.Fill(_pools, _nullPool);

                int entitiesCapacity = ArrayUtility.NormalizeSizeToPowerOfTwo(config.EntitiesCapacity);
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
#if (DEBUG && !DISABLE_DEBUG)
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
        public TAspect GetAspect<TAspect>() where TAspect : EcsAspect, new()
        {
            return Get<AspectCache<TAspect>>().Instance;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetQueryCache<TExecutor, TAspect>(out TExecutor executor, out TAspect aspect)
            where TExecutor : MaskQueryExecutor, new()
            where TAspect : EcsAspect, new()
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
        public ref T GetUnchecked<T>() where T : struct
        {
            return ref WorldComponentPool<T>.GetForWorldUnchecked(ID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Get<T>(int worldID) where T : struct
        {
            return ref WorldComponentPool<T>.GetForWorld(worldID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetUnchecked<T>(int worldID) where T : struct
        {
            return ref WorldComponentPool<T>.GetForWorldUnchecked(worldID);
        }
        #endregion

        #region Entity

        #region New/Del
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
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (IsUsed(entityID)) { Throw.World_EntityIsAlreadyСontained(entityID); }
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
        }

        public entlong NewEntityLong()
        {
            int entityID = NewEntity();
            return GetEntityLong(entityID);
        }
        public entlong NewEntityLong(int entityID)
        {
            NewEntity(entityID);
            return GetEntityLong(entityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryDelEntity(int entityID)
        {
            if (IsUsed(entityID))
            {
                DelEntity(entityID);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DelEntity(entlong entity)
        {
            DelEntity(entity.ID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DelEntity(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (IsUsed(entityID) == false) { Throw.World_EntityIsNotContained(entityID); }
#endif
            UpVersion();
            _delEntBuffer[_delEntBufferCount++] = entityID;
            _entities[entityID].isUsed = false;
            _entitiesCount--;
            _entityListeners.InvokeOnDelEntity(entityID);
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
        public unsafe EntitySlotInfo GetEntitySlotInfoDebug(int entityID)
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
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (mask.WorldID != ID) { Throw.World_MaskDoesntBelongWorld(); }
#endif
            for (int i = 0, iMax = mask._incs.Length; i < iMax; i++)
            {
                if (!_pools[mask._incs[i]].Has(entityID))
                {
                    return false;
                }
            }
            for (int i = 0, iMax = mask._excs.Length; i < iMax; i++)
            {
                if (_pools[mask._excs[i]].Has(entityID))
                {
                    return false;
                }
            }
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
            ReleaseDelEntityBuffer(_delEntBufferCount);
        }
        public unsafe void ReleaseDelEntityBuffer(int count)
        {
            if (_delEntBufferCount <= 0)
            {
                return;
            }
            unchecked { _version++; }
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

            ReadOnlySpan<int> buffer = new ReadOnlySpan<int>(_delEntBuffer, _delEntBufferCount, count);
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
                        group.OnReleaseDelEntityBuffer_Internal(buffer);
                    }
                }
                else
                {
                    RemoveGroupAt(i--);
                }
            }

            _listeners.InvokeOnReleaseDelEntityBuffer(buffer);
            for (int i = 0; i < buffer.Length; i++)
            {
                int e = buffer[i];
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

        #region Groups Pool
        private void RemoveGroupAt(int index)
        {
            int last = _groups.Count - 1;
            _groups[index] = _groups[last];
            _groups.RemoveAt(last);
        }
        internal void RegisterGroup(EcsGroup group)
        {
            _groups.Add(new WeakReference<EcsGroup>(group));
        }
        internal EcsGroup GetFreeGroup()
        {
            EcsGroup result = _groupsPool.Count <= 0 ? new EcsGroup(this, _configs.GetWorldConfigOrDefault().GroupCapacity) : _groupsPool.Pop();
            result._isReleased = false;
            return result;
        }
        internal void ReleaseGroup(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (group.World != this) { Throw.World_GroupDoesNotBelongWorld(); }
#endif
            group._isReleased = true;
            group.Clear();
            _groupsPool.Push(group);
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
            if (componentIDs == null)
            {
                componentIDs = new int[itemsCount];
            }
            if (componentIDs.Length < itemsCount)
            {
                Array.Resize(ref componentIDs, itemsCount);
            }

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
        private EcsSpan GetSpan_Debug()
        {
            return _entityDispenser.UsedToEcsSpan(ID);
        }
        protected class DebuggerProxy
        {
            private EcsWorld _world;
            private List<MaskQueryExecutor> _queries;
            public EntitySlotInfo[] Entities
            {
                get
                {
                    EntitySlotInfo[] result = new EntitySlotInfo[_world.Count];
                    int i = 0;
                    foreach (var e in _world.ToSpan())
                    {
                        result[i++] = _world.GetEntitySlotInfoDebug(e);
                    }
                    return result;
                }
            }
            public long Version { get { return _world.Version; } }
            public IEcsPool[] Pools { get { return _world._pools; } }
            public short ID { get { return _world.ID; } }
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
        public static void InvokeOnWorldResize(this List<IEcsWorldEventListener> self, int newSize)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++)
            {
                self[i].OnWorldResize(newSize);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnReleaseDelEntityBuffer(this List<IEcsWorldEventListener> self, ReadOnlySpan<int> buffer)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++)
            {
                self[i].OnReleaseDelEntityBuffer(buffer);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnWorldDestroy(this List<IEcsWorldEventListener> self)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++)
            {
                self[i].OnWorldDestroy();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnNewEntity(this List<IEcsEntityEventListener> self, int entityID)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++)
            {
                self[i].OnNewEntity(entityID);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnDelEntity(this List<IEcsEntityEventListener> self, int entityID)
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