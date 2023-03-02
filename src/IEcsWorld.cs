using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public interface IWorldArchetype { }
    public struct DefaultArchetype : IWorldArchetype { }

    public interface IEcsWorld
    {
        public const int MAX_WORLDS = byte.MaxValue; //Номер последнего мира 254
        public const int DEAD_WORLD_ID = byte.MaxValue; //Зарезервированный номер мира для мертвых сущьностей

        //private float _timeScale;//TODO реализовать собсвенныйтайм склей для разных миров

        #region Properties
        public ushort ID { get; internal set; }
        public bool IsAlive { get; }
        public bool IsEmpty { get; }
        public Type ArchetypeType { get; }
        #endregion

        public EcsPool<T> GetPool<T>() where T : struct;
        public EcsFilter GetFilter<TMask>() where TMask : MaskSingleton<TMask>;
        public ent NewEntity();
        public void Destroy();

        public bool IsMaskCompatible(Mask mask, int entity);
        public bool IsMaskCompatibleWithout(Mask mask, int entity, int otherPoolID);

        internal void OnEntityComponentAdded(int entityID, int changedPoolID);
        internal void OnEntityComponentRemoved(int entityID, int changedPoolID);
    }

    public class EcsWorld<TArchetype> : IEcsWorld
        where TArchetype : IWorldArchetype
    {
        private ushort _id = IEcsWorld.DEAD_WORLD_ID;

        private SparseSet _componentIDToPoolID;

        private SparseSet _entities = new SparseSet();
        private short[] _gens;

        private IEcsPool[] _pools;

        private List<EcsFilter>[] _filtersByIncludedComponents;
        private List<EcsFilter>[] _filtersByExcludedComponents;

        private EcsFilter[] _filters;

        #region Properties
        public ushort ID => _id;
        ushort IEcsWorld.ID { get => _id; set => _id = value; }

        public bool IsAlive => _id != IEcsWorld.DEAD_WORLD_ID;
        public bool IsEmpty => _entities.Count < 0;
        public Type ArchetypeType => typeof(TArchetype);

        #endregion

        #region Constructors
        public EcsWorld()
        {
            _pools = new IEcsPool[512];
            _entities = new SparseSet(512);
            _componentIDToPoolID = new SparseSet(512);
            _filters = new EcsFilter[512];
        }
        #endregion

        #region GetPool
        public EcsPool<T> GetPool<T>() where T : struct
        {
            int uniqueID = ComponentType<T>.uniqueID;

            if (uniqueID >= _pools.Length)
            {
                Array.Resize(ref _pools, ComponentType.capacity);
                Array.Resize(ref _filtersByIncludedComponents, ComponentType.capacity);
                Array.Resize(ref _filtersByExcludedComponents, ComponentType.capacity);
            }

            if (_pools[uniqueID] == null)
            {
                _pools[uniqueID] = new EcsPool<T>(this, 512);
            }
            return (EcsPool<T>)_pools[uniqueID];
        }
        #endregion

        #region GetFilter
        public EcsFilter GetFilter<TMask>() where TMask : MaskSingleton<TMask>
        {
            var bakedmask = BakedMask<TArchetype, TMask>.Instance;

            if (_filters.Length >= BakedMask<TArchetype>.capacity)
            {
                Array.Resize(ref _filters, BakedMask<TArchetype>.capacity);
            }

            if (_filters[bakedmask.UniqueID] == null)
            {
                _filters[bakedmask.UniqueID] = new EcsFilter(this, bakedmask, 512);
            }
            return _filters[bakedmask.UniqueID];
        }
        #endregion

        #region IsMaskCompatible/IsMaskCompatibleWithout
        public bool IsMaskCompatible(Mask mask, int entity)
        {
            BakedMask bakedMask = mask.GetBaked<TArchetype>();
            for (int i = 0, iMax = bakedMask.IncCount; i < iMax; i++)
            {
                if (!_pools[_componentIDToPoolID[bakedMask.Inc[i]]].Has(entity))
                {
                    return false;
                }
            }
            for (int i = 0, iMax = bakedMask.ExcCount; i < iMax; i++)
            {
                if (_pools[_componentIDToPoolID[bakedMask.Exc[i]]].Has(entity))
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsMaskCompatibleWithout(Mask mask, int entity, int otherPoolID)
        {
            BakedMask bakedMask = mask.GetBaked<TArchetype>();
            for (int i = 0, iMax = bakedMask.IncCount; i < iMax; i++)
            {
                int poolID = _componentIDToPoolID[bakedMask.Inc[i]];
                if (poolID == otherPoolID || !_pools[poolID].Has(entity))
                {
                    return false;
                }
            }
            for (int i = 0, iMax = bakedMask.ExcCount; i < iMax; i++)
            {
                int poolID = _componentIDToPoolID[bakedMask.Exc[i]];
                if (poolID != otherPoolID && _pools[poolID].Has(entity))
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region EntityChangedReact
        void IEcsWorld.OnEntityComponentAdded(int entityID, int changedPoolID)
        {
            var includeList = _filtersByIncludedComponents[changedPoolID];
            var excludeList = _filtersByExcludedComponents[changedPoolID];

            if (includeList != null)
            {
                foreach (var filter in includeList)
                {
                    if (IsMaskCompatible(filter.Mask.Mask, entityID))
                    {
                        filter.Add(entityID);
                    }
                }
            }
            if (excludeList != null)
            {
                foreach (var filter in excludeList)
                {
                    if (IsMaskCompatibleWithout(filter.Mask.Mask, entityID, changedPoolID))
                    {
                        filter.Remove(entityID);
                    }
                }
            }
        }

        void IEcsWorld.OnEntityComponentRemoved(int entityID, int changedPoolID)
        {
            var includeList = _filtersByIncludedComponents[changedPoolID];
            var excludeList = _filtersByExcludedComponents[changedPoolID];

            if (includeList != null)
            {
                foreach (var filter in includeList)
                {
                    if (IsMaskCompatible(filter.Mask.Mask, entityID))
                    {
                        filter.Remove(entityID);
                    }
                }
            }
            if (excludeList != null)
            {
                foreach (var filter in excludeList)
                {
                    if (IsMaskCompatibleWithout(filter.Mask.Mask, entityID, changedPoolID))
                    {
                        filter.Add(entityID);
                    }
                }
            }
        }
        #endregion

        #region NewEntity
        public Entity NewEntity()
        {
            int entityID = _entities.GetFree();
            _entities.Normalize(ref _gens);
            _gens[entityID]++;

            return new Entity(this, entityID);
        }
        #endregion

        #region Destroy
        public void Destroy()
        {
            _id = IEcsWorld.DEAD_WORLD_ID;
        }
        #endregion

        #region Utils
        internal abstract class ComponentType
        {
            internal static int increment = 1;
            internal static int capacity = 512;
        }
        internal sealed class ComponentType<T> : ComponentType
        {
            internal static int uniqueID;

            static ComponentType()
            {
                uniqueID = increment++;
#if DEBUG || DCFAECS_NO_SANITIZE_CHECKS
                if (increment + 1 > ushort.MaxValue)
                {
                    throw new EcsFrameworkException($"No more room for new component for this {typeof(TArchetype).FullName} IWorldArchetype");
                }
#endif

                if (increment > capacity)
                {
                    capacity <<= 1;
                }
            }
        }
        #endregion
    }
}
