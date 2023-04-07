using System;

namespace DCFApixels.DragonECS
{
    public interface IEcsReadonlyTable
    {
        #region Properties
        /// <summary>Table Archetype</summary>
        public Type ArchetypeType { get; }
        internal int Count { get; }
        internal int Capacity { get; }
        #endregion

        #region Methods
        public ReadOnlySpan<IEcsPool> GetAllPools();
        public int GetComponentID<T>();

        public EcsPool<T> GetPool<T>() where T : struct;

        public bool IsMaskCompatible<TInc>(int entity) where TInc : struct, IInc;
        public bool IsMaskCompatible<TInc, TExc>(int entity) where TInc : struct, IInc where TExc : struct, IExc;
        public bool IsMaskCompatible(EcsComponentMask mask, int entity);
        public bool IsMaskCompatibleWithout(EcsComponentMask mask, int entity, int otherPoolID);
        #endregion

        #region Internal Methods
        internal void OnEntityComponentAdded(int entityID, int changedPoolID);
        internal void OnEntityComponentRemoved(int entityID, int changedPoolID);
        internal void RegisterGroup(EcsGroup group);
        #endregion
    }
}
