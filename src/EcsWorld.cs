using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class EcsWorld
    {
        public const int MAX_WORLDS = byte.MaxValue; //Номер последнего мира 254
        public const int DEAD_WORLD_ID = byte.MaxValue; //Зарезервированный номер мира для мертвых сущьностей

        private byte _id = DEAD_WORLD_ID;

        private float _timeScale;//TODO реализовать собсвенныйтайм склей для разных миров

        private IEcsPool[] _pools;
        private SparseSet _componentIDToPoolID;

        private SparseSet _entities = new SparseSet();
        private short[] _gens;

        private List<EcsFilter>[] _filtersByIncludedComponents;
        private List<EcsFilter>[] _filtersByExcludedComponents;

        private EcsFilter[] _filters;
        private SparseSet _maskIDToFilterID;

        #region Properties
        public int ID => _id;
        public bool IsAlive => _id != DEAD_WORLD_ID;
        public bool IsEmpty => _entities.Count < 0;
        #endregion

        #region Constructors
        public EcsWorld()
        {
            _pools = new IEcsPool[512];
            _entities = new SparseSet(512);
            _componentIDToPoolID = new SparseSet(512);
            _maskIDToFilterID = new SparseSet(512);
            _filters = new EcsFilter[512];
        }
        #endregion

        #region Filters
        public EcsFilter GetFilter<TMask>(TMask mask) where TMask : Mask
        {
            if (_maskIDToFilterID.TryAdd(mask.ID, ref _filters))
            {
                EcsFilter filter = new EcsFilter(this, mask, 512);
                _filters[_maskIDToFilterID.IndexOf(mask.ID)] = filter;
                return filter;
            }
            else
            {
                return _filters[_maskIDToFilterID.IndexOf(mask.ID)];
            }
        }
        #endregion

        #region GetPool
        public EcsPool<T> GetPool<T>()
            where T : struct
        {
            int uniqueID = ComponentType<T>.uniqueID;
            int poolIndex = _componentIDToPoolID.IndexOf(uniqueID);
            if (poolIndex >= 0)
            {
                return (EcsPool<T>)_pools[poolIndex];
            }
#if DEBUG
            if (_componentIDToPoolID.Count >= ushort.MaxValue) 
            { 
                throw new EcsFrameworkException("No more room for new component into this world.");
            }
#endif
            var pool = new EcsPool<T>(this, 512);
            _componentIDToPoolID.Add(uniqueID);
            _componentIDToPoolID.Normalize(ref _pools);
            _componentIDToPoolID.Normalize(ref _filtersByIncludedComponents);
            _componentIDToPoolID.Normalize(ref _filtersByExcludedComponents);

            _pools[_componentIDToPoolID.IndexOf(poolIndex)] = pool;
            return pool;
        }
        #endregion

        #region NewEntity
        public ent NewEntity()
        {
            int entityID = _entities.GetFree();
            _entities.Normalize(ref _gens);
            _gens[entityID]++;


            return new ent(entityID, _gens[entityID], _id);
        }
        #endregion

        #region Destroy
        public void Destroy()
        {
            _id = DEAD_WORLD_ID;
        }
        #endregion

        #region IsMaskCompatible/IsMaskCompatibleWithout
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMaskCompatible(Mask mask, int entity)
        {
            for (int i = 0, iMax = mask.IncCount; i < iMax; i++)
            {
                if (!_pools[_componentIDToPoolID[mask.Include[i]]].Has(entity))
                {
                    return false;
                }
            }
            for (int i = 0, iMax = mask.ExcCount; i < iMax; i++)
            {
                if (_pools[_componentIDToPoolID[mask.Exclude[i]]].Has(entity))
                {
                    return false;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsMaskCompatibleWithout(Mask mask, int entity, int otherPoolID)
        {
            for (int i = 0, iMax = mask.IncCount; i < iMax; i++)
            {
                int poolID = _componentIDToPoolID[mask.Include[i]];
                if (poolID == otherPoolID || !_pools[poolID].Has(entity))
                {
                    return false;
                }
            }
            for (int i = 0, iMax = mask.ExcCount; i < iMax; i++)
            {
                int poolID = _componentIDToPoolID[mask.Exclude[i]];
                if (poolID != otherPoolID && _pools[poolID].Has(entity))
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region EntityChangedReact
        internal void OnEntityComponentAdded(int entityID, int changedPoolID)
        {
            var includeList = _filtersByIncludedComponents[changedPoolID];
            var excludeList = _filtersByExcludedComponents[changedPoolID];

            if (includeList != null)
            {
                foreach (var filter in includeList)
                {
                    if (IsMaskCompatible(filter.Mask, entityID))
                    {
                        filter.Add(entityID);
                    }
                }
            }
            if (excludeList != null)
            {
                foreach (var filter in excludeList)
                {
                    if (IsMaskCompatibleWithout(filter.Mask, entityID, changedPoolID))
                    {
                        filter.Remove(entityID);
                    }
                }
            }
        }

        internal void OnEntityComponentRemoved(int entityID, int changedPoolID)
        {
            var includeList = _filtersByIncludedComponents[changedPoolID];
            var excludeList = _filtersByExcludedComponents[changedPoolID];

            if (includeList != null)
            {
                foreach (var filter in includeList)
                {
                    if (IsMaskCompatible(filter.Mask, entityID))
                    {
                        filter.Remove(entityID);
                    }
                }
            }
            if (excludeList != null)
            {
                foreach (var filter in excludeList)
                {
                    if (IsMaskCompatibleWithout(filter.Mask, entityID, changedPoolID))
                    {
                        filter.Add(entityID);
                    }
                }
            }
        }
        #endregion
    }
}
