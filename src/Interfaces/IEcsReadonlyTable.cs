using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public interface IEcsReadonlyTable
    {
        #region Properties
        /// <summary>Table Archetype</summary>
        public Type ArchetypeType { get; }
        public int Count { get; }
        public int Capacity { get; }
        #endregion

        #region Methods
        public EcsPool<T> GetPool<T>() where T : struct;
        public ReadOnlySpan<EcsPool> GetAllPools();
        public TQuery Query<TQuery>(out TQuery query) where TQuery : EcsQueryBase;

        public int GetComponentID<T>();
        public bool IsMaskCompatible<TInc, TExc>(int entityID) where TInc : struct, IInc where TExc : struct, IExc;
        public bool IsMaskCompatible(EcsComponentMask mask, int entityID);
        public bool IsMaskCompatibleWithout(EcsComponentMask mask, int entity, int componentID);
        #endregion

        #region Internal Methods
        internal void RegisterGroup(EcsGroup group);
        #endregion
    }

    public static class IEcsReadonlyTableExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMaskCompatible<TInc>(this IEcsReadonlyTable self, int entityID) where TInc : struct, IInc
        {
            return self.IsMaskCompatible<TInc, Exc>(entityID);
        }
    }
}
