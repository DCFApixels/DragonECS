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

        private bool _isDestroyed;

        private IntDispenser _entityDispenser;
        private int _entitiesCount;
        private int _entitesCapacity;
        private short[] _gens; //старший бит указывает на то жива ли сущность
        private short[] _componentCounts;
        private EcsGroup _allEntites;

        private int[] _delEntBuffer;
        private int _delEntBufferCount;

        private List<WeakReference<EcsGroup>> _groups = new List<WeakReference<EcsGroup>>();
        private Stack<EcsGroup> _groupsPool = new Stack<EcsGroup>(64);

        private List<IEcsWorldEventListener> _listeners = new List<IEcsWorldEventListener>();
        private List<IEcsEntityEventListener> _entityListeners = new List<IEcsEntityEventListener>();

        #region Properties
        public bool IsDestroyed => _isDestroyed;
        public int Count => _entitiesCount;
        public int Capacity => _entitesCapacity; //_denseEntities.Length;
        public EcsReadonlyGroup Entities => _allEntites.Readonly;
        public ReadOnlySpan<IEcsPoolImplementation> AllPools => _pools;// new ReadOnlySpan<IEcsPoolImplementation>(pools, 0, _poolsCount);
        #endregion

        #region Constructors/Destroy
        public EcsWorld() : this(true) { }
        internal EcsWorld(bool isIndexable)
        {
            _entitesCapacity = 512;

            if (isIndexable)
            {
                id = (short)_worldIdDispenser.UseFree();
                if (id >= Worlds.Length)
                    Array.Resize(ref Worlds, Worlds.Length << 1);
                Worlds[id] = this;
            }

            _entityDispenser = new IntDispenser(0);
            _pools = new IEcsPoolImplementation[512];
            ArrayUtility.Fill(_pools, _nullPool);

            _gens = new short[_entitesCapacity];
            _componentCounts = new short[_entitesCapacity];

            ArrayUtility.Fill(_gens, DEATH_GEN_BIT);
            _delEntBufferCount = 0;
            _delEntBuffer = new int[_entitesCapacity >> DEL_ENT_BUFFER_SIZE_OFFSET];

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
        public ref T Get<T>() where T : struct => ref WorldComponentPool<T>.GetForWorld(id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T UncheckedGet<T>() where T : struct => ref WorldComponentPool<T>.UncheckedGetForWorld(id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Get<T>(int worldID) where T : struct => ref WorldComponentPool<T>.GetForWorld(worldID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T UncheckedGet<T>(int worldID) where T : struct => ref WorldComponentPool<T>.UncheckedGetForWorld(worldID);
        #endregion

        #region Where Query
        public EcsReadonlyGroup WhereFor<TAspect>(EcsReadonlyGroup sourceGroup, out TAspect aspect) where TAspect : EcsAspect
        {
            var executor = GetExecutor<EcsWhereExecutor<TAspect>>();
            aspect = executor.Aspect;
            return executor.ExecuteFor(sourceGroup);
        }
        public EcsReadonlyGroup WhereFor<TAspect>(EcsReadonlyGroup sourceGroup) where TAspect : EcsAspect
        {
            return GetExecutor<EcsWhereExecutor<TAspect>>().ExecuteFor(sourceGroup);
        }
        public EcsReadonlyGroup Where<TAspect>(out TAspect aspect) where TAspect : EcsAspect
        {
            var executor = GetExecutor<EcsWhereExecutor<TAspect>>();
            aspect = executor.Aspect;
            return executor.Execute();
        }
        public EcsReadonlyGroup Where<TAspect>() where TAspect : EcsAspect
        {
            return GetExecutor<EcsWhereExecutor<TAspect>>().Execute();
        }
        #endregion

        #region Entity
        public int NewEmptyEntity()
        {
            int entityID = _entityDispenser.GetFree();
            _entitiesCount++;

            if (_gens.Length <= entityID)
            {
                Array.Resize(ref _gens, _gens.Length << 1);
                Array.Resize(ref _componentCounts, _gens.Length);
                ArrayUtility.Fill(_gens, DEATH_GEN_BIT, _entitesCapacity);
                _entitesCapacity = _gens.Length;

                for (int i = 0; i < _groups.Count; i++)
                {
                    if (_groups[i].TryGetTarget(out EcsGroup group))
                    {
                        group.OnWorldResize(_gens.Length);
                    }
                    else
                    {
                        int last = _groups.Count - 1;
                        _groups[i--] = _groups[last];
                        _groups.RemoveAt(last);
                    }
                }
                foreach (var item in _pools)
                    item.OnWorldResize(_gens.Length);

                _listeners.InvokeOnWorldResize(_gens.Length);
            }
            _gens[entityID] &= GEN_BITS;
            _allEntites.Add(entityID);
            _entityListeners.InvokeOnNewEntity(entityID);
            return entityID;
        }
        public entlong NewEmptyEntityLong()
        {
            int e = NewEmptyEntity();
            return GetEntityLong(e);
        }
        public void DelEntity(int entityID)
        {
            _allEntites.Remove(entityID);
            _delEntBuffer[_delEntBufferCount++] = entityID;
            _gens[entityID] |= DEATH_GEN_BIT;
            _entitiesCount--;
            _entityListeners.InvokeOnDelEntity(entityID);

            if (_delEntBufferCount >= _delEntBuffer.Length)
                ReleaseDelEntityBuffer();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe entlong GetEntityLong(int entityID)
        {
            long x = ((long)id << 48 | (long)_gens[entityID] << 32  | entityID);
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
            for (int i = 0, iMax = mask.inc.Length; i < iMax; i++)
            {
                if (!_pools[mask.inc[i]].Has(entityID))
                    return false;
            }
            for (int i = 0, iMax = mask.exc.Length; i < iMax; i++)
            {
                if (_pools[mask.exc[i]].Has(entityID))
                    return false;
            }
            return true;
        }
        public void ReleaseDelEntityBuffer()
        {
            ReadOnlySpan<int> buffser = new ReadOnlySpan<int>(_delEntBuffer, 0, _delEntBufferCount);
            foreach (var pool in _pools)
                pool.OnReleaseDelEntityBuffer(buffser);
            _listeners.InvokeOnReleaseDelEntityBuffer(buffser);
            for (int i = 0; i < _delEntBufferCount; i++)
                _entityDispenser.Release(_delEntBuffer[i]);
            _delEntBufferCount = 0;
        }
        public void DeleteEmptyEntites()
        {
            foreach (var e in _allEntites)
            {
                if (_componentCounts[e] <= 0) DelEntity(e);
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
            int newEntity = NewEmptyEntity();
            CopyEntity(fromEntityID, newEntity);
            return newEntity;
        }
        public int CloneEntity(int fromEntityID, EcsWorld toWorld)
        {
            int newEntity = NewEmptyEntity();
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

        #region Components Increment
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void IncrementEntityComponentCount(int entityID) => _componentCounts[entityID]++;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DecrementEntityComponentCount(int entityID)
        {
            var count = --_componentCounts[entityID];
            if (count == 0 && _allEntites.Has(entityID)) DelEntity(entityID);
#if (DEBUG && !DISABLE_DEBUG) || !DISABLE_DRAGONECS_ASSERT_CHEKS
            if (count < 0) Throw.World_InvalidIncrementComponentsBalance();
#endif
        }
        #endregion

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

            for (var i = 0; i < _pools.Length; i++)
            {
                if (_pools[i].Has(entityID))
                    list.Add(_pools[i].GetRaw(entityID));
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
