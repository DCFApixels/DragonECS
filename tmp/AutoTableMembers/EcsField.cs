using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public readonly struct EcsField<T> : IEcsMemberCachePool<EcsField<T>, T>
        where T :struct
    {
        private readonly EcsPool<T> _pool;
        private readonly int _poolID;

        public EcsPool<T> Pool => _pool;
        public int PoolID => _poolID;

        private EcsField(int poolID)
        {
            _pool = null;
            _poolID = poolID;
        }
        internal EcsField(EcsPool<T> pool)
        {
            _pool = pool;
            _poolID = pool.ID;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref T this[int entityID]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _pool[entityID];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            return _pool.Has(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Add(int entityID)
        {
            return ref _pool.Add(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int entityID)
        {
            _pool.Del(entityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsField<T>(in int poolID) => new EcsField<T>(poolID);

        void IEcsMemberCachePool<EcsField<T>, T>.Inject(out EcsField<T> self, EcsPool<T> pool)
        {
            self = new EcsField<T>(pool);
        }
    }

    public readonly struct EcsIncField<T> : IEcsMemberCachePool<EcsIncField<T>, T>
        where T :struct
    {
        private readonly EcsPool<T> _pool;
        private readonly int _poolID;

        public EcsPool<T> Pool => _pool;
        public int PoolID => _poolID;

        private EcsIncField(int poolID)
        {
            _pool = null;
            _poolID = poolID;
        }
        internal EcsIncField(EcsPool<T> pool)
        {
            _pool = pool;
            _poolID = pool.ID;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref T this[int entityID]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _pool[entityID];
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Del(int entityID)
        {
            _pool.Del(entityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsIncField<T>(in int poolID) => new EcsIncField<T>(poolID);

        void IEcsMemberCachePool<EcsIncField<T>, T>.Inject(out EcsIncField<T> self, EcsPool<T> pool)
        {
            self = new EcsIncField<T>(pool);
        }
    }

    public struct EcsExcField<T> : IEcsMemberCachePool<EcsExcField<T>, T>
        where T :struct
    {
        private readonly EcsPool<T> _pool;
        private readonly int _poolID;

        public EcsPool<T> Pool => _pool;
        public int PoolID => _poolID;

        private EcsExcField(int poolID)
        {
            _pool = null;
            _poolID = poolID;
        }
        internal EcsExcField(EcsPool<T> pool)
        {
            _pool = pool;
            _poolID = pool.ID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Add(int entityID)
        {
            return ref _pool.Add(entityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsExcField<T>(in int poolID) => new EcsExcField<T>(poolID);

        void IEcsMemberCachePool<EcsExcField<T>, T>.Inject(out EcsExcField<T> self, EcsPool<T> pool)
        {
            self = new EcsExcField<T>(pool);
        }
    }
}
