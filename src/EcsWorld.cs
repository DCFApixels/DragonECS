using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace DCFApixels.DragonECS
{
    public interface IWorldArchetype { }
    public struct DefaultWorld : IWorldArchetype { }

    public interface IEcsWorld
    {
        #region Properties
        //private float _timeScale;//TODO реализовать собсвенныйтайм склей для разных миров
        public bool IsEmpty { get; }
        public Type ArchetypeType { get; }
        public int ID { get; }
        #endregion

        #region Methods
        public EcsPool<T> GetPool<T>() where T : struct;
        public EcsFilter GetFilter<TInc>() where TInc : struct, IInc;
        public EcsFilter GetFilter<TInc, TExc>() where TInc : struct, IInc where TExc : struct, IExc;
        public ent NewEntity();
        public void DelEntity(int entityID);
        public void Destroy();

        public bool IsMaskCompatible(EcsMask mask, int entity);
        public bool IsMaskCompatibleWithout(EcsMask mask, int entity, int otherPoolID);

        internal void OnEntityComponentAdded(int entityID, int changedPoolID);
        internal void OnEntityComponentRemoved(int entityID, int changedPoolID);
        #endregion
    }

    public static class IEcsWorldExt
    {
        public static void DelEntity(this IEcsWorld self, ent entity)
        {
            self.DelEntity(entity.id);
        }
    }

    public abstract class EcsWorld
    {
        internal static IEcsWorld[] Worlds = new IEcsWorld[8];
        private static IntDispenser _worldIdDispenser = new IntDispenser(1);

        public readonly short id;

        public EcsWorld()
        {
            id = (short)_worldIdDispenser.GetFree();
            Worlds[id] = (IEcsWorld)this;
        }
    }

    public sealed class EcsWorld<TArchetype> : EcsWorld, IEcsWorld
        where TArchetype : IWorldArchetype
    {
        private IntDispenser _entityDispenser;
        private EcsGroup _entities;

        private short[] _gens;
        private short[] _componentCounts;

        private IEcsPool[] _pools;
        private EcsNullPool _nullPool;

        private List<EcsFilter>[] _filtersByIncludedComponents;
        private List<EcsFilter>[] _filtersByExcludedComponents;

        private EcsFilter[] _filters;

        #region Properties
        public bool IsEmpty => _entities.Count < 0;
        public Type ArchetypeType => typeof(TArchetype);
        public int ID => id;
        #endregion

        #region Constructors
        public EcsWorld()
        {
            _entityDispenser = new IntDispenser();
            _nullPool = new EcsNullPool(this);
            _pools = new IEcsPool[512];
            Array.Fill(_pools, _nullPool);
            _gens = new short[512];
            _filters = new EcsFilter[64];
            _entities = new EcsGroup(this, 512);
            _filtersByIncludedComponents = new List<EcsFilter>[16];
            _filtersByExcludedComponents = new List<EcsFilter>[16];
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
                Array.Fill(_pools, _nullPool, oldCapacity, oldCapacity - _pools.Length);

                Array.Resize(ref _filtersByIncludedComponents, ComponentType.Capacity);
                Array.Resize(ref _filtersByExcludedComponents, ComponentType.Capacity);
            }

            if (_pools[uniqueID] == _nullPool)
            {
                _pools[uniqueID] = new EcsPool<T>(this, ComponentType<T>.uniqueID, 512);
            }
            return (EcsPool<T>)_pools[uniqueID];
        }
      
        #endregion

        #region GetFilter

        public EcsFilter GetFilter<TInc>() where TInc : struct, IInc => GetFilter<TInc, Exc>();
        public EcsFilter GetFilter<TInc, TExc>() where TInc : struct, IInc where TExc : struct, IExc
        {
            var mask = EcsMaskMap<TArchetype>.GetMask<TInc, Exc>();

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
            foreach (var item in _entities)
            {
                if (IsMaskCompatible(mask, item.id))
                {
                    filter.Add(item.id);
                }
            }


            return filter;
        }
        #endregion

        #region IsMaskCompatible/IsMaskCompatibleWithout
        public bool IsMaskCompatible(EcsMask mask, int entity)
        {
#if DEBUG || !DRAGONECS_NO_SANITIZE_CHECKS
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
#if DEBUG || !DRAGONECS_NO_SANITIZE_CHECKS
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
                int poolID = mask.Exc[i];
                if (poolID != otherComponentID && _pools[poolID].Has(entity))
                    return false;
            }
            return true;
        }
        #endregion

        #region EntityChangedReact
        void IEcsWorld.OnEntityComponentAdded(int entityID, int componentID)
        {
            var includeList = _filtersByIncludedComponents[componentID];
            var excludeList = _filtersByExcludedComponents[componentID];

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
                    if (IsMaskCompatibleWithout(filter.Mask, entityID, componentID))
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

        #region NewEntity/DelEntity
        public ent NewEntity()
        {
            int entid = _entityDispenser.GetFree();
            _entities.Add(entid);
            if (_gens.Length < entid) Array.Resize(ref _gens, _gens.Length << 1);
            return new ent(entid, _gens[entid]++, id);
        }
        public void DelEntity(int entityID)
        {
            _entityDispenser.Release(entityID);
            _entities.Remove(entityID);
        }
        #endregion

        #region Destroy
        public void Destroy()
        {

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
#if DEBUG || !DRAGONECS_NO_SANITIZE_CHECKS
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
        #endregion
    }
}
