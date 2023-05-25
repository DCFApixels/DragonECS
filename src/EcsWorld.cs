using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DCFApixels.DragonECS.Internal;

namespace DCFApixels.DragonECS
{
    public abstract class EcsWorld
    {
        private const short GEN_BITS = 0x7fff;
        private const short DEATH_GEN_BIT = short.MinValue;
        private const int DEL_ENT_BUFFER_SIZE_OFFSET = 2;

        public static EcsWorld[] Worlds = new EcsWorld[8];
        private static IntDispenser _worldIdDispenser = new IntDispenser(0);

        public readonly short uniqueID;

        private int _worldTypeID;

        private IntDispenser _entityDispenser;
        private int _entitiesCount;
        private int _entitesCapacity;
        private short[] _gens; //старший бит указывает на то жива ли сущность
        private short[] _componentCounts;
        private EcsGroup _allEntites;

        //буфер удаления откладывает освобождение андишников сущностей.
        //Нужен для того чтобы запускать некоторые процесыы связанные с удалением сущности не по одному при каждом удалении, а пачкой
        //В теории такой подход частично улучшает ситуацию с переполнением поколений
        private int[] _delEntBuffer;
        private int _delEntBufferCount;

        internal IEcsPoolImplementation[] _pools;
        private EcsNullPool _nullPool;
        private int _poolsCount = 0;

        private EcsSubject[] _subjects;
        private EcsQueryExecutor[] _executors;

        private List<WeakReference<EcsGroup>> _groups;
        private Stack<EcsGroup> _groupsPool = new Stack<EcsGroup>(64);

        private List<IEcsWorldEventListener> _listeners;

        #region Properties
        public abstract Type Archetype { get; }
        public int UniqueID => uniqueID;
        public int Count => _entitiesCount;
        public int Capacity => _entitesCapacity; //_denseEntities.Length;
        public EcsReadonlyGroup Entities => _allEntites.Readonly;
        public ReadOnlySpan<IEcsPoolImplementation> AllPools => _pools;// new ReadOnlySpan<IEcsPoolImplementation>(pools, 0, _poolsCount);
        public int PoolsCount => _poolsCount;
        #endregion

        #region Constructors/Destroy
        static EcsWorld() 
        {
            EcsNullWorld nullWorld = new EcsNullWorld();
            Worlds[0] = nullWorld;
        }
        public EcsWorld() : this(true) { }
        internal EcsWorld(bool isIndexable)
        {
            _entitesCapacity = 512;

            _listeners = new List<IEcsWorldEventListener>();

            if (isIndexable)
            {
                uniqueID = (short)_worldIdDispenser.GetFree();
                if (uniqueID >= Worlds.Length)
                    Array.Resize(ref Worlds, Worlds.Length << 1);
                Worlds[uniqueID] = this;
            }

            _worldTypeID = WorldMetaStorage.GetWorldId(Archetype);

            _entityDispenser = new IntDispenser(0);
            _nullPool = EcsNullPool.instance;
            _pools = new IEcsPoolImplementation[512];
            ArrayUtility.Fill(_pools, _nullPool);

            _gens = new short[_entitesCapacity];
            _componentCounts = new short[_entitesCapacity];

            ArrayUtility.Fill(_gens, DEATH_GEN_BIT);
            _delEntBufferCount = 0;
            _delEntBuffer = new int[_entitesCapacity >> DEL_ENT_BUFFER_SIZE_OFFSET];

            _groups = new List<WeakReference<EcsGroup>>();
            _allEntites = GetGroupFromPool();

            _subjects = new EcsSubject[128];
            _executors = new EcsQueryExecutor[128];
        }

        public void Destroy()
        {
            _entityDispenser = null;
            //_denseEntities = null;
            _gens = null;
            _pools = null;
            _nullPool = null;
            _subjects = null;
            _executors = null;

            Worlds[uniqueID] = null;
            _worldIdDispenser.Release(uniqueID);
        }
        public void DestryWithPipeline()
        {
            Destroy();
        }
        #endregion

        #region GetComponentID
        public int GetComponentID<T>() => WorldMetaStorage.GetComponentId<T>(_worldTypeID);////ComponentType<TWorldArchetype>.uniqueID;

        #endregion

        #region GetPool
        public TPool GetPool<TComponent, TPool>() where TComponent : struct where TPool : IEcsPoolImplementation<TComponent>, new()
        {
            int uniqueID = WorldMetaStorage.GetComponentId<TComponent>(_worldTypeID);

            if (uniqueID >= _pools.Length)
            {
                int oldCapacity = _pools.Length;
                Array.Resize(ref _pools, _pools.Length << 1);
                ArrayUtility.Fill(_pools, _nullPool, oldCapacity, oldCapacity - _pools.Length);
            }

            if (_pools[uniqueID] == _nullPool)
            {
                var pool = new TPool();
                _pools[uniqueID] = pool;
                pool.OnInit(this, uniqueID);
                _poolsCount++;
                //EcsDebug.Print(pool.GetType().FullName);
            }

            return (TPool)_pools[uniqueID];
        }
        #endregion

        #region Queries
        public TSubject GetSubject<TSubject>() where TSubject : EcsSubject
        {
            int uniqueID = WorldMetaStorage.GetSubjectId<TSubject>(_worldTypeID);
            if (uniqueID >= _subjects.Length)
                Array.Resize(ref _subjects, _subjects.Length << 1);
            if (_subjects[uniqueID] == null)
                _subjects[uniqueID] = EcsSubject.Builder.Build<TSubject>(this);
            return (TSubject)_subjects[uniqueID];
        }
        #region Iterate
        public EcsSubjectIterator<TSubject> IterateFor<TSubject>(EcsReadonlyGroup sourceGroup, out TSubject subject) where TSubject : EcsSubject
        {

            subject = GetSubject<TSubject>();
            return subject.GetIteratorFor(sourceGroup);
        }
        public EcsSubjectIterator<TSubject> IterateFor<TSubject>(EcsReadonlyGroup sourceGroup) where TSubject : EcsSubject
        {
            return GetSubject<TSubject>().GetIteratorFor(sourceGroup);
        }
        public EcsSubjectIterator<TSubject> Iterate<TSubject>(out TSubject subject) where TSubject : EcsSubject
        {
            subject = GetSubject<TSubject>();
            return subject.GetIterator();
        }
        public EcsSubjectIterator<TSubject> Iterate<TSubject>() where TSubject : EcsSubject
        {
            return GetSubject<TSubject>().GetIterator();
        }
        #endregion

        #region Where
        private EcsWhereExecutor<TSubject> GetWhereExecutor<TSubject>() where TSubject : EcsSubject
        {
            int id = WorldMetaStorage.GetExecutorId<EcsWhereExecutor<TSubject>>(_worldTypeID);
            if (id >= _executors.Length)
                Array.Resize(ref _executors, _executors.Length << 1);
            if (_executors[id] == null)
                _executors[id] = new EcsWhereExecutor<TSubject>(GetSubject<TSubject>());
            return (EcsWhereExecutor<TSubject>)_executors[id];
        }
        public EcsWhereResult<TSubject> WhereFor<TSubject>(EcsReadonlyGroup sourceGroup, out TSubject subject) where TSubject : EcsSubject
        {
            var executor = GetWhereExecutor<TSubject>();
            subject = executor.Subject;
            return executor.ExecuteFor(sourceGroup);
        }
        public EcsWhereResult<TSubject> WhereFor<TSubject>(EcsReadonlyGroup sourceGroup) where TSubject : EcsSubject
        {
            return GetWhereExecutor<TSubject>().ExecuteFor(sourceGroup);
        }
        public EcsWhereResult<TSubject> Where<TSubject>(out TSubject subject) where TSubject : EcsSubject
        {
            var executor = GetWhereExecutor<TSubject>();
            subject = executor.Subject;
            return executor.Execute();
        }
        public EcsWhereResult<TSubject> Where<TSubject>() where TSubject : EcsSubject
        {
            return GetWhereExecutor<TSubject>().Execute();
        }
        #endregion

        #region Join
        private EcsJoinAttachExecutor<TSubject, TAttachComponent> GetJoinAttachExecutor<TSubject, TAttachComponent>()
            where TSubject : EcsSubject
            where TAttachComponent : struct, IEcsAttachComponent
        {
            int id = WorldMetaStorage.GetExecutorId<EcsJoinAttachExecutor<TSubject, TAttachComponent>>(_worldTypeID);
            if (id >= _executors.Length)
                Array.Resize(ref _executors, _executors.Length << 1);
            if (_executors[id] == null)
                _executors[id] = new EcsJoinAttachExecutor<TSubject, TAttachComponent>(GetSubject<TSubject>());
            return (EcsJoinAttachExecutor<TSubject, TAttachComponent>)_executors[id];
        }
        public EcsJoinAttachResult<TSubject, TAttachComponent> JoinFor<TSubject, TAttachComponent>(EcsReadonlyGroup sourceGroup, out TSubject subject)
            where TSubject : EcsSubject
            where TAttachComponent : struct, IEcsAttachComponent
        {
            var executor = GetJoinAttachExecutor<TSubject, TAttachComponent>();
            subject = executor.Subject;
            return executor.ExecuteFor(sourceGroup);
        }
        public EcsJoinAttachResult<TSubject, TAttachComponent> JoinFor<TSubject, TAttachComponent>(EcsReadonlyGroup sourceGroup)
            where TSubject : EcsSubject
            where TAttachComponent : struct, IEcsAttachComponent
        {
            return GetJoinAttachExecutor<TSubject, TAttachComponent>().ExecuteFor(sourceGroup);
        }
        public EcsJoinAttachResult<TSubject, TAttachComponent> Join<TSubject, TAttachComponent>(out TSubject subject)
            where TSubject : EcsSubject
            where TAttachComponent : struct, IEcsAttachComponent
        {
            var executor = GetJoinAttachExecutor<TSubject, TAttachComponent>();
            subject = executor.Subject;
            return executor.Execute();
        }
        public EcsJoinAttachResult<TSubject, TAttachComponent> Join<TSubject, TAttachComponent>()
            where TSubject : EcsSubject
            where TAttachComponent : struct, IEcsAttachComponent
        {
            return GetJoinAttachExecutor<TSubject, TAttachComponent>().Execute();
        }
        #endregion

        #endregion

        #region IsMatchesMask
        public bool IsMatchesMask(EcsMask mask, int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (mask._worldType != Archetype)
                throw new EcsFrameworkException("mask.WorldArchetypeType != typeof(TTableArhetype)");
#endif
            for (int i = 0, iMax = mask._inc.Length; i < iMax; i++)
            {
                if (!_pools[mask._inc[i]].Has(entityID))
                    return false;
            }
            for (int i = 0, iMax = mask._exc.Length; i < iMax; i++)
            {
                if (_pools[mask._exc[i]].Has(entityID))
                    return false;
            }
            return true;
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
            _listeners.InvokeOnNewEntity(entityID);
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
            _listeners.InvokeOnDelEntity(entityID);

            if (_delEntBufferCount >= _delEntBuffer.Length)
                ReleaseDelEntityBuffer();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong GetEntityLong(int entityID) => new entlong(entityID, _gens[entityID], uniqueID); //TODO придумать получше имя метода
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(int entityID, short gen) => _gens[entityID] == gen;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUsed(int entityID) => _gens[entityID] >= 0;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetGen(int entityID) => _gens[entityID];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetComponentsCount(int entityID) => _componentCounts[entityID];
        public void DeleteEmptyEntites()
        {
            foreach (var e in _allEntites)
            {
                if (_componentCounts[e] <= 0) DelEntity(e);
            }
        }

        public void CopyEntity(int fromEntityID, int toEntityID)
        {
            foreach (var pool in _pools)
            {
                if(pool.Has(fromEntityID)) pool.Copy(fromEntityID, toEntityID);
            }
        }
        public int CloneEntity(int fromEntityID)
        {
            int newEntity = NewEmptyEntity();
            CopyEntity(fromEntityID, newEntity);
            return newEntity;
        }
        public void CloneEntity(int fromEntityID, int toEntityID)
        {
            CopyEntity(fromEntityID, toEntityID);
            foreach (var pool in _pools)
            {
                if (!pool.Has(fromEntityID)&& pool.Has(toEntityID))
                        pool.Del(toEntityID);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void IncrementEntityComponentCount(int entityID) => _componentCounts[entityID]++;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DecrementEntityComponentCount(int entityID)
        {
            var count = --_componentCounts[entityID];
            if(count == 0 && _allEntites.Has(entityID)) DelEntity(entityID);
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (count < 0) throw new EcsFrameworkException("нарушен баланс инкремента/декремента компонентов");
#endif
        }
        #endregion

        #region Groups
        internal void RegisterGroup(EcsGroup group)
        {
            _groups.Add(new WeakReference<EcsGroup>(group));
        }
        internal EcsGroup GetGroupFromPool()
        {
            if (_groupsPool.Count <= 0) return new EcsGroup(this);
            return _groupsPool.Pop();
        }
        internal void ReleaseGroup(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (group.World != this)
                throw new ArgumentException("groupFilter.WorldIndex != this");
#endif
            group.Clear();
            _groupsPool.Push(group);
        }
        #endregion

        #region Debug
        public void GetComponents(int entityID, List<object> list)
        {
            list.Clear();
            var itemsCount = GetComponentsCount(entityID);
            if (itemsCount == 0) 
                return;

            for (var i = 0; i < _pools.Length; i++)
            {
                if (_pools[i].Has(entityID))
                list.Add(_pools[i].GetRaw(entityID));
                if (list.Count >= itemsCount)
                    break;
            }
        }

        //  public int GetComponents(int entity, ref object[] list)
        //  {
        //      var entityOffset = GetRawEntityOffset(entity);
        //      var itemsCount = _entities[entityOffset + RawEntityOffsets.ComponentsCount];
        //      if (itemsCount == 0) { return 0; }
        //      if (list == null || list.Length < itemsCount)
        //      {
        //          list = new object[_pools.Length];
        //      }
        //      var dataOffset = entityOffset + RawEntityOffsets.Components;
        //      for (var i = 0; i < itemsCount; i++)
        //      {
        //          list[i] = _pools[_entities[dataOffset + i]].GetRaw(entity);
        //      }
        //      return itemsCount;
        //  }
        
        //  public int GetComponentTypes(int entity, ref Type[] list)
        //  {
        //      var entityOffset = GetRawEntityOffset(entity);
        //      var itemsCount = _entities[entityOffset + RawEntityOffsets.ComponentsCount];
        //      if (itemsCount == 0) { return 0; }
        //      if (list == null || list.Length < itemsCount)
        //      {
        //          list = new Type[_pools.Length];
        //      }
        //      var dataOffset = entityOffset + RawEntityOffsets.Components;
        //      for (var i = 0; i < itemsCount; i++)
        //      {
        //          list[i] = _pools[_entities[dataOffset + i]].GetComponentType();
        //      }
        //      return itemsCount;
        //  }
        #endregion
    }

    public abstract class EcsWorld<TWorldArchetype> : EcsWorld
        where TWorldArchetype : EcsWorld<TWorldArchetype>
    {
        public override Type Archetype => typeof(TWorldArchetype);
        public EcsWorld() : base() { }
        internal EcsWorld(bool isIndexable) : base(isIndexable) { }
    }

    #region Utils
    public static class WorldMetaStorage
    {
        private static List<Resizer> _resizer = new List<Resizer>();
        private static int _tokenCount = 0;
        private static int[] _componentCounts = new int[0];
        private static int[] _subjectsCounts = new int[0];
        private static Dictionary<Type, int> _worldIds = new Dictionary<Type, int>();
        private static class WorldIndex<TWorldArchetype>
        {
            public static int id = GetWorldId(typeof(TWorldArchetype));
        }
        private static int GetToken()
        {
            _tokenCount++;
            Array.Resize(ref _componentCounts, _tokenCount);
            Array.Resize(ref _subjectsCounts, _tokenCount);
            foreach (var item in _resizer)
                item.Resize(_tokenCount);
            return _tokenCount - 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWorldId(Type archetype)
        {
            if(_worldIds.TryGetValue(archetype, out int id) == false)
            {
                id = GetToken();
                _worldIds.Add(archetype, id);
            }
            return id;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWorldId<TWorldArchetype>() => WorldIndex<TWorldArchetype>.id;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetComponentId<T>(int worldID) => Component<T>.Get(worldID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSubjectId<T>(int worldID) => Subject<T>.Get(worldID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetExecutorId<T>(int worldID) => Executor<T>.Get(worldID);
        private abstract class Resizer
        {
            public abstract void Resize(int size);
        }
        private sealed class Resizer<T> : Resizer
        {
            public override void Resize(int size)
            {
                Array.Resize(ref Component<T>.ids, size);
                Array.Resize(ref Subject<T>.ids, size);
                Array.Resize(ref Executor<T>.ids, size);
            }
        }
        private static class Component<T>
        {
            public static int[] ids;
            static Component()
            {
                ids = new int[_tokenCount];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = -1;
                _resizer.Add(new Resizer<T>());
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Get(int token)
            {
                ref int id = ref ids[token];
                if (id < 0)
                    id = _componentCounts[token]++;
                return id;
            }
        }
        private static class Subject<T>
        {
            public static int[] ids;
            static Subject()
            {
                ids = new int[_tokenCount];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = -1;
                _resizer.Add(new Resizer<T>());
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Get(int token)
            {
                ref int id = ref ids[token];
                if (id < 0)
                    id = _subjectsCounts[token]++;
                return id;
            }
        }
        private static class Executor<T>
        {
            public static int[] ids;
            static Executor()
            {
                ids = new int[_tokenCount];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = -1;
                _resizer.Add(new Resizer<T>());
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Get(int token)
            {
                ref int id = ref ids[token];
                if (id < 0)
                    id = _subjectsCounts[token]++;
                return id;
            }
        }
    }
    #endregion

    #region Callbacks Interface
    public interface IEcsWorldEventListener
    {
        void OnWorldResize(int newSize);
        void OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer);
        void OnWorldDestroy();
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
        public static void InvokeOnNewEntity(this List<IEcsWorldEventListener> self, int entityID)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnNewEntity(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnDelEntity(this List<IEcsWorldEventListener> self, int entityID)
        {
            for (int i = 0, iMax = self.Count; i < iMax; i++) self[i].OnDelEntity(entityID);
        }
    }
    #endregion
}
