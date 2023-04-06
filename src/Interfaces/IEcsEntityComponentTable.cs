using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public interface IEcsEntityComponentTable
    {
        #region Properties
        public bool IsEmpty { get; }
        public Type ArchetypeType { get; }
        //public int ID { get; }
        public int EntitesCount { get; }
        public int EntitesCapacity { get; }
        #endregion

        #region GetterMethods
        public ReadOnlySpan<IEcsPool> GetAllPools();

        #endregion

        #region Methods
        public EcsPool<T> GetPool<T>() where T : struct;
        public EcsPool<T> UncheckedGetPool<T>() where T : struct;

        public EcsFilter Entities<TComponent>() where TComponent : struct;
        public EcsFilter Filter<TInc>() where TInc : struct, IInc;
        public EcsFilter Filter<TInc, TExc>() where TInc : struct, IInc where TExc : struct, IExc;

        public ent NewEntity();
        public void DelEntity(ent entity);
        public bool EntityIsAlive(int entityID, short gen);
        public ent GetEntity(int entityID);
        public void Destroy();

        public bool IsMaskCompatible<TInc>(int entity) where TInc : struct, IInc;
        public bool IsMaskCompatible<TInc, TExc>(int entity) where TInc : struct, IInc where TExc : struct, IExc;
        public bool IsMaskCompatible(EcsMask mask, int entity);
        public bool IsMaskCompatibleWithout(EcsMask mask, int entity, int otherPoolID);

        internal void OnEntityComponentAdded(int entityID, int changedPoolID);
        internal void OnEntityComponentRemoved(int entityID, int changedPoolID);

        public int GetComponentID<T>();
        internal void RegisterGroup(EcsGroup group);
        #endregion
    }
}
