﻿using System;
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
        private const int DEL_ENT_BUFFER_SIZE_OFFSET = 2;
        private readonly int _worldArchetypeID = WorldMetaStorage.GetWorldId<TWorldArchetype>();

        private IntDispenser _entityDispenser;
        private int _entitiesCount;
        private int _entitesCapacity;
        private short[] _gens; //старший бит указывает на то жива ли сущьность.
        //private short[] _componentCounts; //TODO
        private EcsGroup _allEntites;

        //буфер удаления откладывает освобождение андишников сущьностей.
        //Нужен для того чтобы запускать некоторые процесыы связанные с удалением сущьности не по одному при каждом удалении, а пачкой
        private int[] _delEntBuffer;
        private int _delEntBufferCount;

        private IEcsPool[] _pools;
        private EcsNullPool _nullPool;

        private EcsQueryBase[] _queries;

        private EcsPipeline _pipeline;

        private List<WeakReference<EcsGroup>> _groups;
        private Stack<EcsGroup> _groupsPool = new Stack<EcsGroup>(64);

        private PoolRunners _poolRunners;
        private IEcsEntityCreate _entityCreate;
        private IEcsEntityDestroy _entityDestry;

        #region GetterMethods
        public ReadOnlySpan<IEcsPool> GetAllPools() => new ReadOnlySpan<IEcsPool>(_pools);
        public int GetComponentID<T>() => WorldMetaStorage.GetComponentId<T>(_worldArchetypeID);////ComponentType<TWorldArchetype>.uniqueID;

        #endregion

        #region Properties
        public Type Archetype => typeof(TWorldArchetype);
        public int UniqueID => uniqueID;
        public int Count => _entitiesCount;
        public int Capacity => _entitesCapacity; //_denseEntities.Length;
        public EcsPipeline Pipeline => _pipeline;
        public EcsReadonlyGroup Entities => _allEntites.Readonly;
        #endregion

        #region Constructors
        public EcsWorld(EcsPipeline pipline)
        { 
            _pipeline = pipline ?? EcsPipeline.Empty;
            if (!_pipeline.IsInit) pipline.Init();
            _entityDispenser = new IntDispenser(0);
            _nullPool = EcsNullPool.instance;
            _pools = new IEcsPool[512];
            ArrayUtility.Fill(_pools, _nullPool);

            _gens = new short[512];
            _entitesCapacity = _gens.Length;
            _delEntBufferCount = 0;
            _delEntBuffer = new int[_gens.Length >> DEL_ENT_BUFFER_SIZE_OFFSET];

            _groups = new List<WeakReference<EcsGroup>>();
            _allEntites = GetGroupFromPool();

            _queries = new EcsQuery[128];

            _poolRunners = new PoolRunners(_pipeline);
            _entityCreate = _pipeline.GetRunner<IEcsEntityCreate>();
            _entityDestry = _pipeline.GetRunner<IEcsEntityDestroy>();
            _pipeline.GetRunner<IEcsInject<IEcsWorld>>().Inject(this);
            _pipeline.GetRunner<IEcsWorldCreate>().OnWorldCreate(this);
        }
        #endregion

        #region GetPool
        public EcsPool<TComponent> GetPool<TComponent>() where TComponent : struct
        {
            int uniqueID = WorldMetaStorage.GetComponentId<TComponent>(_worldArchetypeID);

            if (uniqueID >= _pools.Length)
            {
                int oldCapacity = _pools.Length;
                Array.Resize(ref _pools, _pools.Length << 1);
                ArrayUtility.Fill(_pools, _nullPool, oldCapacity, oldCapacity - _pools.Length);
            }

            if (_pools[uniqueID] == _nullPool)
                _pools[uniqueID] = new EcsPool<TComponent>(this, 512, _poolRunners);

            return (EcsPool<TComponent>)_pools[uniqueID];
        }
        #endregion

        #region Queries
        public TQuery Where<TQuery>(out TQuery query) where TQuery : EcsQueryBase
        {
            query = Select<TQuery>();
            query.Execute();
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
            _delEntBuffer[_delEntBufferCount++] = entity.id;
            _gens[entity.id] |= short.MinValue;
            _entitiesCount--;
            _entityDestry.OnEntityDestroy(entity);

            if(_delEntBufferCount >= _delEntBuffer.Length)
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
        EcsGroup IEcsTable.GetGroupFromPool() => GetGroupFromPool();
        internal EcsGroup GetGroupFromPool()
        {
            if (_groupsPool.Count <= 0)
                return new EcsGroup(this);
            return _groupsPool.Pop();
        }
        void IEcsTable.ReleaseGroup(EcsGroup group)
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
