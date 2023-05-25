using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
    using static EcsGroup.ThrowHalper;
#endif

    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 8)]
    public readonly ref struct EcsReadonlyGroup
    {
        private readonly EcsGroup _source;

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup(EcsGroup source) => _source = source;
        #endregion

        #region Properties
        public bool IsNull => _source == null;
        public EcsWorld World
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
            get => _source.IsReleased;
        }
        public int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source[index];
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) => _source.Has(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup.Enumerator GetEnumerator() => _source.GetEnumerator();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup Clone() => _source.Clone();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int First() => _source.First();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Last() => _source.Last();
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

    public unsafe class EcsGroup : IDisposable, IEquatable<EcsGroup>
    {
        private EcsWorld _source;
        private int[] _dense;
        private int[] _sparse;
        private int _count;
        private bool _isReleased = true;

        #region Properties
        public EcsWorld World => _source;
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
        public bool IsReleased
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _isReleased;
        }
        public int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
                if (index < 0 || index >= Count) ThrowArgumentOutOfRange();
#endif
                return _dense[index];
            }
        }
        #endregion

        #region Constrcutors/Finalizer
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsGroup New(EcsWorld world)
        {
            return world.GetGroupFromPool();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsGroup(EcsWorld world, int denseCapacity = 64)
        {
            _source = world;
            _source.RegisterGroup(this);
            _dense = new int[denseCapacity];
            _sparse = new int[world.Capacity];

            _count = 0;
        }
        #endregion

        #region Has
        //TODO переименовать в Has
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
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
            if (Has(entityID)) return;
            AddInternal(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddInternal(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (Has(entityID)) ThrowAlreadyContains(entityID);
#endif
            if (++_count >= _dense.Length)
                Array.Resize(ref _dense, _dense.Length << 1);
            _dense[_count] = entityID;
            _sparse[entityID] = _count;
        }

        public void UncheckedRemove(int entityID) => RemoveInternal(entityID);
        public void Remove(int entityID)
        {
            if (!Has(entityID)) return;
            RemoveInternal(entityID);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RemoveInternal(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!Has(entityID)) ThrowDoesNotContain(entityID);
#endif
            _dense[_sparse[entityID]] = _dense[_count];
            _sparse[_dense[_count--]] = _sparse[entityID];
            _sparse[entityID] = 0;
        }

        public void RemoveUnusedEntityIDs()
        {
            foreach (var e in this)
            {
                if (!_source.IsUsed(e))
                    RemoveInternal(e);
            }
        }
        #endregion

        #region Sort/Clear
        //public void Sort() { } //TODO прошлай реализация сортировки не удачная, так как в dense могут храниться занчения больше чем dense.Length
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
            if (group.World != _source) throw new ArgumentException("groupFilter.WorldIndex != WorldIndex");
#endif
            if(_count > 0)
                Clear();
            foreach (var item in group)
                AddInternal(item);
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
            if (_source != group.World) throw new ArgumentException("WorldIndex != groupFilter.WorldIndex");
#endif
            foreach (var item in group)
                if (!Has(item))
                    AddInternal(item);
        }

        /// <summary>as Except sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExceptWith(EcsReadonlyGroup group) => ExceptWith(group.GetGroupInternal());
        /// <summary>as Except sets</summary>
        public void ExceptWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (_source != group.World) throw new ArgumentException("WorldIndex != groupFilter.WorldIndex");
#endif
            foreach (var item in this)
                if (group.Has(item))
                    RemoveInternal(item);
        }

        /// <summary>as Intersect sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AndWith(EcsReadonlyGroup group) => AndWith(group.GetGroupInternal());
        /// <summary>as Intersect sets</summary>
        public void AndWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (World != group.World) throw new ArgumentException("WorldIndex != groupFilter.WorldIndex");
#endif
            foreach (var item in this)
                if (!group.Has(item))
                    RemoveInternal(item);
        }

        /// <summary>as Symmetric Except sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void XorWith(EcsReadonlyGroup group) => XorWith(group.GetGroupInternal());
        /// <summary>as Symmetric Except sets</summary>
        public void XorWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (_source != group.World) throw new ArgumentException("WorldIndex != groupFilter.WorldIndex");
#endif
            foreach (var item in group)
                if (Has(item))
                    RemoveInternal(item);
                else
                    AddInternal(item);
        }
        #endregion

        #region Static Set operations
        /// <summary>as Except sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Except(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (a._source != b._source) throw new ArgumentException("a.WorldIndex != b.WorldIndex");
#endif
            EcsGroup result = a._source.GetGroupFromPool();
            foreach (var item in a)
                if (!b.Has(item))
                    result.AddInternal(item);
            a._source.ReleaseGroup(a);
            return result;
        }
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup And(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (a._source != b._source) throw new ArgumentException("a.WorldIndex != b.WorldIndex");
#endif
            EcsGroup result = a._source.GetGroupFromPool();
            foreach (var item in a)
                if (b.Has(item))
                    result.AddInternal(item);
            a._source.ReleaseGroup(a);
            return result;
        }
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Union(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (a._source != b._source) throw new ArgumentException("a.WorldIndex != b.WorldIndex");
#endif
            EcsGroup result = a._source.GetGroupFromPool();
            foreach (var item in a)
                result.AddInternal(item);
            foreach (var item in a)
                result.Add(item);
            return result;
        }
        #endregion

        #region GetEnumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
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
                _count = group._count;
                _index = 0;
            }
            public int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _dense[_index];
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
            if (ReferenceEquals(other, null))
                return false;
            if (other.Count != Count)
                return false;
            foreach (var item in other)
                if (!Has(item))
                    return false;
            return true;
        }
        public override int GetHashCode() 
        {
            int hash = 0;
            foreach (var item in this)
                hash ^= 1 << (item % 32); //реализация от балды, так как не нужен, но фишка в том что хеш не учитывает порядок сущьностей, что явлется правильным поведением.
            return hash;
        }
        #endregion

        #region OtherMethods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int First() => this[0];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Last() => this[_count - 1];

        #endregion

        #region operators
        private static bool StaticEquals(EcsGroup a, EcsReadonlyGroup b) => StaticEquals(a, b.GetGroupInternal());
        private static bool StaticEquals(EcsGroup a, EcsGroup b)
        {
            if (ReferenceEquals(a, null))
                return false;
            return a.Equals(b);
        }
        public static bool operator ==(EcsGroup a, EcsGroup b) => StaticEquals(a, b);
        public static bool operator ==(EcsGroup a, EcsReadonlyGroup b) => StaticEquals(a, b);
        public static bool operator !=(EcsGroup a, EcsGroup b) => !StaticEquals(a, b);
        public static bool operator !=(EcsGroup a, EcsReadonlyGroup b) => !StaticEquals(a, b);
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
            _isReleased = true;
            _source.ReleaseGroup(this);
        }
        #endregion

        #region ThrowHalper
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
        internal static class ThrowHalper
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowAlreadyContains(int entityID) => throw new EcsFrameworkException($"This group already contains entity {entityID}.");
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowArgumentOutOfRange() => throw new ArgumentOutOfRangeException($"index is less than 0 or is equal to or greater than Count.");
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowDoesNotContain(int entityID) => throw new EcsFrameworkException($"This group does not contain entity {entityID}.");
        }
#endif
        #endregion
    }

    #region Extensions
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
    #endregion
}
