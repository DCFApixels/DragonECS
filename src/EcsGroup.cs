using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
using static DCFApixels.DragonECS.EcsGroup.ThrowHelper;
#endif

namespace DCFApixels.DragonECS
{
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 8)]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
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
        public int IndexOf(int entityID) => _source.IndexOf(entityID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup.Enumerator GetEnumerator() => _source.GetEnumerator();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup Clone() => _source.Clone();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] Bake() => _source.Bake();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Bake(ref int[] entities) => _source.Bake(ref entities);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bake(List<int> entities) => _source.Bake(entities);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<int> ToSpan() => _source.ToSpan();
        public ReadOnlySpan<int> ToSpan(int start, int length) => _source.ToSpan(start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int First() => _source.First();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Last() => _source.Last();
        #endregion

        #region Object
        public override string ToString() => _source != null ? _source.ToString() : "NULL";
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

        #region DebuggerProxy
        internal class DebuggerProxy : EcsGroup.DebuggerProxy
        {
            public DebuggerProxy(EcsReadonlyGroup group) : base(group._source) { }
        }
        #endregion
    }

    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public unsafe class EcsGroup : IDisposable, IEquatable<EcsGroup>, IEnumerable<int>
    {
        private EcsWorld _source;
        private int[] _dense;
        private int[] _sparse;
        private int _count;
        internal bool _isReleased = true;

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
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (index < 0 || index >= Count) ThrowArgumentOutOfRange();
#endif
                return _dense[++index];
            }
        }
        #endregion

        #region Constrcutors/Dispose
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsGroup New(EcsWorld world)
        {
            return world.GetFreeGroup();
        }
        internal EcsGroup(EcsWorld world, int denseCapacity = 64)
        {
            _source = world;
            _source.RegisterGroup(this);
            _dense = new int[denseCapacity];
            _sparse = new int[world.Capacity];

            _count = 0;
        }
        public void Dispose() => _source.ReleaseGroup(this);
        #endregion

        #region Has/IndexOf
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            return _sparse[entityID] > 0;
        }
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
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
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
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
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

        #region Clear
        public void Clear()
        {
            _count = 0;
            //массив _dense нет смысла очищать
            for (int i = 0; i < _sparse.Length; i++)
                _sparse[i] = 0;
        }
        #endregion

        #region CopyFrom/Clone/Bake/ToSpan
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(EcsReadonlyGroup group) => CopyFrom(group.GetGroupInternal());
        public void CopyFrom(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (group.World != _source) throw new ArgumentException("groupFilter.WorldIndex != WorldIndex");
#endif
            if (_count > 0)
                Clear();
            foreach (var item in group)
                AddInternal(item);
        }
        public EcsGroup Clone()
        {
            EcsGroup result = _source.GetFreeGroup();
            result.CopyFrom(this);
            return result;
        }
        public int[] Bake()
        {
            int[] result = new int[_count];
            Array.Copy(_dense, 1, result, 0, _count);
            return result;
        }
        public int Bake(ref int[] entities)
        {
            if(entities.Length < _count)
                entities = new int[_count];
            Array.Copy(_dense, 1, entities, 0, _count);
            return _count;
        }
        public void Bake(List<int> entities)
        {
            entities.Clear();
            foreach (var e in this)
                entities.Add(e);
        }
        public ReadOnlySpan<int> ToSpan() => new ReadOnlySpan<int>(_dense, 0, _count);
        public ReadOnlySpan<int> ToSpan(int start, int length)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (start + length > _count) ThrowArgumentOutOfRangeException();
#endif
            return new ReadOnlySpan<int>(_dense, start, length);
        }
        #endregion

        #region Set operations
        /// <summary>as Union sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnionWith(EcsReadonlyGroup group) => UnionWith(group.GetGroupInternal());
        /// <summary>as Union sets</summary>
        public void UnionWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) ThrowArgumentDifferentWorldsException();
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
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) ThrowArgumentDifferentWorldsException();
#endif
            foreach (var item in this)
                if (group.Has(item))
                    RemoveInternal(item);
        }

        /// <summary>as Intersect sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IntersectWith(EcsReadonlyGroup group) => IntersectWith(group.GetGroupInternal());
        /// <summary>as Intersect sets</summary>
        public void IntersectWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (World != group.World) ThrowArgumentDifferentWorldsException();
#endif
            foreach (var item in this)
                if (!group.Has(item))
                    RemoveInternal(item);
        }

        /// <summary>as Symmetric Except sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SymmetricExceptWith(EcsReadonlyGroup group) => SymmetricExceptWith(group.GetGroupInternal());
        /// <summary>as Symmetric Except sets</summary>
        public void SymmetricExceptWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) ThrowArgumentDifferentWorldsException();
#endif
            foreach (var item in group)
                if (Has(item))
                    RemoveInternal(item);
                else
                    AddInternal(item);
        }
        public void Inverse()
        {
            foreach (var item in _source.Entities)
                if (Has(item))
                    RemoveInternal(item);
                else
                    AddInternal(item);
        }
        #endregion

        #region Static Set operations
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Union(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (a._source != b._source) ThrowArgumentDifferentWorldsException();
#endif
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var item in a)
                result.AddInternal(item);
            foreach (var item in b)
                result.Add(item);
            return result;
        }
        /// <summary>as Except sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Except(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (a._source != b._source) ThrowArgumentDifferentWorldsException();
#endif
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var item in a)
                if (!b.Has(item))
                    result.AddInternal(item);
            return result;
        }
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Intersect(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (a._source != b._source) ThrowArgumentDifferentWorldsException();
#endif
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var item in a)
                if (b.Has(item))
                    result.AddInternal(item);
            return result;
        }

        /// <summary>as Symmetric Except sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup SymmetricExcept(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (a._source != b._source) ThrowArgumentDifferentWorldsException();
#endif
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var item in a)
                if (!b.Has(item))
                    result.AddInternal(item);
            foreach (var item in b)
                if (!a.Has(item))
                    result.AddInternal(item);
            return result;
        }

        public static EcsGroup Inverse(EcsGroup a)
        {
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var item in a._source.Entities)
                if (!a.Has(item))
                    result.AddInternal(item);
            return result;
        }
        #endregion

        #region Enumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return _dense[i];
        }
        IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return _dense[i];
        }
        public ref struct Enumerator
        {
            private readonly int[] _dense;
            private readonly int _count;
            private int _index;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(EcsGroup group)
            {
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
            public bool MoveNext() => ++_index <= _count && _count < _dense.Length; // <= потму что отсчет начинается с индекса 1 //_count < _dense.Length дает среде понять что проверки на выход за границы не нужны
        }
        #endregion

        #region Object
        public override string ToString() => string.Join(", ", _dense.Cast<string>(), 0, _count);
        public override bool Equals(object obj) => obj is EcsGroup group && Equals(group);
        public bool Equals(EcsReadonlyGroup other) => Equals(other.GetGroupInternal());
        public bool Equals(EcsGroup other)
        {
            if (other is null || other.Count != Count)
                return false;
            foreach (var e in other)
                if (!Has(e))
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
            if (a is null) return false;
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

        #region ThrowHalper
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
        internal static class ThrowHelper
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowAlreadyContains(int entityID) => throw new EcsFrameworkException($"This group already contains entity {entityID}.");
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowArgumentOutOfRange() => throw new ArgumentOutOfRangeException($"index is less than 0 or is equal to or greater than Count.");
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowDoesNotContain(int entityID) => throw new EcsFrameworkException($"This group does not contain entity {entityID}.");
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowArgumentOutOfRangeException() => throw new ArgumentOutOfRangeException();
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowArgumentDifferentWorldsException() => throw new ArgumentException("The groups belong to different worlds.");
        }
#endif
        #endregion

        #region DebuggerProxy
        internal class DebuggerProxy
        {
            private EcsGroup _group;
            public EcsWorld World => _group.World;
            public bool IsReleased => _group.IsReleased;
            public entlong[] Entities
            {
                get
                {
                    entlong[] result = new entlong[_group.Count];
                    int i = 0;
                    foreach (var e in _group)
                        result[i++] = _group.World.GetEntityLong(e);
                    return result;
                }
            }
            public int Count => _group.Count;
            public int CapacityDense => _group.CapacityDense;
            public int CapacitySparce => _group.CapacitySparce;

            public override string ToString() => _group.ToString();
            public DebuggerProxy(EcsGroup group) => _group = group;
        }
        #endregion
    }
}