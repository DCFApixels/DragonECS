using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public abstract partial class EcsWorld
    {
        public readonly short id;
        private IEcsWorldConfig _config;

        private bool _isDestroyed;

        private IntDispenser _entityDispenser;
        private int _entitiesCount;
        private int _entitesCapacity;
        private short[] _gens; //старший бит указывает на то жива ли сущность
        private short[] _componentCounts;
        private EcsGroup _allEntites;

        private int[] _delEntBuffer;
        private int _delEntBufferCount;
        private int _delEntBufferMinCount;
        private int _freeSpace;
        private bool _isEnableAutoReleaseDelEntBuffer = true;

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
            //_denseEntities.Length;
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

        public EcsReadonlyGroup Entities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_isEnableAutoReleaseDelEntBuffer)
                {
                    ReleaseDelEntityBufferAll();
                }
                return _allEntites.Readonly;
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
        public EcsWorld(IEcsWorldConfig config) : this(config, true) { }
        private EcsWorld(IEcsWorldConfig config, bool isIndexable)
        {
            if (config == null)
            {
                config = EmptyConfig.Instance;
            }
            _config = config;

            if (isIndexable)
            {
                id = (short)_worldIdDispenser.UseFree();
                if (id >= Worlds.Length)
                {
                    Array.Resize(ref Worlds, Worlds.Length << 1);
                }
                Worlds[id] = this;
            }

            _poolsMediator = new PoolsMediator(this);
            _entityDispenser = new IntDispenser(0);

            int poolsCapacity = ArrayUtility.NormalizeSizeToPowerOfTwo(config.Get_PoolsCapacity());
            _pools = new IEcsPoolImplementation[poolsCapacity];
            _poolComponentCounts = new int[poolsCapacity];
            ArrayUtility.Fill(_pools, _nullPool);

            _entitesCapacity = ArrayUtility.NormalizeSizeToPowerOfTwo(config.Get_EntitiesCapacity());
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

            _delEntBufferMinCount = Math.Max(_delEntBuffer.Length >> DEL_ENT_BUFFER_SIZE_OFFSET, DEL_ENT_BUFFER_MIN_SIZE);

            _allEntites = GetFreeGroup();
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
            _poolIds = null;
            _componentIds = null;
        }
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

        #region Where Query
        public EcsReadonlyGroup WhereToGroupFor<TAspect>(EcsSpan span, out TAspect aspect) where TAspect : EcsAspect
        {
            if (_isEnableAutoReleaseDelEntBuffer)
            {
                ReleaseDelEntityBufferAll();
            }
            var executor = GetExecutor<EcsWhereExecutor<TAspect>>();
            aspect = executor.Aspect;
            return executor.ExecuteFor(span);
        }
        public EcsReadonlyGroup WhereToGroupFor<TAspect>(EcsSpan span) where TAspect : EcsAspect
        {
            if (_isEnableAutoReleaseDelEntBuffer)
            {
                ReleaseDelEntityBufferAll();
            }
            return GetExecutor<EcsWhereExecutor<TAspect>>().ExecuteFor(span);
        }
        public EcsReadonlyGroup WhereToGroup<TAspect>(out TAspect aspect) where TAspect : EcsAspect
        {
            if (_isEnableAutoReleaseDelEntBuffer)
            {
                ReleaseDelEntityBufferAll();
            }
            var executor = GetExecutor<EcsWhereExecutor<TAspect>>();
            aspect = executor.Aspect;
            return executor.Execute();
        }
        public EcsReadonlyGroup WhereToGroup<TAspect>() where TAspect : EcsAspect
        {
            if (_isEnableAutoReleaseDelEntBuffer)
            {
                ReleaseDelEntityBufferAll();
            }
            return GetExecutor<EcsWhereExecutor<TAspect>>().Execute();
        }

        public EcsSpan WhereFor<TAspect>(EcsSpan span, out TAspect aspect) where TAspect : EcsAspect
        {
            if (_isEnableAutoReleaseDelEntBuffer)
            {
                ReleaseDelEntityBufferAll();
            }
            var executor = GetExecutor<EcsWhereSpanExecutor<TAspect>>();
            aspect = executor.Aspect;
            return executor.ExecuteFor(span);
        }
        public EcsSpan WhereFor<TAspect>(EcsSpan span) where TAspect : EcsAspect
        {
            if (_isEnableAutoReleaseDelEntBuffer)
            {
                ReleaseDelEntityBufferAll();
            }
            return GetExecutor<EcsWhereSpanExecutor<TAspect>>().ExecuteFor(span);
        }
        public EcsSpan Where<TAspect>(out TAspect aspect) where TAspect : EcsAspect
        {
            if (_isEnableAutoReleaseDelEntBuffer)
            {
                ReleaseDelEntityBufferAll();
            }
            var executor = GetExecutor<EcsWhereSpanExecutor<TAspect>>();
            aspect = executor.Aspect;
            return executor.Execute();
        }
        public EcsSpan Where<TAspect>() where TAspect : EcsAspect
        {
            if (_isEnableAutoReleaseDelEntBuffer)
            {
                ReleaseDelEntityBufferAll();
            }
            return GetExecutor<EcsWhereSpanExecutor<TAspect>>().Execute();
        }
        #endregion

        #region Entity
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NewEntity()
        {
            int entityID = _entityDispenser.GetFree();
            _freeSpace--;
            _entitiesCount++;

            if (_gens.Length <= entityID)
            {
                Upsize_Internal(_gens.Length << 1);
            }

            _gens[entityID] &= GEN_BITS;
            _allEntites.AddUnchecked(entityID);
            _entityListeners.InvokeOnNewEntity(entityID);
            return entityID;
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
        public void DelEntity(int entityID)
        {
#if DEBUG
            if(IsUsed(entityID) == false)
            {
                Throw.UndefinedException();
            }
#endif
            _allEntites.Remove(entityID);
            _delEntBuffer[_delEntBufferCount++] = entityID;
            _gens[entityID] |= DEATH_GEN_BIT;
            _entitiesCount--;
            _entityListeners.InvokeOnDelEntity(entityID);
            //if (_delEntBufferCount >= _delEntBuffer.Length)
            //    ReleaseDelEntityBufferAll();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe entlong GetEntityLong(int entityID)
        {
            long x = (long)id << 48 | (long)_gens[entityID] << 32 | (long)entityID;
            return *(entlong*)&x;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(int entityID, short gen) => _gens[entityID] == gen;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUsed(int entityID) => _gens[entityID] >= 0;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetGen(int entityID) => _gens[entityID];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetComponentsCount(int entityID) => _componentCounts[entityID];

        public bool IsMatchesMask(EcsMask mask, int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (mask.worldID != id)
                throw new EcsFrameworkException("The types of the target world of the mask and this world are different.");
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
            foreach (var e in _allEntites)
            {
                if (_componentCounts[e] <= 0)
                {
                    DelEntity(e);
                }
            }
        }

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
            ReleaseDelEntityBuffer(-1);
        }
        public void ReleaseDelEntityBuffer(int count)
        {
            if (_delEntBufferCount <= 0)
            {
                return;
            }

            if (count < 0)
            {
                count = _delEntBufferCount;
            }
            else if (count > _delEntBufferCount)
            {
                count = _delEntBufferCount;
            }
            _delEntBufferCount -= count;
            ReadOnlySpan<int> buffser = new ReadOnlySpan<int>(_delEntBuffer, _delEntBufferCount, count);
            for (int i = 0; i < _poolsCount; i++)
            {
                _pools[i].OnReleaseDelEntityBuffer(buffser);
            }
            _listeners.InvokeOnReleaseDelEntityBuffer(buffser);
            for (int i = 0; i < buffser.Length; i++)
            {
                _entityDispenser.Release(buffser[i]);
            }
            _freeSpace += count;// _entitesCapacity - _entitiesCount;
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

            _delEntBufferMinCount = Math.Max(_delEntBuffer.Length >> DEL_ENT_BUFFER_SIZE_OFFSET, DEL_ENT_BUFFER_MIN_SIZE);
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

        #region EmptyConfig
        private class EmptyConfig : IEcsWorldConfig
        {
            public static readonly EmptyConfig Instance = new EmptyConfig();
            private EmptyConfig() { }
            public bool IsLocked => true;
            public T Get<T>(string valueName) { return default; }
            public bool Has(string valueName) { return false; }
            public void Lock() { }
            public void Remove(string valueName) { }
            public void Set<T>(string valueName, T value) { }
            public bool TryGet<T>(string valueName, out T value) { value = default; return false; }
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