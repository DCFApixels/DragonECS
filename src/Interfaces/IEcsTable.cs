using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public interface IEcsTable
    {
        #region Properties
        /// <summary>Table Archetype</summary>
        public Type ArchetypeType { get; }
        public int Count { get; }
        public int Capacity { get; }
        public EcsReadonlyGroup Entities => default;
        #endregion

        #region Methods
        public int GetComponentID<T>();
        public EcsPool<T> GetPool<T>() where T : struct;
        public ReadOnlySpan<IEcsPool> GetAllPools();
        public TQuery Where<TQuery>(out TQuery query) where TQuery : EcsQueryBase;
        public TQuery Select<TQuery>() where TQuery : EcsQueryBase;

        public bool IsMaskCompatible(EcsComponentMask mask, int entityID);

        public void Destroy();
        #endregion

        #region Internal Methods
        internal void RegisterGroup(EcsGroup group);
        internal EcsGroup GetGroupFromPool();
        internal void ReleaseGroup(EcsGroup group);
        #endregion
    }
}
