using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    public interface IEcsWorld : IEcsTable
    {
        #region Properties
        public int UniqueID { get; }
        public EcsPipeline Pipeline { get; }
        #endregion

        #region Entities
        public EcsEntity NewEntity();
        public void DelEntity(EcsEntity entity);
        public bool EntityIsAlive(int entityID, short gen);
        public EcsEntity GetEcsEntity(int entityID);
        #endregion
    }

    public abstract class EcsWorld : IEcsWorld
    {
        private const short GEN_BITS = 0x7fff;
        private const short DEATH_GEN_BIT = short.MinValue;

        public static EcsWorld[] Worlds = new EcsWorld[8];
        private static IntDispenser _worldIdDispenser = new IntDispenser(0);
        public readonly short uniqueID;

        private const int DEL_ENT_BUFFER_SIZE_OFFSET = 2;
        private int _worldArchetypeID;

        private IntDispenser _entityDispenser;
        private int _entitiesCount;
        private int _entitesCapacity;
        private short[] _gens; //старший бит указывает на то жива ли сущьность.
        //private short[] _componentCounts; //TODO
        private EcsGroup _allEntites;

        //буфер удаления откладывает освобождение андишников сущьностей.
        //Нужен для того чтобы запускать некоторые процесыы связанные с удалением сущьности не по одному при каждом удалении, а пачкой
        //В теории такой подход частично улучшает ситуацию с переполнением поколений
        private int[] _delEntBuffer;
        private int _delEntBufferCount;

        private EcsPoolBase[] _pools;
        private EcsNullPool _nullPool;

        private EcsQueryBase[] _queries;

        private EcsPipeline _pipeline;

        private List<WeakReference<EcsGroup>> _groups;
        private Stack<EcsGroup> _groupsPool = new Stack<EcsGroup>(64);

        private IEcsEntityCreate _entityCreate;
        private IEcsEntityDestroy _entityDestry;

        #region GetterMethods
        public ReadOnlySpan<EcsPoolBase> GetAllPools() => new ReadOnlySpan<EcsPoolBase>(_pools);
        public int GetComponentID<T>() => WorldMetaStorage.GetComponentId<T>(_worldArchetypeID);////ComponentType<TWorldArchetype>.uniqueID;

        #endregion

        #region Properties
        public abstract Type Archetype { get; }
        public int UniqueID => uniqueID;
        public int Count => _entitiesCount;
        public int Capacity => _entitesCapacity; //_denseEntities.Length;
        public EcsPipeline Pipeline => _pipeline;
        public EcsReadonlyGroup Entities => _allEntites.Readonly;
        #endregion

        #region Internal Properties
        internal EcsPoolBase[] Pools
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _pools;
        }
        #endregion

        #region Constructors/Destroy
        public EcsWorld(EcsPipeline pipline)
        {
            _entitesCapacity = 512;

            uniqueID = (short)_worldIdDispenser.GetFree();
            if (uniqueID >= Worlds.Length)
                Array.Resize(ref Worlds, Worlds.Length << 1);
            Worlds[uniqueID] = this;

            _worldArchetypeID = WorldMetaStorage.GetWorldId(Archetype);

            _pipeline = pipline ?? EcsPipeline.Empty;
            if (!_pipeline.IsInit) pipline.Init();
            _entityDispenser = new IntDispenser(0);
            _nullPool = EcsNullPool.instance;
            _pools = new EcsPoolBase[512];
            ArrayUtility.Fill(_pools, _nullPool);

            _gens = new short[_entitesCapacity];
            ArrayUtility.Fill(_gens, DEATH_GEN_BIT);
            _delEntBufferCount = 0;
            _delEntBuffer = new int[_entitesCapacity >> DEL_ENT_BUFFER_SIZE_OFFSET];

            _groups = new List<WeakReference<EcsGroup>>();
            _allEntites = GetGroupFromPool();

            _queries = new EcsQueryBase[128];

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
            _pools = null;
            _nullPool = null;
            _queries = null;

            Worlds[uniqueID] = null;
            _worldIdDispenser.Release(uniqueID);
        }
        public void DestryWithPipeline()
        {
            Destroy();
            _pipeline.Destroy();
        }
        #endregion

        #region GetPool
        public TPool GetPool<TComponent, TPool>() where TComponent : struct where TPool : EcsPoolBase<TComponent>, new()
        {
            int uniqueID = WorldMetaStorage.GetComponentId<TComponent>(_worldArchetypeID);

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
                pool.InvokeInit(this);

                //EcsDebug.Print(pool.GetType().FullName);
            }

            return (TPool)_pools[uniqueID];
        }
        #endregion

        #region Queries
        public TQuery Where<TQuery>(out TQuery query) where TQuery : EcsQuery
        {
            query = Select<TQuery>();
            query.ExecuteWhere();
            return query;
        }
        public TQuery Select<TQuery>() where TQuery : EcsQueryBase
        {
            int uniqueID = WorldMetaStorage.GetQueryId<TQuery>(_worldArchetypeID);
            if (uniqueID >= _queries.Length)
                Array.Resize(ref _queries, _queries.Length << 1);
            if (_queries[uniqueID] == null)
                _queries[uniqueID] = EcsQueryBase.Builder.Build<TQuery>(this);
            return (TQuery)_queries[uniqueID];
        }
        #endregion

        #region IsMaskCompatible
        public bool IsMaskCompatible(EcsComponentMask mask, int entityID)
        {
#if (DEBUG && !DISABLE_DRAGONECS_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (mask.WorldArchetype != Archetype)
                throw new EcsFrameworkException("mask.WorldArchetypeType != typeof(TTableArhetype)");
#endif
            for (int i = 0, iMax = mask.Inc.Length; i < iMax; i++)
            {
                if (!_pools[mask.Inc[i]].Has(entityID))
                    return false;
            }
            for (int i = 0, iMax = mask.Exc.Length; i < iMax; i++)
            {
                if (_pools[mask.Exc[i]].Has(entityID))
                    return false;
            }
            return true;
        }
        #endregion

        #region Entity
        public EcsEntity NewEntity()
        {
            int entityID = _entityDispenser.GetFree();
            _entitiesCount++;

            if (_gens.Length <= entityID)
            {
                Array.Resize(ref _gens, _gens.Length << 1);
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
                    item.InvokeOnWorldResize(_gens.Length);
            }
            _gens[entityID] &= GEN_BITS;
            EcsEntity entity = new EcsEntity(entityID, ++_gens[entityID], uniqueID);
          //  UnityEngine.Debug.Log($"{entityID}  {_gens[entityID]} {uniqueID}");
            _entityCreate.OnEntityCreate(entity);
            _allEntites.Add(entityID);
            return entity;
        }
        public void DelEntity(EcsEntity entity)
        {
            _allEntites.Remove(entity.id);
            _delEntBuffer[_delEntBufferCount++] = entity.id;
            _gens[entity.id] |= DEATH_GEN_BIT;
            _entitiesCount--;
            _entityDestry.OnEntityDestroy(entity);

            if (_delEntBufferCount >= _delEntBuffer.Length)
                ReleaseDelEntBuffer();
        }

        private void ReleaseDelEntBuffer()//TODO проверить что буфер удаления работает нормально
        {
            for (int i = 0; i < _delEntBufferCount; i++)
                _entityDispenser.Release(_delEntBuffer[i]);
            _delEntBufferCount = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEntity GetEcsEntity(int entityID)
        {
            return new EcsEntity(entityID, _gens[entityID], uniqueID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EntityIsAlive(int entityID, short gen) //TODO пофиксить EntityIsAlive
        {
            return _gens[entityID] == gen;
        }
        #endregion

        #region Groups
        void IEcsTable.RegisterGroup(EcsGroup group) => RegisterGroup(group);
        internal void RegisterGroup(EcsGroup group)
        {
            _groups.Add(new WeakReference<EcsGroup>(group));
        }
        EcsGroup IEcsTable.GetGroupFromPool() => GetGroupFromPool();
        internal EcsGroup GetGroupFromPool()
        {
            if (_groupsPool.Count <= 0)
                return new EcsGroup(this);
            return _groupsPool.Pop();
        }
        void IEcsTable.ReleaseGroup(EcsGroup group) => ReleaseGroup(group);
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
    }

    public abstract class EcsWorld<TWorldArchetype> : EcsWorld
        where TWorldArchetype : EcsWorld<TWorldArchetype>
    {
        public override Type Archetype => typeof(TWorldArchetype);
        public EcsWorld(EcsPipeline pipline) : base(pipline) { }
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
        public static int GetQueryId<T>(int worldID) => Query<T>.Get(worldID);
        private abstract class Resizer
        {
            public abstract void Resize(int size);
        }
        private sealed class Resizer<T> : Resizer
        {
            public override void Resize(int size)
            {
                Array.Resize(ref Component<T>.ids, size);
                Array.Resize(ref Query<T>.ids, size);
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
        private static class Query<T>
        {
            public static int[] ids;
            static Query()
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
}
