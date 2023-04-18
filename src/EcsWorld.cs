using DCFApixels.DragonECS.Internal;
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
        public EcsReadonlyGroup Entities => default;
        #endregion

        #region Entities
        public EcsEntity NewEntity();
        public void DelEntity(EcsEntity entity);
        public bool EntityIsAlive(int entityID, short gen);
        public EcsEntity GetEcsEntity(int entityID);
        public void Destroy();
        #endregion

        #region Group
        internal EcsGroup GetGroupFromPool();
        internal void ReleaseGroup(EcsGroup group);
        #endregion
    }

    public abstract class EcsWorld
    {
        public static IEcsWorld[] Worlds = new IEcsWorld[8];
        private static IntDispenser _worldIdDispenser = new IntDispenser(0);

        public readonly short uniqueID;

        protected EcsWorld()
        {
            uniqueID = (short)_worldIdDispenser.GetFree();
            if (uniqueID >= Worlds.Length)
                Array.Resize(ref Worlds, Worlds.Length << 1);
            Worlds[uniqueID] = (IEcsWorld)this;
        }

        protected void Realeze()    
        {
            Worlds[uniqueID] = null;  
            _worldIdDispenser.Release(uniqueID);
        }
    }

    public abstract class EcsWorld<TWorldArchetype> : EcsWorld, IEcsWorld 
        where TWorldArchetype : EcsWorld<TWorldArchetype>
    {
        private readonly int _worldArchetypeID = ComponentIndexer.GetWorldId<TWorldArchetype>();

        private IntDispenser _entityDispenser;
        private int _entitiesCount;
        private int _entitesCapacity;
        private short[] _gens; //старший бит указывает на то жива ли сущьность.
        private EcsGroup _allEntites;
        //private short[] _componentCounts; //TODO

        private EcsPool[] _pools;
        private EcsNullPool _nullPool;

        private EcsQueryBase[] _queries;

        private EcsPipeline _pipeline;

        private List<WeakReference<EcsGroup>> _groups;
        private Stack<EcsGroup> _groupsPool = new Stack<EcsGroup>(64);

        private PoolRunnres _poolRunnres;
        private IEcsEntityCreate _entityCreate;
        private IEcsEntityDestroy _entityDestry;

        #region GetterMethods
        public ReadOnlySpan<EcsPool> GetAllPools() => new ReadOnlySpan<EcsPool>(_pools);
        public int GetComponentID<T>() => ComponentIndexer.GetComponentId<T>(_worldArchetypeID);////ComponentType<T>.uniqueID;

        #endregion

        #region Properties
        public Type ArchetypeType => typeof(TWorldArchetype);
        public int UniqueID => uniqueID;
        public int Count => _entitiesCount;
        public int Capacity => _entitesCapacity; //_denseEntities.Length;
        public EcsPipeline Pipeline => _pipeline;
        public EcsReadonlyGroup Entities => _allEntites.Readonly;
        #endregion

        #region Constructors
        public EcsWorld(EcsPipeline pipline = null)
        { 
            _pipeline = pipline ?? EcsPipeline.Empty;
            if (!_pipeline.IsInit) pipline.Init();
            _entityDispenser = new IntDispenser(0);
            _nullPool = EcsNullPool.instance;
            _pools = new EcsPool[512];
            ArrayUtility.Fill(_pools, _nullPool);

            _gens = new short[512];
            _entitesCapacity = _gens.Length;

            _queries = new EcsQuery[QueryType.capacity];
            _groups = new List<WeakReference<EcsGroup>>();

            _poolRunnres = new PoolRunnres(_pipeline);
            _entityCreate = _pipeline.GetRunner<IEcsEntityCreate>();
            _entityDestry = _pipeline.GetRunner<IEcsEntityDestroy>();
            _pipeline.GetRunner<IEcsInject<TWorldArchetype>>().Inject((TWorldArchetype)this);
            _pipeline.GetRunner<IEcsInject<IEcsWorld>>().Inject(this);
            _pipeline.GetRunner<IEcsWorldCreate>().OnWorldCreate(this);
            
            _allEntites = GetGroupFromPool();
        }
        #endregion

        #region GetPool
        public EcsPool<T> GetPool<T>() where T : struct
        {
            int uniqueID = ComponentIndexer.GetComponentId<T>(_worldArchetypeID);

            if (uniqueID >= _pools.Length)
            {
                int oldCapacity = _pools.Length;
                Array.Resize(ref _pools, _pools.Length << 1);
                ArrayUtility.Fill(_pools, _nullPool, oldCapacity, oldCapacity - _pools.Length);
            }

            if (_pools[uniqueID] == _nullPool)
            {
                _pools[uniqueID] = new EcsPool<T>(this, uniqueID, 512, _poolRunnres);
            }
            return (EcsPool<T>)_pools[uniqueID];
        }
        #endregion

        #region Query
        public TQuery Query<TQuery>(out TQuery query) where TQuery : EcsQueryBase
        {
            int uniqueID = QueryType<TQuery>.uniqueID;
            if (_queries.Length < QueryType.capacity)
                Array.Resize(ref _queries, QueryType.capacity);
            if (_queries[uniqueID] == null)
                _queries[uniqueID] = EcsQueryBase.Builder.Build<TQuery>(this);
            query = (TQuery)_queries[uniqueID];
            query.Execute();
            return query;
        }
        #endregion

        #region IsMaskCompatible
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMaskCompatible<TInc, TExc>(int entityID) where TInc : struct, IInc where TExc : struct, IExc
        {
            return IsMaskCompatible(EcsMaskMap<TWorldArchetype>.GetMask<TInc, TExc>(), entityID);
        }
        public bool IsMaskCompatible(EcsComponentMask mask, int entityID)
        {
#if (DEBUG && !DISABLE_DRAGONECS_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (mask.WorldArchetypeType != typeof(TWorldArchetype))
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
            }
            _gens[entityID] |= short.MinValue;
            EcsEntity entity = new EcsEntity(entityID, _gens[entityID]++, uniqueID);
            _entityCreate.OnEntityCreate(entity);
            _allEntites.Add(entityID);
            return entity;
        }
        public void DelEntity(EcsEntity entity)
        {
            _allEntites.Remove(entity.id);
            _entityDispenser.Release(entity.id);
            _gens[entity.id] |= short.MinValue;
            _entitiesCount--;
            _entityDestry.OnEntityDestroy(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEntity GetEcsEntity(int entityID)
        {
            return new EcsEntity(entityID, _gens[entityID], uniqueID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EntityIsAlive(int entityID, short gen)
        {
            return _gens[entityID] == gen;
        }
        #endregion

        #region Destroy
        public void Destroy()
        {
            _entityDispenser = null;
            //_denseEntities = null;
            _gens = null;
            _pools = null;
            _nullPool = null;
            _queries = null;
            Realeze();
        }
        public void DestryWithPipeline()
        {
            Destroy();
            _pipeline.Destroy();
        }
        #endregion

        #region Groups
        void IEcsTable.RegisterGroup(EcsGroup group)
        {
            _groups.Add(new WeakReference<EcsGroup>(group));
        }
        EcsGroup IEcsWorld.GetGroupFromPool() => GetGroupFromPool();
        internal EcsGroup GetGroupFromPool()
        {
            if (_groupsPool.Count <= 0)
                return new EcsGroup(this);
            return _groupsPool.Pop();
        }
        void IEcsWorld.ReleaseGroup(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (group.World != this)
                throw new ArgumentException("groupFilter.World != this");
#endif
            group.Clear();
            _groupsPool.Push(group);
        }
        #endregion

        #region Utils
        internal static class QueryType
        {
            public static int increment = 0;
            public static int capacity = 128;
        }
        internal static class QueryType<TQuery>
        {
            public static int uniqueID;
            static QueryType()
            {
                uniqueID = QueryType.increment++;
                if (QueryType.increment > QueryType.capacity)
                    QueryType.capacity <<= 1;
            }
        }
        #endregion
    }

    #region Utils
    [StructLayout(LayoutKind.Sequential, Pack = 8, Size = 24)]
    internal readonly struct PoolRunnres
    {
        public readonly IEcsComponentAdd add;
        public readonly IEcsComponentWrite write;
        public readonly IEcsComponentDel del;

        public PoolRunnres(EcsPipeline pipeline)
        {
            add = pipeline.GetRunner<IEcsComponentAdd>();
            write = pipeline.GetRunner<IEcsComponentWrite>();
            del = pipeline.GetRunner<IEcsComponentDel>();
        }
    }
    public static class ComponentIndexer
    {
        private static List<Resizer> resizer = new List<Resizer>();
        private static int tokenCount = 0;
        private static int[] componentCounts = new int[0];
        private static class World<TWorldArchetype>
        {
            public static int id = GetToken();
        }
        private static int GetToken()
        {
            tokenCount++;
            Array.Resize(ref componentCounts, tokenCount);
            foreach (var item in resizer)
                item.Resize(tokenCount);
            return tokenCount - 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWorldId<TWorldArchetype>() => World<TWorldArchetype>.id;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetComponentId<TComponent>(int worldID) => Component<TComponent>.Get(worldID);
        private abstract class Resizer
        {
            public abstract void Resize(int size);
        }
        private sealed class Resizer<T> : Resizer
        {
            public override void Resize(int size) => Array.Resize(ref Component<T>.ids, size);
        }
        private static class Component<TComponent>
        {
            public static int[] ids;
            static Component()
            {
                ids = new int[tokenCount];
                for (int i = 0; i < ids.Length; i++)
                    ids[i] = -1;
                resizer.Add(new Resizer<TComponent>());
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
    }
    #endregion
}
