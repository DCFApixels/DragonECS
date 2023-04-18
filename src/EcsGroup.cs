using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Profiling;
using UnityEngine;
using delayedOp = System.Int32;

namespace DCFApixels.DragonECS
{
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 8)]
    public readonly ref struct EcsReadonlyGroup
    {
        private readonly EcsGroup _source;
        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup(EcsGroup source) => _source = source;
        #endregion

        #region Properties
        public IEcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source.World;
        }
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source.Count;
        }
        public int CapacityDense
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source.CapacityDense;
        }
        public int CapacitySparce
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source.CapacitySparce;
        }
        public bool IsReleazed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source.IsReleazed;
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int entityID) => _source.Contains(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup.Enumerator GetEnumerator() => _source.GetEnumerator();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup Clone() => _source.Clone();
        #endregion

        #region Object
        public override string ToString()
        {
            if (_source != null)
                return _source.ToString();
            return "NULL";
        }
        public override int GetHashCode() => _source.GetHashCode();
        public override bool Equals(object obj) => obj is EcsGroup group && group == this;
        public bool Equals(EcsReadonlyGroup other) => _source == other._source;
        #endregion

        #region Internal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsGroup GetGroupInternal() => _source;

        #endregion

        #region operators
        public static bool operator ==(EcsReadonlyGroup a, EcsReadonlyGroup b) => a.Equals(b);
        public static bool operator ==(EcsReadonlyGroup a, EcsGroup b) => a.Equals(b);
        public static bool operator !=(EcsReadonlyGroup a, EcsReadonlyGroup b) => !a.Equals(b);
        public static bool operator !=(EcsReadonlyGroup a, EcsGroup b) => !a.Equals(b);
        #endregion
    }

    // индексация начинается с 1
    // _delayedOps это int[] для отложенных операций, хранятся отложенные операции в виде int значения, если старший бит = 0 то это опреация добавленияб если = 1 то это операция вычитания
    public unsafe class EcsGroup : IDisposable, IEquatable<EcsGroup>
    {
        private const int DEALAYED_ADD = 0;
        private const int DEALAYED_REMOVE = int.MinValue;

        private IEcsWorld _source;

        private int[] _dense;
        private int[] _sparse;

        private int _count;

        private delayedOp[] _delayedOps;
        private int _delayedOpsCount;

        private int _lockCount;

        private bool _isReleazed = true; 

        #region Properties
        public IEcsWorld World => _source;
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }
        public int CapacityDense
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dense.Length;
        }
        public int CapacitySparce
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _sparse.Length;
        }
        public EcsReadonlyGroup Readonly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new EcsReadonlyGroup(this);
        }
        public bool IsReleazed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isReleazed;
        }
        #endregion

        #region Constrcutors/Finalizer
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsGroup New(IEcsWorld world)
        {
            return world.GetGroupFromPool();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsGroup(IEcsWorld world, int denseCapacity = 64, int delayedOpsCapacity = 128)
        {
            _source = world;
            _source.RegisterGroup(this);
            _dense = new int[denseCapacity];
            _sparse = new int[world.Capacity];

            _delayedOps = new delayedOp[delayedOpsCapacity];

            _lockCount = 0;
            _delayedOpsCount = 0;
            _count = 0;
        }

        //защита от криворукости
        //перед сборкой мусора снова создает сильную ссылку и возвращает в пул
        //TODO переделат ьиил удалить, так как сборщик мусора просыпается только после 12к и более экземпляров, только тогда и вызывается финализатор, слишком жирно
        ~EcsGroup()
        {
            Release();
        }
        #endregion

        #region Contains
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int entityID)
        {
            return _sparse[entityID] > 0;
        }
        #endregion

        #region IndexOf
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int entityID)
        {
            return _sparse[entityID];
        }
        #endregion

        #region Add/Remove
        public void UncheckedAdd(int entityID) => AddInternal(entityID);
        public void Add(int entityID)
        {
            if (Contains(entityID)) return;
            AddInternal(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddInternal(int entityID)
        {
            //if (_lockCount > 0)
            //{
            //    AddDelayedOp(entityID, DEALAYED_ADD);
            //    return;
            //}
            AggressiveAdd(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AggressiveAdd(int entityID)
        {
            if (++_count >= _dense.Length)
                Array.Resize(ref _dense, _dense.Length << 1);
            _dense[_count] = entityID;
            _sparse[entityID] = _count;
        }

        public void UncheckedRemove(int entityID) => RemoveInternal(entityID);
        public void Remove(int entityID)
        {
            if (!Contains(entityID)) return;
            RemoveInternal(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RemoveInternal(int entityID)
        {
            //if (_lockCount > 0)
            //{
            //    AddDelayedOp(entityID, DEALAYED_REMOVE);
            //    return;
            //}
            AggressiveRemove(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AggressiveRemove(int entityID)
        {
            _dense[_sparse[entityID]] = _dense[_count];
            _sparse[_dense[_count--]] = _sparse[entityID];
            _sparse[entityID] = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddDelayedOp(int entityID, int isAddBitFlag)
        {
            if (_delayedOpsCount >= _delayedOps.Length)
            {
                Array.Resize(ref _delayedOps, _delayedOps.Length << 1);
            }
            _delayedOps[_delayedOpsCount++] = entityID | isAddBitFlag; // delayedOp = entityID add isAddBitFlag
        }
        #endregion

        #region Sort/Clear
        public void Sort()
        {
            int increment = 1;
            for (int i = 0; i < _dense.Length; i++)
            {
                if (_sparse[i] > 0)
                {
                    _sparse[i] = increment;
                    _dense[increment++] = i;
                }
            }
        }
        public void Clear()
        {
            _count = 0;
            //массив _dense нет смысла очищать, испольщуется только область от 1 до _count
            for (int i = 0; i < _sparse.Length; i++)
                _sparse[i] = 0;
        }
        #endregion

        #region CopyFrom/Clone
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(EcsReadonlyGroup group) => CopyFrom(group.GetGroupInternal());
        public void CopyFrom(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (group.World != _source) throw new ArgumentException("groupFilter.World != World");
#endif
            if(_count > 0)
                Clear();
            foreach (var item in group)
                AggressiveAdd(item.id);
        }
        public EcsGroup Clone()
        {
            EcsGroup result = _source.GetGroupFromPool();
            result.CopyFrom(this);
            return result;
        }
        #endregion

        #region Set operations
        /// <summary>as Union sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnionWith(EcsReadonlyGroup group) => UnionWith(group.GetGroupInternal());
        /// <summary>as Union sets</summary>
        public void UnionWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (_source != group.World) throw new ArgumentException("World != groupFilter.World");
#endif
            foreach (var item in group)
                if (!Contains(item.id))
                    AggressiveAdd(item.id);
        }

        /// <summary>as Except sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExceptWith(EcsReadonlyGroup group) => ExceptWith(group.GetGroupInternal());
        /// <summary>as Except sets</summary>
        public void ExceptWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (_source != group.World) throw new ArgumentException("World != groupFilter.World");
#endif
            foreach (var item in this)
                if (group.Contains(item.id))
                    AggressiveRemove(item.id);
        }

        /// <summary>as Intersect sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AndWith(EcsReadonlyGroup group) => AndWith(group.GetGroupInternal());
        /// <summary>as Intersect sets</summary>
        public void AndWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (World != group.World) throw new ArgumentException("World != groupFilter.World");
#endif
            foreach (var item in this)
                if (!group.Contains(item.id))
                    AggressiveRemove(item.id);
        }

        /// <summary>as Symmetric Except sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void XorWith(EcsReadonlyGroup group) => XorWith(group.GetGroupInternal());
        /// <summary>as Symmetric Except sets</summary>
        public void XorWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (_source != group.World) throw new ArgumentException("World != groupFilter.World");
#endif
            foreach (var item in group)
                if (Contains(item.id))
                    AggressiveRemove(item.id);
                else
                    AggressiveAdd(item.id);
        }
        #endregion

        #region Static Set operations
        /// <summary>as Except sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Except(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (a._source != b._source) throw new ArgumentException("a.World != b.World");
#endif
            EcsGroup result = a._source.GetGroupFromPool();
            foreach (var item in a)
                if (!b.Contains(item.id))
                    result.AggressiveAdd(item.id);
            a._source.ReleaseGroup(a);
            return result;
        }
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup And(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (a._source != b._source) throw new ArgumentException("a.World != b.World");
#endif
            EcsGroup result = a._source.GetGroupFromPool();
            foreach (var item in a)
                if (b.Contains(item.id))
                    result.AggressiveAdd(item.id);
            a._source.ReleaseGroup(a);
            return result;
        }
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Union(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (a._source != b._source) throw new ArgumentException("a.World != b.World");
#endif
            EcsGroup result = a._source.GetGroupFromPool();
            foreach (var item in a)
                result.AggressiveAdd(item.id);
            foreach (var item in a)
                result.Add(item.id);
            return result;
        }
        #endregion

        #region GetEnumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Unlock()
        {
#if (DEBUG && !DISABLE_DRAGONECS_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (_lockCount <= 0)
            {
                throw new Exception($"Invalid lock-unlock balance for {nameof(EcsGroup)}.");
            }
#endif
            if (--_lockCount <= 0)
            {
                for (int i = 0; i < _delayedOpsCount; i++)
                {
                    delayedOp op = _delayedOps[i];
                    if (op >= 0) //delayedOp.IsAdded
                        AggressiveAdd(op & int.MaxValue); //delayedOp.EcsEntity
                    else
                        AggressiveRemove(op & int.MaxValue); //delayedOp.EcsEntity
                }
            }
        }
        private ProfilerMarker _getEnumeratorReturn = new ProfilerMarker("EcsGroup.GetEnumerator");


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            // _lockCount++;
            return new Enumerator(this);
        }
        #endregion

        #region Enumerator
        public ref struct Enumerator// : IDisposable
        {
           // private readonly EcsGroup source;
            private readonly int[] _dense;
            private readonly int _count;
            private int _index;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(EcsGroup group)
            {
               // source = group;
                _dense = group._dense;
                _count = group.Count;
                _index = 0;
            }
            public ent Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new ent(_dense[_index]);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index <= _count && _count<_dense.Length; // <= потму что отсчет начинается с индекса 1 //_count < _dense.Length дает среде понять что проверки на выход за границы не нужны
            //[MethodImpl(MethodImplOptions.AggressiveInlining)]
            //public void Dispose() => source.Unlock();
        }
        #endregion

        #region Object
        public override string ToString()
        {
            return string.Join(", ", _dense.AsSpan(1, _count).ToArray());
        }
        public override bool Equals(object obj) => obj is EcsGroup group && Equals(group);
        public bool Equals(EcsReadonlyGroup other) => Equals(other.GetGroupInternal());
        public bool Equals(EcsGroup other)
        {
            if (other.Count != Count)
                return false;
            foreach (var item in other)
                if (!Contains(item.id))
                    return false;
            return true;
        }
        public override int GetHashCode() 
        {
            int hash = 0;
            foreach (var item in this)
                hash ^= 1 << (item.id % 32); //реализация от балды, так как не нужен, но фишка в том что хеш не учитывает порядок сущьностей, что явлется правильным поведением.
            return hash;
        }
        #endregion

        #region operators
        public static bool operator ==(EcsGroup a, EcsGroup b) => a.Equals(b);
        public static bool operator ==(EcsGroup a, EcsReadonlyGroup b) => a.Equals(b);
        public static bool operator !=(EcsGroup a, EcsGroup b) => !a.Equals(b);
        public static bool operator !=(EcsGroup a, EcsReadonlyGroup b) => !a.Equals(b);
        #endregion

        #region OnWorldResize
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnWorldResize(int newSize)
        {
            Array.Resize(ref _sparse, newSize);
        }
        #endregion

        #region IDisposable/Release
        public void Dispose()
        {
            Release();
        }
        public void Release()
        {
            _isReleazed = true;
            _source.ReleaseGroup(this);
        }
        #endregion
    }

    public static class EcsGroupExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Normalize<T>(this EcsGroup self, ref T[] array)
        {
            if (array.Length < self.CapacityDense) Array.Resize(ref array, self.CapacityDense);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Normalize<T>(this EcsReadonlyGroup self, ref T[] array)
        {
            if (array.Length < self.CapacityDense) Array.Resize(ref array, self.CapacityDense);
        }
    }
}
