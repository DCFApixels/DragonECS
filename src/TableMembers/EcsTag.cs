using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public readonly struct EcsTag<T> : IEcsMemberCachePool<EcsTag<T>, T>
    {
        private readonly EcsFieldPool<T> _pool;
        private readonly int _poolID;

        public EcsFieldPool<T> Pool => _pool;
        public int PoolID => _poolID;

        private EcsTag(int poolID)
        {
            _pool = null;
            _poolID = poolID;
        }
        internal EcsTag(EcsFieldPool<T> pool)
        {
            _pool = pool;
            _poolID = pool.ID;
        }

        public void Add(int entityID)
        {
            _pool.Add(entityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsTag<T>(in int poolID) => new EcsTag<T>(poolID);

        void IEcsMemberCachePool<EcsTag<T>, T>.Inject(out EcsTag<T> self, EcsFieldPool<T> pool)
        {
            self = new EcsTag<T>(pool);
        }
    }
    public readonly struct EcsIncTag<T> : IEcsTableMember
    {
        private readonly int _poolID;
        public int PoolID => _poolID;

        private EcsIncTag(int poolID)
        {
            _poolID = poolID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsIncTag<T>(in int poolID) => new EcsIncTag<T>(poolID);
    }
    public readonly struct EcsExcTag<T> : IEcsTableMember
    {
        private readonly int _poolID;
        public int PoolID => _poolID;

        private EcsExcTag(int poolID)
        {
            _poolID = poolID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsExcTag<T>(in int poolID) => new EcsExcTag<T>(poolID);
    }
}
