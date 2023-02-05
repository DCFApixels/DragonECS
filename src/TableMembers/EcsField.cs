using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public readonly struct EcsField<T> : IEcsMemberCachePool<EcsField<T>, T>
    {
        private readonly EcsFieldPool<T> _pool;
        private readonly int _poolID;

        public EcsFieldPool<T> Pool => _pool;
        public int PoolID => _poolID;

        private EcsField(int poolID)
        {
            _pool = null;
            _poolID = poolID;
        }
        internal EcsField(EcsFieldPool<T> pool)
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
        public bool HasValue(int entityID)
        {
            return _pool.Has(entityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T New(int entityID)
        {
            return ref _pool.Add(entityID);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsField<T>(in int poolID) => new EcsField<T>(poolID);

        void IEcsMemberCachePool<EcsField<T>, T>.Inject(out EcsField<T> self, EcsFieldPool<T> pool)
        {
            self = new EcsField<T>(pool);
        }
    }

    public readonly struct EcsIncField<T> : IEcsMemberCachePool<EcsIncField<T>, T>
    {
        private readonly EcsFieldPool<T> _pool;
        private readonly int _poolID;

        public EcsFieldPool<T> Pool => _pool;
        public int PoolID => _poolID;

        private EcsIncField(int poolID)
        {
            _pool = null;
            _poolID = poolID;
        }
        internal EcsIncField(EcsFieldPool<T> pool)
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
        public static implicit operator EcsIncField<T>(in int poolID) => new EcsIncField<T>(poolID);

        void IEcsMemberCachePool<EcsIncField<T>, T>.Inject(out EcsIncField<T> self, EcsFieldPool<T> pool)
        {
            self = new EcsIncField<T>(pool);
        }
    }

    public struct EcsExcField<T> : IEcsTableMember
    {
        private readonly int _poolID;
        public int PoolID => _poolID;

        private EcsExcField(int poolID)
        {
            _poolID = poolID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsExcField<T>(in int poolID) => new EcsExcField<T>(poolID);
    }
}
