using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public interface IEcsFilter
    {
        #region Properties
        public IEcsWorld World { get; }
        public EcsMask Mask { get; }
        public IEcsReadonlyGroup Entities { get; }
        public int EntitiesCount { get; }
        #endregion
    }

    public class EcsFilter : IEcsFilter
    {
        private readonly IEcsWorld _source;
        private readonly EcsGroup _entities;
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
        public IEcsReadonlyGroup Entities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _entities;
        }
        public int EntitiesCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _entities.Count;
        }
        #endregion

        #region Constrcutors
        internal EcsFilter(IEcsWorld source, EcsMask mask, int capasity)
        {
            _source = source;
            _mask = mask;
            _entities = new EcsGroup(source, capasity);
        }
        #endregion

        #region EntityChangedReact
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Add(int entityID)
        {
            _entities.Add(entityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Remove(int entityID)
        {
            _entities.Remove(entityID);
        }
        #endregion

        #region GetEnumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup.Enumerator GetEnumerator() => _entities.GetEnumerator();
        #endregion
    }
}
