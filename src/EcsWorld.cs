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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsMaskCompatible(Mask filterMask, int entity)
        {
            for (int i = 0, iMax = filterMask.includeCount; i < iMax; i++)
            {
                if (!_pools[filterMask.include[i]].Has(entity))
                {
                    return false;
                }
            }
            for (int i = 0, iMax = filterMask.excludeCount; i < iMax; i++)
            {
                if (_pools[filterMask.exclude[i]].Has(entity))
                {
                    return false;
                }
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsMaskCompatibleWithout(Mask filterMask, int entity, int componentId)
        {
            for (int i = 0, iMax = filterMask.includeCount; i < iMax; i++)
            {
                var typeId = filterMask.include[i];
                if (typeId == componentId || !_pools[typeId].Has(entity))
                {
                    return false;
                }
            }
            for (int i = 0, iMax = filterMask.excludeCount; i < iMax; i++)
            {
                var typeId = filterMask.exclude[i];
                if (typeId != componentId && _pools[typeId].Has(entity))
                {
                    return false;
                }
            }
            return true;
        }

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


        internal void OnEntityComponentRemoved(int entityID, int changedPool)
        {

        }


        public class Mask
        {
            private readonly EcsWorld _world;
            internal int[] include;
            internal int[] exclude;
            internal int includeCount;
            internal int excludeCount;

#if DEBUG && !DCFAECS_NO_SANITIZE_CHECKS
            bool _built;
#endif

            internal Mask(EcsWorld world)
            {
                _world = world;
                include = new int[8];
                exclude = new int[2];
                Reset();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Reset()
            {
                includeCount = 0;
                excludeCount = 0;
#if DEBUG && !DCFAECS_NO_SANITIZE_CHECKS
                _built = false;
#endif
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Mask Inc<T>() where T : struct
            {
                var poolId = _world.GetPool<T>().ID;
#if DEBUG && !DCFAECS_NO_SANITIZE_CHECKS
                if (_built) { throw new Exception("Cant change built mask."); }
                if (Array.IndexOf(include, poolId, 0, includeCount) != -1) { throw new Exception($"{typeof(T).Name} already in constraints list."); }
                if (Array.IndexOf(exclude, poolId, 0, excludeCount) != -1) { throw new Exception($"{typeof(T).Name} already in constraints list."); }
#endif
                if (includeCount == include.Length) { Array.Resize(ref include, includeCount << 1); }
                include[includeCount++] = poolId;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Mask Exc<T>() where T : struct
            {
                var poolId = _world.GetPool<T>().ID;
#if DEBUG && !DCFAECS_NO_SANITIZE_CHECKS
                if (_built) { throw new Exception("Cant change built mask."); }
                if (Array.IndexOf(include, poolId, 0, includeCount) != -1) { throw new Exception($"{typeof(T).Name} already in constraints list."); }
                if (Array.IndexOf(exclude, poolId, 0, excludeCount) != -1) { throw new Exception($"{typeof(T).Name} already in constraints list."); }
#endif
                if (excludeCount == exclude.Length) { Array.Resize(ref exclude, excludeCount << 1); }
                exclude[excludeCount++] = poolId;
                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public EcsFilter End(int capacity = 512)
            {
#if DEBUG && !LEOECSLITE_NO_SANITIZE_CHECKS
                if (_built) { throw new Exception("Cant change built mask."); }
                _built = true;
#endif
                Array.Sort(include, 0, includeCount);
                Array.Sort(exclude, 0, excludeCount);

                var (filter, isNew) = _world.GetFilterInternal(this, capacity);
                if (!isNew) { Recycle(); }
                return filter;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void Recycle()
            {
                Reset();
                if (_world._masksCount == _world._masks.Length)
                {
                    Array.Resize(ref _world._masks, _world._masksCount << 1);
                }
                _world._masks[_world._masksCount++] = this;
            }
        }
    }
}
