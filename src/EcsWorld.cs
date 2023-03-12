using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public interface IWorldArchetype { }
    public struct DefaultWorld : IWorldArchetype { }

    public interface IEcsWorld
    {
        //private float _timeScale;//TODO реализовать собсвенныйтайм склей для разных миров

        #region Properties
        public bool IsEmpty { get; }
        public Type ArchetypeType { get; }
        public int ID { get; }
        #endregion

        public EcsPool<T> GetPool<T>() where T : struct;
        public EcsFilter GetFilter<TInc>() where TInc : struct, IInc;
        public EcsFilter GetFilter<TInc, TExc>() where TInc : struct, IInc where TExc : struct, IExc;
        public ent NewEntity();
        public void Destroy();

        public bool IsMaskCompatible(Mask mask, int entity);
        public bool IsMaskCompatibleWithout(Mask mask, int entity, int otherPoolID);

        internal void OnEntityComponentAdded(int entityID, int changedPoolID);
        internal void OnEntityComponentRemoved(int entityID, int changedPoolID);
    }


    public abstract class EcsWorld
    {
        internal static IEcsWorld[] Worlds = new IEcsWorld[8];
        private static IntDispenser _worldIdDispenser = new IntDispenser();

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
            _pools = new IEcsPool[512];
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

        public EcsFilter GetFilter<TInc>() where TInc : struct, IInc
        {
            return GetFilterInternal<Mask<TInc>>();
        }
        public EcsFilter GetFilter<TInc, TExc>() where TInc : struct, IInc where TExc : struct, IExc
        {
            return GetFilterInternal<Mask<TInc, TExc>>();
        }
        private EcsFilter GetFilterInternal<TMask>() where TMask : Mask, new()
        {
            var bakedmask = BakedMask<TArchetype, TMask>.Instance;

            if (_filters.Length >= BakedMask<TArchetype>.capacity)
            {
                Array.Resize(ref _filters, BakedMask<TArchetype>.capacity);
            }

            if (_filters[bakedmask.UniqueID] == null)
            {
                _filters[bakedmask.UniqueID] = NewFilter(bakedmask);
            }
            return _filters[bakedmask.UniqueID];
        }

        private EcsFilter NewFilter(BakedMask mask, int capacirty = 512)
        {
            var newFilter = new EcsFilter(this, mask, capacirty);

            for (int i = 0; i < mask.IncCount; i++)
            {
                int poolid = mask.Inc[i];
                var list = _filtersByIncludedComponents[poolid];
                if (list == null)
                {
                    list = new List<EcsFilter>(8);
                    _filtersByIncludedComponents[poolid] = list;
                }
                list.Add(newFilter);
            }

            for (int i = 0; i < mask.ExcCount; i++)
            {
                int poolid = mask.Exc[i];
                var list = _filtersByExcludedComponents[poolid];
                if (list == null)
                {
                    list = new List<EcsFilter>(8);
                    _filtersByExcludedComponents[poolid] = list;
                }
                list.Add(newFilter);
            }

            return newFilter;
        }
        #endregion

        #region IsMaskCompatible/IsMaskCompatibleWithout
        public bool IsMaskCompatible(Mask mask, int entity)
        {
            BakedMask bakedMask = mask.GetBaked<TArchetype>();
            for (int i = 0, iMax = bakedMask.IncCount; i < iMax; i++)
            {
                if (!_pools[bakedMask.Inc[i]].Has(entity))
                {
                    return false;
                }
            }
            for (int i = 0, iMax = bakedMask.ExcCount; i < iMax; i++)
            {
                if (_pools[bakedMask.Exc[i]].Has(entity))
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
                int poolID = bakedMask.Inc[i];
                if (poolID == otherPoolID || !_pools[poolID].Has(entity))
                {
                    return false;
                }
            }
            for (int i = 0, iMax = bakedMask.ExcCount; i < iMax; i++)
            {
                int poolID = bakedMask.Exc[i];
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
        public ent NewEntity()
        {
            int entid = _entityDispenser.GetFree();
            if(_gens.Length < entid) Array.Resize(ref _gens, _gens.Length << 1);
            return new ent(entid, _gens[entid]++, id);
        }
        #endregion

        #region Destroy
        public void Destroy()
        {

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
