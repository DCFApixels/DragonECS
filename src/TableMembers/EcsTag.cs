using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public readonly struct EcsTag : IEcsMemberCachePool<EcsTag, TagType>
    {
        private readonly EcsPool<TagType> _pool;
        private readonly int _poolID;

        public EcsPool<TagType> Pool => _pool;
        public int PoolID => _poolID;

        private EcsTag(int poolID)
        {
            _pool = null;
            _poolID = poolID;
        }
        internal EcsTag(EcsPool<TagType> pool)
        {
            _pool = pool;
            _poolID = pool.ID;
        }

        public void Add(int entityID)
        {
            _pool.Add(entityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsTag(in int poolID) => new EcsTag(poolID);

        void IEcsMemberCachePool<EcsTag, TagType>.Inject(out EcsTag self, EcsPool<TagType> pool)
        {
            self = new EcsTag(pool);
        }
    }
    public readonly struct EcsIncTag : IEcsMemberCachePool<EcsIncTag, TagType>
    {
        private readonly EcsPool<TagType> _pool;
        private readonly int _poolID;

        public EcsPool<TagType> Pool => _pool;
        public int PoolID => _poolID;

        private EcsIncTag(int poolID)
        {
            _pool = null;
            _poolID = poolID;
        }
        internal EcsIncTag(EcsPool<TagType> pool)
        {
            _pool = pool;
            _poolID = pool.ID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsIncTag(in int poolID) => new EcsIncTag(poolID);

        void IEcsMemberCachePool<EcsIncTag, TagType>.Inject(out EcsIncTag self, EcsPool<TagType> pool)
        {
            self = new EcsIncTag(pool);
        }
    }
    public readonly struct EcsExcTag : IEcsMemberCachePool<EcsExcTag, TagType>
    {
        private readonly EcsPool<TagType> _pool;
        private readonly int _poolID;

        public EcsPool<TagType> Pool => _pool;
        public int PoolID => _poolID;

        private EcsExcTag(int poolID)
        {
            _pool = null;
            _poolID = poolID;
        }
        internal EcsExcTag(EcsPool<TagType> pool)
        {
            _pool = pool;
            _poolID = pool.ID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsExcTag(in int poolID) => new EcsExcTag(poolID);

        void IEcsMemberCachePool<EcsExcTag, TagType>.Inject(out EcsExcTag self, EcsPool<TagType> pool)
        {
            self = new EcsExcTag(pool);
        }
    }
}
