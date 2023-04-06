using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    public interface IEcsWorld : IEcsReadonlyTable
    {
        #region Properties
        //private float _timeScale;//TODO реализовать собсвенныйтайм склей для разных миров
        public int ID { get; }
        public EcsPipeline Pipeline { get; }
        public int EntitesCount { get; }
        public int EntitesCapacity { get; }
        #endregion

        #region Entities
        public EcsFilter Entities<TComponent>() where TComponent : struct;

        public ent NewEntity();
        public void DelEntity(ent entity);
        public bool EntityIsAlive(int entityID, short gen);
        public ent GetEntity(int entityID);
        public void Destroy();
        #endregion
    }

    public abstract class EcsWorld
    {
        internal static IEcsWorld[] Worlds = new IEcsWorld[8];
        private static IntDispenser _worldIdDispenser = new IntDispenser(1);

        public readonly short id;

        protected EcsWorld(bool isIndexed)
        {
            if(isIndexed == true)
            {
                id = (short)_worldIdDispenser.GetFree();
                if (id >= Worlds.Length)
                    Array.Resize(ref Worlds, Worlds.Length << 1);
                Worlds[id] = (IEcsWorld)this;
            }
            else
            {
                id = -1;
            }
        }

        protected void Realeze()    
        {
            Worlds[id] = null;  
            _worldIdDispenser.Release(id);
        }
    }

    public abstract class EcsWorld<TArchetype> : EcsWorld, IEcsWorld 
        where TArchetype : EcsWorld<TArchetype>
    {
        private IntDispenser _entityDispenser;
        private int[] _denseEntities;
        private int _entitiesCount;
        private short[] _gens; //старший бит указывает на то жива ли сущьность.

        //private short[] _componentCounts; //TODO

        private IEcsPool[] _pools;
        private EcsNullPool _nullPool;

        private List<EcsFilter>[] _filtersByIncludedComponents;
        private List<EcsFilter>[] _filtersByExcludedComponents;

        private EcsFilter[] _filters;

        private EcsPipeline _pipeline;

        private List<EcsGroup> _groups;

        #region RunnersCache
        private PoolRunnres _poolRunnres;
        private IEcsEntityCreate _entityCreate;
        private IEcsEntityDestroy _entityDestry;
        #endregion

        #region GetterMethods
        public ReadOnlySpan<IEcsPool> GetAllPools() => new ReadOnlySpan<IEcsPool>(_pools);
        public int GetComponentID<T>() => ComponentType<T>.uniqueID;

        #endregion

        #region Internal Properties
        int IEcsReadonlyTable.Count => _entitiesCount;
        int IEcsReadonlyTable.Capacity => _denseEntities.Length;
        #endregion

        #region Properties
        public Type ArchetypeType => typeof(TArchetype);
        public int ID => id;
        public EcsPipeline Pipeline => _pipeline;

        public int EntitesCount => _entitiesCount;
        public int EntitesCapacity => _denseEntities.Length;
        #endregion

        #region Constructors
        public EcsWorld(EcsPipeline pipline = null) : base(true)
        {
            _pipeline = pipline ?? EcsPipeline.Empty;
            if (!_pipeline.IsInit) pipline.Init();
            _entityDispenser = new IntDispenser(1);
            _nullPool = EcsNullPool.instance;
            _pools = new IEcsPool[512];
            FillArray(_pools, _nullPool);

            _gens = new short[512];
            _filters = new EcsFilter[64];
            _groups = new List<EcsGroup>(128);

            _denseEntities = new int[512];

            _filtersByIncludedComponents = new List<EcsFilter>[16];
            _filtersByExcludedComponents = new List<EcsFilter>[16];

            _poolRunnres = new PoolRunnres(_pipeline);
            _entityCreate = _pipeline.GetRunner<IEcsEntityCreate>();
            _entityDestry = _pipeline.GetRunner<IEcsEntityDestroy>();
            _pipeline.GetRunner<IEcsInject<TArchetype>>().Inject((TArchetype)this);
            _pipeline.GetRunner<IEcsInject<IEcsWorld>>().Inject(this);
            _pipeline.GetRunner<IEcsWorldCreate>().OnWorldCreate(this);
        }
        #endregion

        #region GetPool
        public EcsPool<T> GetPool<T>() where T : struct
        {
            int uniqueID = ComponentType<T>.uniqueID;

            if (uniqueID >= _pools.Length)
            {
                int oldCapacity = _pools.Length;
                Array.Resize(ref _pools, ComponentType.Capacity);
                FillArray(_pools, _nullPool, oldCapacity, oldCapacity - _pools.Length);

                Array.Resize(ref _filtersByIncludedComponents, ComponentType.Capacity);
                Array.Resize(ref _filtersByExcludedComponents, ComponentType.Capacity);
            }

            if (_pools[uniqueID] == _nullPool)
            {
                _pools[uniqueID] = new EcsPool<T>(this, ComponentType<T>.uniqueID, 512, _poolRunnres);
            }
            return (EcsPool<T>)_pools[uniqueID];
        }
        public EcsPool<T> UncheckedGetPool<T>() where T : struct
        {
            return (EcsPool<T>)_pools[ComponentType<T>.uniqueID];
        }
        #endregion

        #region GetFilter
        public EcsFilter Entities<TComponent>() where TComponent : struct => Filter<Inc<TComponent>, Exc>();
        public EcsFilter Filter<TInc>() where TInc : struct, IInc => Filter<TInc, Exc>();
        public EcsFilter Filter<TInc, TExc>() where TInc : struct, IInc where TExc : struct, IExc
        {
            var mask = EcsMaskMap<TArchetype>.GetMask<TInc, TExc>();

            if (_filters.Length <= EcsMaskMap<TArchetype>.Capacity)
            {
                Array.Resize(ref _filters, EcsMaskMap<TArchetype>.Capacity);
            }

            if (_filters[mask.UniqueID] == null)
            {
                _filters[mask.UniqueID] = NewFilter(mask);
            }
            return _filters[mask.UniqueID];
        }

        private EcsFilter NewFilter(EcsMask mask, int capacirty = 512)
        {
            var filter = new EcsFilter(this, mask, capacirty);

            for (int i = 0; i < mask.IncCount; i++)
            {
                int componentID = mask.Inc[i];
                var list = _filtersByIncludedComponents[componentID];
                if (list == null)
                {
                    list = new List<EcsFilter>(8);
                    _filtersByIncludedComponents[componentID] = list;
                }
                list.Add(filter);
            }

            for (int i = 0; i < mask.ExcCount; i++)
            {
                int componentID = mask.Exc[i];
                var list = _filtersByExcludedComponents[componentID];
                if (list == null)
                {
                    list = new List<EcsFilter>(8);
                    _filtersByExcludedComponents[componentID] = list;
                }
                list.Add(filter);
            }
            // scan exist entities for compatibility with new filter.
            for (int i = 0; i < _entitiesCount && _entitiesCount <= _denseEntities.Length; i++)
            {
                int entity = _denseEntities[i];
                if (IsMaskCompatible(mask, entity))
                    filter.Add(entity);
            }

            return filter;
        }
        #endregion

        #region IsMaskCompatible/IsMaskCompatibleWithout
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMaskCompatible<TInc>(int entityID) where TInc : struct, IInc
        {
            return IsMaskCompatible(EcsMaskMap<TArchetype>.GetMask<TInc, Exc>(), entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMaskCompatible<TInc, TExc>(int entityID) where TInc : struct, IInc where TExc : struct, IExc
        {
            return IsMaskCompatible(EcsMaskMap<TArchetype>.GetMask<TInc, TExc>(), entityID);
        }

        public bool IsMaskCompatible(EcsMask mask, int entity)
        {
#if (DEBUG && !DISABLE_DRAGONECS_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (mask.WorldArchetypeType != typeof(TArchetype))
                throw new EcsFrameworkException("mask.WorldArchetypeType != typeof(TArchetype)");
#endif
            for (int i = 0, iMax = mask.IncCount; i < iMax; i++)
            {
                if (!_pools[mask.Inc[i]].Has(entity))
                    return false;
            }
            for (int i = 0, iMax = mask.ExcCount; i < iMax; i++)
            {
                if (_pools[mask.Exc[i]].Has(entity))
                    return false;
            }
            return true;
        }

        public bool IsMaskCompatibleWithout(EcsMask mask, int entity, int otherComponentID)
        {
#if (DEBUG && !DISABLE_DRAGONECS_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (mask.WorldArchetypeType != typeof(TArchetype))
                throw new EcsFrameworkException("mask.WorldArchetypeType != typeof(TArchetype)");
#endif
            for (int i = 0, iMax = mask.IncCount; i < iMax; i++)
            {
                int componentID = mask.Inc[i];
                if (componentID == otherComponentID || !_pools[componentID].Has(entity))
                    return false;
            }
            for (int i = 0, iMax = mask.ExcCount; i < iMax; i++)
            {
                int componentID = mask.Exc[i];
                if (componentID != otherComponentID && _pools[componentID].Has(entity))
                    return false;
            }
            return true;
        }
        #endregion

        #region EntityChangedReact

        void IEcsReadonlyTable.OnEntityComponentAdded(int entityID, int componentID)
        {
            var includeList = _filtersByIncludedComponents[componentID];
            var excludeList = _filtersByExcludedComponents[componentID];

            if (includeList != null)
            {
                foreach (var filter in includeList)
                {
                    if (IsMaskCompatible(filter.Mask, entityID))
                    {
                        filter.entities.UncheckedAdd(entityID);
                    }
                }
            }
            if (excludeList != null)
            {
                foreach (var filter in excludeList)
                {
                    if (IsMaskCompatibleWithout(filter.Mask, entityID, componentID))
                    {
                        filter.entities.UncheckedRemove(entityID);
                    }
                }
            }
            //TODO провести стресс тест для варианта выши и закоментированного ниже

            // if (includeList != null) foreach (var filter in includeList) filter.entities.Add(entityID);
            // if (excludeList != null) foreach (var filter in excludeList) filter.entities.Remove(entityID);
        }

        void IEcsReadonlyTable.OnEntityComponentRemoved(int entityID, int componentID)
        {
            var includeList = _filtersByIncludedComponents[componentID];
            var excludeList = _filtersByExcludedComponents[componentID];

            if (includeList != null)
            {
                foreach (var filter in includeList)
                {
                    if (IsMaskCompatible(filter.Mask, entityID))
                    {
                        filter.entities.UncheckedRemove(entityID);
                    }
                }
            }
            if (excludeList != null)
            {
                foreach (var filter in excludeList)
                {
                    if (IsMaskCompatibleWithout(filter.Mask, entityID, componentID))
                    {
                        filter.entities.UncheckedAdd(entityID);
                    }
                }
            }
            //TODO провести стресс тест для варианта выши и закоментированного ниже

            // if (includeList != null) foreach (var filter in includeList) filter.entities.Remove(entityID);
            // if (excludeList != null) foreach (var filter in excludeList) filter.entities.Add(entityID);
        }
        #endregion

        #region Entity
        public ent NewEntity()
        {
            int entityID = _entityDispenser.GetFree();
            if (_entityDispenser.LastInt >= _denseEntities.Length)
                Array.Resize(ref _denseEntities, _denseEntities.Length << 1);
            _denseEntities[_entitiesCount++] = entityID;

            if (_gens.Length <= entityID)
            {
                Array.Resize(ref _gens, _gens.Length << 1);
                foreach (var item in _groups)
                    item.OnWorldResize(_gens.Length);
                foreach (var item in _pools)
                    item.OnWorldResize(_gens.Length);
            }
            _gens[entityID] |= short.MinValue;
            ent entity = new ent(entityID, _gens[entityID]++, id);
            _entityCreate.OnEntityCreate(entity);
            return entity;
        }
        public void DelEntity(ent entity)
        {
            _entityDispenser.Release(entity.id);
            _gens[entity.id] |= short.MinValue;
            _entitiesCount--;
            _entityDestry.OnEntityDestroy(entity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ent GetEntity(int entityID)
        {
            return new ent(entityID, _gens[entityID], id);
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
            _denseEntities = null;
            _gens = null;
            _pools = null;
            _nullPool = null;
            _filtersByIncludedComponents = null;
            _filtersByExcludedComponents = null;
            _filters = null;
            Realeze();
        }
        public void DestryWithPipeline()
        {
            Destroy();
            _pipeline.Destroy();
        }
        #endregion

        #region Utils
        internal static class ComponentType
        {
            internal static int increment = 1;
            internal static int Capacity
            {
                get => types.Length;
            }
            internal static Type[] types = new Type[64];
        }
        internal static class ComponentType<T>
        {
            internal static int uniqueID;

            static ComponentType()
            {
                uniqueID = ComponentType.increment++;
#if (DEBUG && !DISABLE_DRAGONECS_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
                if (ComponentType.increment + 1 > ushort.MaxValue)
                {
                    throw new EcsFrameworkException($"No more room for new component for this {typeof(TArchetype).FullName} IWorldArchetype");
                }
#endif
                if (uniqueID >= ComponentType.types.Length)
                {
                    Array.Resize(ref ComponentType.types, ComponentType.types.Length << 1);
                }
                ComponentType.types[uniqueID] = typeof(T);
            }
        }

        private void FillArray<T>(T[] array, T value, int startIndex = 0, int length = -1)
        {
            if (length < 0)
            {
                length = array.Length;
            }
            else
            {
                length = startIndex + length;
            }
            for (int i = startIndex; i < length; i++)
            {
                array[i] = value;
            }
        }
        #endregion

        #region Other
        void IEcsReadonlyTable.RegisterGroup(EcsGroup group)
        {
            _groups.Add(group);
        }
        #endregion
    }
    
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
}
