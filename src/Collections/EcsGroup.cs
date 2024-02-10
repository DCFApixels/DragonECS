using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    //_dense заполняется с индекса 1
    //в операциях изменяющих состояние группы нельзя итерироваться по this, либо осторожно учитывать этот момент
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 8)]
    [DebuggerTypeProxy(typeof(EcsGroup.DebuggerProxy))]
    public readonly ref struct EcsReadonlyGroup
    {
        private readonly EcsGroup _source;

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup(EcsGroup source) => _source = source;
        #endregion

        #region Properties
        public bool IsNull => _source == null;
        public int WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source.WorldID;
        }
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
        public EcsSpan ToSpan() => _source.ToSpan();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ToSpan(int start, int length) => _source.ToSpan(start, length);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup.LongsIterator GetLongs() => _source.GetLongs();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int First() => _source.First();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Last() => _source.Last();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsReadonlyGroup group) => _source.SetEquals(group._source);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsGroup group) => _source.SetEquals(group);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsSpan span) => _source.SetEquals(span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsReadonlyGroup group) => _source.Overlaps(group._source);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsGroup group) => _source.Overlaps(group);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsSpan span) => _source.Overlaps(span);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(EcsReadonlyGroup group) => _source.IsSubsetOf(group._source);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(EcsGroup group) => _source.IsSubsetOf(group);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(EcsReadonlyGroup group) => _source.IsSupersetOf(group._source);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(EcsGroup group) => _source.IsSupersetOf(group);
        #endregion

        #region Internal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsGroup GetSource_Internal() => _source;
        #endregion

        #region Other
        public override string ToString()
        {
            return _source != null ? _source.ToString() : "NULL";
        }
#pragma warning disable CS0809 // Устаревший член переопределяет неустаревший член
        [Obsolete("Equals() on EcsGroup will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new NotSupportedException();
        [Obsolete("GetHashCode() on EcsGroup will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new NotSupportedException();
#pragma warning restore CS0809 // Устаревший член переопределяет неустаревший член

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsSpan(EcsReadonlyGroup a) => a.ToSpan();
        //internal class DebuggerProxy : EcsGroup.DebuggerProxy
        //{
        //    public DebuggerProxy(EcsReadonlyGroup group) : base(group._source) { }
        //}
        #endregion
    }

    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public unsafe class EcsGroup : IDisposable, IEnumerable<int>, IEntitiesCollection
    {
        private EcsWorld _source;
        private int[] _dense;
        private int[] _sparse;
        private int _count;
        internal bool _isReleased = true;

        #region Properties
        public int WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source.id;
        }
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source;
        }
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
                if (index < 0 || index >= Count) Throw.ArgumentOutOfRange();
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
        public void AddUnchecked(int entityID) => AddInternal(entityID);
        public bool Add(int entityID)
        {
            if (Has(entityID))
                return false;
            AddInternal(entityID);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddInternal(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (Has(entityID)) Throw.Group_AlreadyContains(entityID);
#endif
            if (++_count >= _dense.Length)
                Array.Resize(ref _dense, _dense.Length << 1);
            _dense[_count] = entityID;
            _sparse[entityID] = _count;
        }

        public void RemoveUnchecked(int entityID) => RemoveInternal(entityID);
        public bool Remove(int entityID)
        {
            if (!Has(entityID))
                return false;
            RemoveInternal(entityID);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void RemoveInternal(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (!Has(entityID)) Throw.Group_DoesNotContain(entityID);
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
        public void CopyFrom(EcsReadonlyGroup group) => CopyFrom(group.GetSource_Internal());
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
            if (entities.Length < _count)
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
        public EcsSpan ToSpan()
        {
            return new EcsSpan(WorldID, _dense);
        }
        public EcsSpan ToSpan(int start, int length)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (start + length > _count) Throw.ArgumentOutOfRange();
#endif
            return new EcsSpan(WorldID, _dense, start, length);
        }
        #endregion

        #region Set operations

        #region UnionWith
        /// <summary>as Union sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnionWith(EcsReadonlyGroup group) => UnionWith(group.GetSource_Internal());
        /// <summary>as Union sets</summary>
        public void UnionWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) Throw.Group_ArgumentDifferentWorldsException();
#endif
            foreach (var item in group)
                if (!Has(item))
                    AddInternal(item);
        }
        public void UnionWith(EcsSpan span)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source.id != span.WorldID) Throw.Group_ArgumentDifferentWorldsException();
#endif
            foreach (var item in span)
            {
                if (!Has(item))
                    AddInternal(item);
            }
        }
        #endregion

        #region ExceptWith
        /// <summary>as Except sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExceptWith(EcsReadonlyGroup group) => ExceptWith(group.GetSource_Internal());
        /// <summary>as Except sets</summary>
        public void ExceptWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) Throw.Group_ArgumentDifferentWorldsException();
#endif
            //if (group.Count > Count) // вариант 1. есть итерация по this
            //{
            //    foreach (var item in this)
            //        if (group.Has(item))
            //            RemoveInternal(item);
            //}
            //else
            //{
            //    foreach (var item in group)
            //        if (Has(item))
            //            RemoveInternal(item);
            //}

            //foreach (var item in group) // вариант 2
            //    if (Has(item))
            //        RemoveInternal(item);

            if (group.Count > Count)
            {
                for (int i = _count; i > 0; i--)//итерация в обратном порядке исключает ошибки при удалении элементов
                {
                    int item = _dense[i];
                    if (group.Has(item))
                        RemoveInternal(item);
                }
            }
            else
            {
                foreach (var item in group)
                {
                    if (Has(item))
                        RemoveInternal(item);
                }
            }
        }
        public void ExceptWith(EcsSpan span)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source.id != span.WorldID) Throw.Group_ArgumentDifferentWorldsException();
#endif
            foreach (var item in span)
            {
                if (Has(item))
                    RemoveInternal(item);
            }
        }
        #endregion

        #region IntersectWith
        /// <summary>as Intersect sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IntersectWith(EcsReadonlyGroup group) => IntersectWith(group.GetSource_Internal());
        /// <summary>as Intersect sets</summary>
        public void IntersectWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (World != group.World) Throw.Group_ArgumentDifferentWorldsException();
#endif
            for (int i = _count; i > 0; i--)//итерация в обратном порядке исключает ошибки при удалении элементов
            {
                int item = _dense[i];
                if (!group.Has(item))
                    RemoveInternal(item);
            }
        }
        #endregion

        #region SymmetricExceptWith
        /// <summary>as Symmetric Except sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SymmetricExceptWith(EcsReadonlyGroup group) => SymmetricExceptWith(group.GetSource_Internal());
        /// <summary>as Symmetric Except sets</summary>
        public void SymmetricExceptWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) Throw.Group_ArgumentDifferentWorldsException();
#endif
            foreach (var item in group)
                if (Has(item))
                    RemoveInternal(item);
                else
                    AddInternal(item);
        }
        #endregion

        #region Inverse
        public void Inverse()
        {
            foreach (var item in _source.Entities)
                if (Has(item))
                    RemoveInternal(item);
                else
                    AddInternal(item);
        }
        #endregion

        #region SetEquals
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsReadonlyGroup group) => SetEquals(group.GetSource_Internal());
        public bool SetEquals(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) Throw.Group_ArgumentDifferentWorldsException();
#endif
            if (group.Count != Count)
                return false;
            foreach (var item in group)
                if (!Has(item))
                    return false;
            return true;
        }
        public bool SetEquals(EcsSpan span)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source.id != span.WorldID) Throw.Group_ArgumentDifferentWorldsException();
#endif
            if (span.Length != Count)
                return false;
            foreach (var item in span)
                if (!Has(item))
                    return false;
            return true;
        }
        #endregion

        #region Overlaps
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsReadonlyGroup group) => Overlaps(group.GetSource_Internal());
        public bool Overlaps(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) Throw.Group_ArgumentDifferentWorldsException();
#endif
            if (group.Count > Count)
            {
                foreach (var item in this)
                    if (group.Has(item))
                        return true;
            }
            else
            {
                foreach (var item in group)
                    if (Has(item))
                        return true;
            }
            return false;
        }
        public bool Overlaps(EcsSpan span)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source.id != span.WorldID) Throw.Group_ArgumentDifferentWorldsException();
#endif
            foreach (var item in span)
                if (Has(item))
                    return true;
            return false;
        }
        #endregion

        #region IsSubsetOf
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(EcsReadonlyGroup group) => IsSubsetOf(group.GetSource_Internal());
        public bool IsSubsetOf(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) Throw.Group_ArgumentDifferentWorldsException();
#endif
            if (group.Count < Count)
                return false;
            foreach (var item in this)
                if (!group.Has(item))
                    return false;
            return true;
        }
        #endregion

        #region IsSupersetOf
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(EcsReadonlyGroup group) => IsSupersetOf(group.GetSource_Internal());
        public bool IsSupersetOf(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) Throw.Group_ArgumentDifferentWorldsException();
#endif
            if (group.Count > Count)
                return false;
            foreach (var item in group)
                if (!Has(item))
                    return false;
            return true;
        }
        #endregion

        #endregion

        #region Static Set operations
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Union(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (a._source != b._source) Throw.Group_ArgumentDifferentWorldsException();
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
            if (a._source != b._source) Throw.Group_ArgumentDifferentWorldsException();
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
            if (a._source != b._source) Throw.Group_ArgumentDifferentWorldsException();
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
            if (a._source != b._source) Throw.Group_ArgumentDifferentWorldsException();
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
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
        public LongsIterator GetLongs() => new LongsIterator(this);
        public struct Enumerator : IEnumerator<int>
        {
            private readonly int[] _dense;
            private uint _index;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(EcsGroup group)
            {
                _dense = group._dense;
                _index = (uint)(group._count > _dense.Length ? _dense.Length : group._count) + 1;
            }
            public int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _dense[_index];
            }
            object IEnumerator.Current => Current;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => --_index > 0; // <= потму что отсчет начинается с индекса 1 //_count < _dense.Length дает среде понять что проверки на выход за границы не нужны
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() { }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() { }
        }
        public readonly struct LongsIterator : IEnumerable<entlong>
        {
            private readonly EcsGroup _group;
            public LongsIterator(EcsGroup group) => _group = group;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator() => new Enumerator(_group);
            IEnumerator IEnumerable.GetEnumerator()
            {
                for (int i = 0; i < _group._count; i++)
                    yield return _group.World.GetEntityLong(_group._dense[i]);
            }
            IEnumerator<entlong> IEnumerable<entlong>.GetEnumerator()
            {
                for (int i = 0; i < _group._count; i++)
                    yield return _group.World.GetEntityLong(_group._dense[i]);
            }
            public struct Enumerator : IEnumerator<entlong>
            {
                private readonly EcsWorld world;
                private readonly int[] _dense;
                private uint _index;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public Enumerator(EcsGroup group)
                {
                    world = group.World;
                    _dense = group._dense;
                    _index = (uint)(group._count > _dense.Length ? _dense.Length : group._count) + 1;
                }
                public entlong Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get => world.GetEntityLong(_dense[_index]);
                }
                object IEnumerator.Current => Current;
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext() => --_index > 0; // <= потму что отсчет начинается с индекса 1 //_count < _dense.Length дает среде понять что проверки на выход за границы не нужны
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Dispose() { }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Reset() { }
            }
        }
        #endregion

        #region Other
        public override string ToString()
        {
            return CollectionUtility.EntitiesToString(_dense.Skip(1).Take(_count), "group");
            //return $"group({_count}) {{{string.Join(", ", _dense.Skip(1).Take(_count).OrderBy(o => o))}}}";
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int First()
        {
            return _dense[1];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Last()
        {
            return _dense[_count];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnWorldResize(int newSize)
        {
            Array.Resize(ref _sparse, newSize);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsReadonlyGroup(EcsGroup a) => a.Readonly;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsSpan(EcsGroup a) => a.ToSpan();
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
            public DebuggerProxy(EcsReadonlyGroup group) : this(group.GetSource_Internal()) { }
        }
        #endregion
    }

#if false
    public static class EcsGroupAliases
    {
        /// <summary>Alias for UnionWith</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(this EcsGroup self, EcsGroup group)
        {
            self.UnionWith(group);
        }
        /// <summary>Alias for UnionWith</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(this EcsGroup self, EcsReadonlyGroup group)
        {
            self.UnionWith(group);
        }
        /// <summary>Alias for ExceptWith</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Remove(this EcsGroup self, EcsGroup group)
        {
            self.ExceptWith(group);
        }
        /// <summary>Alias for ExceptWith</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Remove(this EcsGroup self, EcsReadonlyGroup group)
        {
            self.ExceptWith(group);
        }
        /// <summary>Alias for SymmetricExceptWith</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Xor(this EcsGroup self, EcsGroup group)
        {
            self.SymmetricExceptWith(group);
        }
        /// <summary>Alias for SymmetricExceptWith</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Xor(this EcsGroup self, EcsReadonlyGroup group)
        {
            self.SymmetricExceptWith(group);
        }
        /// <summary>Alias for IntersectWith</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void And(this EcsGroup self, EcsGroup group)
        {
            self.IntersectWith(group);
        }
        /// <summary>Alias for IntersectWith</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void And(this EcsGroup self, EcsReadonlyGroup group)
        {
            self.IntersectWith(group);
        }
    }
#endif
}