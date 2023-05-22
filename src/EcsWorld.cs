using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    using Internal;

    internal sealed class EcsNullWorld : EcsWorld<EcsNullWorld>
    {
        public EcsNullWorld() : base(null, false) { }
    }

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
        private short[] _gens; //старший бит указывает на то жива ли сущьность.
        private short[] _componentCounts;
        private EcsGroup _allEntites;

        //буфер удаления откладывает освобождение андишников сущьностей.
        //Нужен для того чтобы запускать некоторые процесыы связанные с удалением сущьности не по одному при каждом удалении, а пачкой
        //В теории такой подход частично улучшает ситуацию с переполнением поколений
        private int[] _delEntBuffer;
        private int _delEntBufferCount;

        internal IEcsPoolImplementation[] pools;
        private EcsNullPool _nullPool;
        private int _poolsCount = 0;

        private EcsSubject[] _subjects;
        private EcsQueryExecutor[] _executors;

        private EcsPipeline _pipeline;

        private List<WeakReference<EcsGroup>> _groups;
        private Stack<EcsGroup> _groupsPool = new Stack<EcsGroup>(64);

        private IEcsEntityCreate _entityCreate;
        private IEcsEntityDestroy _entityDestry;

        #region Properties
        public abstract Type Archetype { get; }
        public int UniqueID => uniqueID;
        public int Count => _entitiesCount;
        public int Capacity => _entitesCapacity; //_denseEntities.Length;
        public EcsPipeline Pipeline => _pipeline;
        public EcsReadonlyGroup Entities => _allEntites.Readonly;
        public ReadOnlySpan<IEcsPoolImplementation> AllPools => pools;// new ReadOnlySpan<IEcsPoolImplementation>(pools, 0, _poolsCount);
        public int PoolsCount => _poolsCount;
        #endregion

        #region Constructors/Destroy
        static EcsWorld() 
        {
            EcsNullWorld nullWorld = new EcsNullWorld();
            Worlds[0] = nullWorld;
        }
        public EcsWorld(EcsPipeline pipline) : this(pipline, true) { }
        internal EcsWorld(EcsPipeline pipline, bool isIndexable)
        {
            _entitesCapacity = 512;

            if (isIndexable)
            {
                uniqueID = (short)_worldIdDispenser.GetFree();
                if (uniqueID >= Worlds.Length)
                    Array.Resize(ref Worlds, Worlds.Length << 1);
                Worlds[uniqueID] = this;
            }

            _worldTypeID = WorldMetaStorage.GetWorldId(Archetype);

            _pipeline = pipline ?? EcsPipeline.Empty;
            if (!_pipeline.IsInit) pipline.Init();
            _entityDispenser = new IntDispenser(0);
            _nullPool = EcsNullPool.instance;
            pools = new IEcsPoolImplementation[512];
            ArrayUtility.Fill(pools, _nullPool);

            _gens = new short[_entitesCapacity];
            _componentCounts = new short[_entitesCapacity];

            ArrayUtility.Fill(_gens, DEATH_GEN_BIT);
            _delEntBufferCount = 0;
            _delEntBuffer = new int[_entitesCapacity >> DEL_ENT_BUFFER_SIZE_OFFSET];

            _groups = new List<WeakReference<EcsGroup>>();
            _allEntites = GetGroupFromPool();

            _subjects = new EcsSubject[128];
            _executors = new EcsQueryExecutor[128];

            _entityCreate = _pipeline.GetRunner<IEcsEntityCreate>();
            _entityDestry = _pipeline.GetRunner<IEcsEntityDestroy>();
            _pipeline.GetRunner<IEcsInject<EcsWorld>>().Inject(this);
            _pipeline.GetRunner<IEcsWorldCreate>().OnWorldCreate(this);
        }


        public void Destroy()
        {
            _entityDispenser = null;
            //_denseEntities = null;
            _gens = null;
            pools = null;
            _nullPool = null;
            _subjects = null;
            _executors = null;

            Worlds[uniqueID] = null;
            _worldIdDispenser.Release(uniqueID);
        }
        public void DestryWithPipeline()
        {
            Destroy();
            _pipeline.Destroy();
        }
        #endregion

        #region GetComponentID
        public int GetComponentID<T>() => WorldMetaStorage.GetComponentId<T>(_worldTypeID);////ComponentType<TWorldArchetype>.uniqueID;

        #endregion

        #region GetPool
        public TPool GetPool<TComponent, TPool>() where TComponent : struct where TPool : IEcsPoolImplementation<TComponent>, new()
        {
            int uniqueID = WorldMetaStorage.GetComponentId<TComponent>(_worldTypeID);

            if (uniqueID >= pools.Length)
            {
                int oldCapacity = pools.Length;
                Array.Resize(ref pools, pools.Length << 1);
                ArrayUtility.Fill(pools, _nullPool, oldCapacity, oldCapacity - pools.Length);
            }

            if (pools[uniqueID] == _nullPool)
            {
                var pool = new TPool();
                pools[uniqueID] = pool;
                pool.OnInit(this, uniqueID);
                _poolsCount++;
                //EcsDebug.Print(pool.GetType().FullName);
            }

            return (TPool)pools[uniqueID];
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

        #region IsMaskCompatible
        public bool IsMaskCompatible(EcsMask mask, int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (mask.WorldType != Archetype)
                throw new EcsFrameworkException("mask.WorldArchetypeType != typeof(TTableArhetype)");
#endif
            for (int i = 0, iMax = mask.Inc.Length; i < iMax; i++)
            {
                if (!pools[mask.Inc[i]].Has(entityID))
                    return false;
            }
            for (int i = 0, iMax = mask.Exc.Length; i < iMax; i++)
            {
                if (pools[mask.Exc[i]].Has(entityID))
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
                foreach (var item in pools)
                    item.OnWorldResize(_gens.Length);

            }
            _gens[entityID] &= GEN_BITS;
            _entityCreate.OnEntityCreate(entityID);
            _allEntites.Add(entityID);
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
            _entityDestry.OnEntityDestroy(entityID);

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
            foreach (var pool in pools)
                pool.OnReleaseDelEntityBuffer(buffser);
            for (int i = 0; i < _delEntBufferCount; i++)
                _entityDispenser.Release(_delEntBuffer[i]);
            _delEntBufferCount = 0;
        }
        public short GetGen(int entityID) => _gens[entityID];
        public short GetComponentsCount(int entityID) => _componentCounts[entityID];
        public void DeleteEmptyEntites()
        {
            foreach (var e in _allEntites)
            {
                if (_componentCounts[e] <= 0)
                    DelEntity(e);
            }
        }

        public void CopyEntity(int fromEntityID, int toEntityID)
        {
            foreach (var pool in pools)
            {
                if(pool.Has(fromEntityID))
                    pool.Copy(fromEntityID, toEntityID);
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
            foreach (var pool in pools)
            {
                if (!pool.Has(fromEntityID)&& pool.Has(toEntityID))
                        pool.Del(toEntityID);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void IncrementEntityComponentCount(int entityID)
        {
            _componentCounts[entityID]++;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void DecrementEntityComponentCount(int entityID)
        {
            var count = --_componentCounts[entityID];
            if(count == 0 && _allEntites.Has(entityID))
                DelEntity(entityID);

#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (count < 0) throw new EcsFrameworkException("нарушен баланс инкремента.декремента компонентов");
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
            if (_groupsPool.Count <= 0)
                return new EcsGroup(this);
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

            for (var i = 0; i < pools.Length; i++)
            {
                if (pools[i].Has(entityID))
                list.Add(pools[i].GetRaw(entityID));
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
        public EcsWorld(EcsPipeline pipline) : base(pipline) { }
        internal EcsWorld(EcsPipeline pipline, bool isIndexable) : base(pipline, isIndexable) { }
    }

    #region Utils
    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 24)]
    internal readonly struct PoolRunners
    {
        public readonly IEcsComponentAdd add;
        public readonly IEcsComponentWrite write;
        public readonly IEcsComponentDel del;

        public PoolRunners(EcsPipeline pipeline)
        {
            add = pipeline.GetRunner<IEcsComponentAdd>();
            write = pipeline.GetRunner<IEcsComponentWrite>();
            del = pipeline.GetRunner<IEcsComponentDel>();
        }
    }
    public static class WorldMetaStorage
    {
        private static List<Resizer> resizer = new List<Resizer>();
        private static int tokenCount = 0;
        private static int[] componentCounts = new int[0];
        private static int[] queryCounts = new int[0];

        private static Dictionary<Type, int> _worldIds = new Dictionary<Type, int>();

        private static class WorldIndex<TWorldArchetype>
        {
            public static int id = GetWorldId(typeof(TWorldArchetype));
        }
        private static int GetToken()
        {
            tokenCount++;
            Array.Resize(ref componentCounts, tokenCount);
            Array.Resize(ref queryCounts, tokenCount);
            foreach (var item in resizer)
                item.Resize(tokenCount);
            return tokenCount - 1;
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
                ids = new int[tokenCount];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = -1;
                resizer.Add(new Resizer<T>());
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Get(int token)
            {
                ref int id = ref ids[token];
                if (id < 0)
                    id = componentCounts[token]++;
                return id;
            }
        }
        private static class Subject<T>
        {
            public static int[] ids;
            static Subject()
            {
                ids = new int[tokenCount];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = -1;
                resizer.Add(new Resizer<T>());
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Get(int token)
            {
                ref int id = ref ids[token];
                if (id < 0)
                    id = queryCounts[token]++;
                return id;
            }
        }
        private static class Executor<T>
        {
            public static int[] ids;
            static Executor()
            {
                ids = new int[tokenCount];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = -1;
                resizer.Add(new Resizer<T>());
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Get(int token)
            {
                ref int id = ref ids[token];
                if (id < 0)
                    id = queryCounts[token]++;
                return id;
            }
        }
    }
    #endregion

   // #region Callbacks Interface //TODO
   // public interface IWorldCallbacks
   // {
   //     void OnWorldResize(int newSize);
   //     void OnReleaseDelEntityBuffer(ReadOnlySpan<int> buffer);
   //     void OnWorldDestroy();
   // }
   // #endregion
}
