using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public abstract partial class EcsWorld : IEntityStorage
    {
        public readonly short id;
        private IEcsWorldConfig _config;

        private bool _isDestroyed;

        private IdDispenser _entityDispenser;
        private int _entitiesCount;
        private int _entitesCapacity;
        private short[] _gens; //старший бит указывает на то жива ли сущность
        private short[] _componentCounts;

        private int[] _delEntBuffer;
        private int _delEntBufferCount;
        private bool _isEnableAutoReleaseDelEntBuffer = true;

        private long _version = 0;

        private List<WeakReference<EcsGroup>> _groups = new List<WeakReference<EcsGroup>>();
        private Stack<EcsGroup> _groupsPool = new Stack<EcsGroup>(64);

        private List<IEcsWorldEventListener> _listeners = new List<IEcsWorldEventListener>();
        private List<IEcsEntityEventListener> _entityListeners = new List<IEcsEntityEventListener>();

        internal int[][] _entitiesComponentMasks;

        private readonly PoolsMediator _poolsMediator;

        #region Properties
        public IEcsWorldConfig Config
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _config; }
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
            get { return _entitesCapacity; }
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
                if (_isEnableAutoReleaseDelEntBuffer)
                {
                    ReleaseDelEntityBufferAll();
                }
                return _entityDispenser.UsedToEcsSpan(id);
            }
        }
        public ReadOnlySpan<IEcsPoolImplementation> AllPools
        {
            // new ReadOnlySpan<IEcsPoolImplementation>(pools, 0, _poolsCount);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _pools; }
        }
        #endregion

        #region Constructors/Destroy
        public EcsWorld(IEcsWorldConfig config = null, short worldID = -1)
        {
            if (config == null)
            {
                config = EcsWorldConfig.Empty;
            }
            _config = config;

            if (worldID < 0)
            {
                worldID = (short)_worldIdDispenser.UseFree();
                if (worldID >= Worlds.Length)
                {
                    Array.Resize(ref Worlds, Worlds.Length << 1);
                }
            }
            else
            {
                if (Worlds[worldID] != null)
                {
                    Throw.UndefinedException();
                }
            }
            id = worldID;
            Worlds[worldID] = this;

            _poolsMediator = new PoolsMediator(this);

            int poolsCapacity = ArrayUtility.NormalizeSizeToPowerOfTwo(config.Get_PoolsCapacity());
            _pools = new IEcsPoolImplementation[poolsCapacity];
            _poolComponentCounts = new int[poolsCapacity];
            ArrayUtility.Fill(_pools, _nullPool);

            _entitesCapacity = ArrayUtility.NormalizeSizeToPowerOfTwo(config.Get_EntitiesCapacity());
            _entityDispenser = new IdDispenser(_entitesCapacity, 0);
            _gens = new short[_entitesCapacity];
            _componentCounts = new short[_entitesCapacity];

            ArrayUtility.Fill(_gens, DEATH_GEN_BIT);
            _delEntBufferCount = 0;
            _delEntBuffer = new int[_entitesCapacity];
            _entitiesComponentMasks = new int[_entitesCapacity][];

            int maskLength = _pools.Length / 32 + 1;
            for (int i = 0; i < _entitesCapacity; i++)
            {
                _entitiesComponentMasks[i] = new int[maskLength];
            }
        }
        public void Destroy()
        {
            _entityDispenser = null;
            _gens = null;
            _pools = null;
            _nullPool = null;
            Worlds[id] = null;
            ReleaseData(id);
            _worldIdDispenser.Release(id);
            _isDestroyed = true;
            _poolTypeCode_2_CmpTypeIDs = null;
            _componentTypeCode_2_CmpTypeIDs = null;
        }
        //public void Clear() { }
        #endregion

        #region Getters
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.Preserve]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TAspect GetAspect<TAspect>() where TAspect : EcsAspect
        {
            return Get<AspectCache<TAspect>>().instance;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TExecutor GetExecutor<TExecutor>() where TExecutor : EcsQueryExecutor, new()
        {
            return Get<ExcecutorCache<TExecutor>>().instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Get<T>() where T : struct
        {
            return ref WorldComponentPool<T>.GetForWorld(id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetUnchecked<T>() where T : struct
        {
            return ref WorldComponentPool<T>.GetForWorldUnchecked(id);
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
            unchecked { _version++; }
            int entityID = _entityDispenser.UseFree();
            _entitiesCount++;

            if (_entitesCapacity <= entityID)
            {
                Upsize_Internal(_gens.Length << 1);
            }

            _gens[entityID] &= GEN_MASK;
            _entityListeners.InvokeOnNewEntity(entityID);
            return entityID;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void NewEntity(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (IsUsed(entityID))
            {
                Throw.World_EntityIsAlreadyСontained(entityID);
            }
#endif
            unchecked { _version++; }
            _entityDispenser.Use(entityID);
            _entitiesCount++;

            if (_entitesCapacity <= entityID)
            {
                Upsize_Internal(_gens.Length << 1);
            }

            _gens[entityID] &= GEN_MASK;
            _entityListeners.InvokeOnNewEntity(entityID);
        }

        public entlong NewEntityLong()
        {
            int e = NewEntity();
            return GetEntityLong(e);
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
        public void DelEntity(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (IsUsed(entityID) == false)
            {
                Throw.World_EntityIsNotContained(entityID);
            }
#endif
            _delEntBuffer[_delEntBufferCount++] = entityID;
            _gens[entityID] |= DEATH_GEN_BIT;
            _entitiesCount--;
            _entityListeners.InvokeOnDelEntity(entityID);
        }
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ToSpan()
        {
            return _entityDispenser.UsedToEcsSpan(id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe entlong GetEntityLong(int entityID)
        {
            long x = (long)id << 48 | (long)_gens[entityID] << 32 | (long)entityID;
            return *(entlong*)&x;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(int entityID, short gen) { return _gens[entityID] == gen; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUsed(int entityID) { return _gens[entityID] >= 0; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetGen(int entityID) { return _gens[entityID]; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetComponentsCount(int entityID) { return _componentCounts[entityID]; }

        public bool IsMatchesMask(EcsMask mask, int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (mask.worldID != id) { Throw.World_MaskDoesntBelongWorld(); }
#endif
            for (int i = 0, iMax = mask.incChunckMasks.Length; i < iMax; i++)
            {
                if (!_pools[mask.inc[i]].Has(entityID))
                    return false;
            }
            for (int i = 0, iMax = mask.excChunckMasks.Length; i < iMax; i++)
            {
                if (_pools[mask.exc[i]].Has(entityID))
                    return false;
            }
            return true;
        }

        public void DeleteEmptyEntites()
        {
            foreach (var e in Entities)
            {
                if (_componentCounts[e] <= 0)
                {
                    DelEntity(e);
                }
            }
        }
        //public void Densify()
        //{
        //    _entityDispenser.Sort();
        //}
        #endregion

        #region Copy/Clone
        public void CopyEntity(int fromEntityID, int toEntityID)
        {
            foreach (var pool in _pools)
            {
                if (pool.Has(fromEntityID))
                    pool.Copy(fromEntityID, toEntityID);
            }
        }
        public void CopyEntity(int fromEntityID, EcsWorld toWorld, int toEntityID)
        {
            foreach (var pool in _pools)
            {
                if (pool.Has(fromEntityID))
                    pool.Copy(fromEntityID, toWorld, toEntityID);
            }
        }
        public int CloneEntity(int fromEntityID)
        {
            int newEntity = NewEntity();
            CopyEntity(fromEntityID, newEntity);
            return newEntity;
        }
        public int CloneEntity(int fromEntityID, EcsWorld toWorld)
        {
            int newEntity = NewEntity();
            CopyEntity(fromEntityID, toWorld, newEntity);
            return newEntity;
        }
        public void CloneEntity(int fromEntityID, int toEntityID)
        {
            CopyEntity(fromEntityID, toEntityID);
            foreach (var pool in _pools)
            {
                if (!pool.Has(fromEntityID) && pool.Has(toEntityID))
                    pool.Del(toEntityID);
            }
        }
        //public void CloneEntity(int fromEntityID, EcsWorld toWorld, int toEntityID)
        #endregion

        #endregion

        #region DelEntBuffer
        public AutoReleaseDelEntBufferLonkUnloker DisableAutoReleaseDelEntBuffer()
        {
            _isEnableAutoReleaseDelEntBuffer = false;
            return new AutoReleaseDelEntBufferLonkUnloker(this);
        }
        public void EnableAutoReleaseDelEntBuffer()
        {
            _isEnableAutoReleaseDelEntBuffer = true;
        }
        public readonly struct AutoReleaseDelEntBufferLonkUnloker : IDisposable
        {
            private readonly EcsWorld _source;
            public AutoReleaseDelEntBufferLonkUnloker(EcsWorld source)
            {
                _source = source;
            }
            public void Dispose()
            {
                _source.EnableAutoReleaseDelEntBuffer();
            }
        }
        public void ReleaseDelEntityBufferAll()
        {
            ReleaseDelEntityBuffer(_delEntBufferCount);
        }
        public void ReleaseDelEntityBuffer(int count)
        {
            if (_delEntBufferCount <= 0)
            {
                return;
            }
            unchecked { _version++; }
            count = Math.Clamp(count, 0, _delEntBufferCount);
            _delEntBufferCount -= count;
            ReadOnlySpan<int> buffser = new ReadOnlySpan<int>(_delEntBuffer, _delEntBufferCount, count);
            for (int i = 0; i < _poolsCount; i++)
            {
                _pools[i].OnReleaseDelEntityBuffer(buffser);
            }
            _listeners.InvokeOnReleaseDelEntityBuffer(buffser);
            for (int i = 0; i < buffser.Length; i++)
            {
                int e = buffser[i];
                _entityDispenser.Release(e);
                unchecked { _gens[e]++; }//up gen
                _gens[e] |= DEATH_GEN_BIT;
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
            Upsize_Internal(ArrayUtility.NormalizeSizeToPowerOfTwo(minSize));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Upsize_Internal(int newSize)
        {
            Array.Resize(ref _gens, newSize);
            Array.Resize(ref _componentCounts, newSize);
            Array.Resize(ref _delEntBuffer, newSize);
            Array.Resize(ref _entitiesComponentMasks, newSize);
            for (int i = _entitesCapacity; i < newSize; i++)
                _entitiesComponentMasks[i] = new int[_pools.Length / 32 + 1];

            ArrayUtility.Fill(_gens, DEATH_GEN_BIT, _entitesCapacity);
            _entitesCapacity = newSize;

            for (int i = 0; i < _groups.Count; i++)
            {
                if (_groups[i].TryGetTarget(out EcsGroup group))
                {
                    group.OnWorldResize(newSize);
                }
                else
                {
                    int last = _groups.Count - 1;
                    _groups[i--] = _groups[last];
                    _groups.RemoveAt(last);
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
        internal void RegisterGroup(EcsGroup group)
        {
            _groups.Add(new WeakReference<EcsGroup>(group));
        }
        internal EcsGroup GetFreeGroup()
        {
            EcsGroup result = _groupsPool.Count <= 0 ? new EcsGroup(this) : _groupsPool.Pop();
            result._isReleased = false;
            return result;
        }
        internal void ReleaseGroup(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (group.World != this) Throw.World_GroupDoesNotBelongWorld();
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

        #region Debug
        public void GetComponents(int entityID, List<object> list)
        {
            list.Clear();
            var itemsCount = GetComponentsCount(entityID);
            for (var i = 0; i < _poolsCount; i++)
            {
                if (_pools[i].Has(entityID))
                {
                    itemsCount--;
                    list.Add(_pools[i].GetRaw(entityID));
                    if (itemsCount <= 0)
                        break;
                }
            }
        }
        public void GetComponentTypes(int entityID, HashSet<Type> typeSet)
        {
            typeSet.Clear();
            var itemsCount = GetComponentsCount(entityID);
            for (var i = 0; i < _poolsCount; i++)
            {
                if (_pools[i].Has(entityID))
                {
                    itemsCount--;
                    typeSet.Add(_pools[i].ComponentType);
                    if (itemsCount <= 0)
                        break;
                }
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
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnWorldResize(newSize);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnReleaseDelEntityBuffer(this List<IEcsWorldEventListener> self, ReadOnlySpan<int> buffer)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnReleaseDelEntityBuffer(buffer);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnWorldDestroy(this List<IEcsWorldEventListener> self)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnWorldDestroy();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnNewEntity(this List<IEcsEntityEventListener> self, int entityID)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnNewEntity(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnDelEntity(this List<IEcsEntityEventListener> self, int entityID)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnDelEntity(entityID);
        }
    }
    #endregion

    #region Extensions
    public static class IntExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static entlong ToEntityLong(this int self, EcsWorld world) => world.GetEntityLong(self);
    }
    #endregion
}