using System;

namespace DCFApixels.DragonECS
{
    public interface IEcsTable
    {
        #region Properties
        /// <summary>Table Archetype</summary>
        public Type Archetype { get; }
        public int Count { get; }
        public int Capacity { get; }
        public EcsReadonlyGroup Entities => default;
        #endregion

        #region Methods
        public int GetComponentID<T>();
        public TPool GetPool<TComponent, TPool>() where TComponent : struct where TPool : EcsPoolBase<TComponent>, new();
        public ReadOnlySpan<EcsPoolBase> GetAllPools();
        public TQuery Select<TQuery>() where TQuery : EcsQueryBase;
        public TQuery Where<TQuery>(out TQuery query) where TQuery : EcsQuery;
      //  public TQuery Join<TQuery>(out TQuery query) where TQuery : EcsJoinQueryBase;

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
