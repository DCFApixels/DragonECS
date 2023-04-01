using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public class EcsFilter
    {
        private readonly IEcsWorld _source;
        internal readonly EcsGroup entities;
        private readonly EcsMask _mask;

        #region Properties
        public IEcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source;
        }
        public EcsMask Mask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _mask;
        }
        public EcsReadonlyGroup Entities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => entities.Readonly;
        }
        public int EntitiesCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => entities.Count;
        }
        #endregion

        #region Constrcutors
        internal EcsFilter(IEcsWorld source, EcsMask mask, int capasity)
        {
            _source = source;
            _mask = mask;
            entities = new EcsGroup(source, capasity);
        }
        #endregion

        #region EntityChangedReact
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Add(int entityID)
        {
            entities.UncheckedAdd(entityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Remove(int entityID)
        {
            entities.UncheckedRemove(entityID);
        }
        #endregion

        #region GetEnumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup.Enumerator GetEnumerator() => entities.GetEnumerator();
        #endregion
    }
}
