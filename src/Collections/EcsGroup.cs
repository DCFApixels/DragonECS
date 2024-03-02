using DCFApixels.DragonECS.Internal;
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

        #region Properties
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source == null; }
        }
        public int WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.WorldID; }
        }
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.World; }
        }
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.Count; }
        }
        public int CapacityDense
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.CapacityDense; }
        }
        public EcsLongsSpan Longs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.Longs; }
        }
        public bool IsReleazed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.IsReleased; }
        }
        public int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source[index]; }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup(EcsGroup source)
        {
            _source = source;
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) { return _source.Has(entityID); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int entityID) { return _source.IndexOf(entityID); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup Clone() { return _source.Clone(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start) { return _source.Slice(start); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start, int length) { return _source.Slice(start, length); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ToSpan() { return _source.ToSpan(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] ToArray() { return _source.ToArray(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup.Enumerator GetEnumerator() { return _source.GetEnumerator(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int First() { return _source.First(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Last() { return _source.Last(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsGroup group) { return _source.SetEquals(group); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsReadonlyGroup group) { return _source.SetEquals(group._source); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsSpan span) { return _source.SetEquals(span); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsGroup group) { return _source.Overlaps(group); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsReadonlyGroup group) { return _source.Overlaps(group._source); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsSpan span) { return _source.Overlaps(span); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(EcsGroup group) { return _source.IsSubsetOf(group); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(EcsReadonlyGroup group) { return _source.IsSubsetOf(group._source); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(EcsGroup group) { return _source.IsSupersetOf(group); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(EcsReadonlyGroup group) { return _source.IsSupersetOf(group._source); }
        #endregion

        #region Internal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsGroup GetSource_Internal() { return _source; }
        #endregion

        #region Other
        public override string ToString()
        {
            return _source != null ? _source.ToString() : "NULL";
        }
#pragma warning disable CS0809 // Устаревший член переопределяет неустаревший член
        [Obsolete("Equals() on EcsGroup will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw new NotSupportedException(); }
        [Obsolete("GetHashCode() on EcsGroup will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() { throw new NotSupportedException(); }
#pragma warning restore CS0809 // Устаревший член переопределяет неустаревший член
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsSpan(EcsReadonlyGroup a) { return a.ToSpan(); }
        #endregion
    }

    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public class EcsGroup : IDisposable, IEnumerable<int>, IEntityStorage
    {
        private EcsWorld _source;
        private int[] _dense;
        private int[] _sparse;
        private int _count = 0;
        internal bool _isReleased = true;

        #region Properties
        public int WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.id; }
        }
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source; }
        }
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count; }
        }
        public int CapacityDense
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _dense.Length; }
        }
        public EcsReadonlyGroup Readonly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new EcsReadonlyGroup(this); }
        }
        public EcsLongsSpan Longs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new EcsLongsSpan(this); }
        }
        public bool IsReleased
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isReleased; }
        }
        public int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (index < 0 || index >= Count) { Throw.ArgumentOutOfRange(); }
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
        internal EcsGroup(EcsWorld world, int denseCapacity)
        {
            _source = world;
            _source.RegisterGroup(this);
            _dense = new int[denseCapacity];
            _sparse = new int[world.Capacity];
        }
        public void Dispose()
        {
            _source.ReleaseGroup(this);
        }
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
        public void AddUnchecked(int entityID)
        {
            Add_Internal(entityID);
        }
        public bool Add(int entityID)
        {
            if (Has(entityID))
            {
                return false;
            }
            Add_Internal(entityID);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Add_Internal(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (Has(entityID)) { Throw.Group_AlreadyContains(entityID); }
#endif
            if (++_count >= _dense.Length)
            {
                Array.Resize(ref _dense, _dense.Length << 1);
            }
            _dense[_count] = entityID;
            _sparse[entityID] = _count;
        }

        public void RemoveUnchecked(int entityID)
        {
            Remove_Internal(entityID);
        }
        public bool Remove(int entityID)
        {
            if (Has(entityID) == false)
            {
                return false;
            }
            Remove_Internal(entityID);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Remove_Internal(int entityID)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (Has(entityID) == false) { Throw.Group_DoesNotContain(entityID); }
#endif
            _dense[_sparse[entityID]] = _dense[_count];
            _sparse[_dense[_count--]] = _sparse[entityID];
            _sparse[entityID] = 0;
        }

        public void RemoveUnusedEntityIDs()
        {
            foreach (var entityID in this)
            {
                if (_source.IsUsed(entityID) == false)
                {
                    Remove_Internal(entityID);
                }
            }
        }
        #endregion

        #region Clear
        public void Clear()
        {
            if (_count == 0)
            {
                return;
            }
            for (int i = 1; i <= _count; i++)
            {
                _sparse[_dense[i]] = 0;
            }
            _count = 0;
        }
        #endregion


        #region Upsize
        public void Upsize(int minSize)
        {
            if (minSize >= _dense.Length)
            {
                Array.Resize(ref _dense, ArrayUtility.NormalizeSizeToPowerOfTwo_ClampOverflow(minSize));
            }
        }
        
        #endregion

        #region CopyFrom/Clone/Slice/ToSpan/ToArray
        public void CopyFrom(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (group.World != _source) { Throw.Group_ArgumentDifferentWorldsException(); }
#endif
            if (_count > 0)
            {
                Clear();
            }
            foreach (var item in group)
            {
                Add_Internal(item);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(EcsReadonlyGroup group)
        {
            CopyFrom(group.GetSource_Internal());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(EcsSpan span)
        {
            if (_count > 0)
            {
                Clear();
            }
            for (int i = 0; i < span.Count; i++)
            {
                Add_Internal(span[i]);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup Clone()
        {
            EcsGroup result = _source.GetFreeGroup();
            result.CopyFrom(this);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start)
        {
            return Slice(start, _count - start + 1);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start, int length)
        {
            start++;
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (start < 1 || start + length > _count) { Throw.ArgumentOutOfRange(); }
#endif
            return new EcsSpan(WorldID, _dense, start, length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ToSpan()
        {
            return new EcsSpan(WorldID, _dense, 1, _count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] ToArray()
        {
            int[] result = new int[_count];
            Array.Copy(_dense, 1, result, 0, _count);
            return result;
        }
        #endregion

        #region Set operations

        #region UnionWith
        /// <summary>as Union sets</summary>
        public void UnionWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) Throw.Group_ArgumentDifferentWorldsException();
#endif
            foreach (var entityID in group)
            {
                if (Has(entityID) == false)
                {
                    Add_Internal(entityID);
                }
            }
        }
        /// <summary>as Union sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnionWith(EcsReadonlyGroup group) => UnionWith(group.GetSource_Internal());
        /// <summary>as Union sets</summary>
        public void UnionWith(EcsSpan span)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source.id != span.WorldID) Throw.Group_ArgumentDifferentWorldsException();
#endif
            foreach (var entityID in span)
            {
                if (Has(entityID) == false)
                {
                    Add_Internal(entityID);
                }
            }
        }
        #endregion

        #region ExceptWith
        /// <summary>as Except sets</summary>
        public void ExceptWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) { Throw.Group_ArgumentDifferentWorldsException(); }
#endif
            if (group.Count > Count)
            {
                for (int i = _count; i > 0; i--)//итерация в обратном порядке исключает ошибки при удалении элементов
                {
                    int entityID = _dense[i];
                    if (group.Has(entityID))
                    {
                        Remove_Internal(entityID);
                    }
                }
            }
            else
            {
                foreach (var entityID in group)
                {
                    if (Has(entityID))
                    {
                        Remove_Internal(entityID);
                    }
                }
            }
        }
        /// <summary>as Except sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExceptWith(EcsReadonlyGroup group) { ExceptWith(group.GetSource_Internal()); }
        /// <summary>as Except sets</summary>
        public void ExceptWith(EcsSpan span)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source.id != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#endif
            foreach (var entityID in span)
            {
                if (Has(entityID))
                {
                    Remove_Internal(entityID);
                }
            }
        }
        #endregion

        #region IntersectWith
        /// <summary>as Intersect sets</summary>
        public void IntersectWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (World != group.World) { Throw.Group_ArgumentDifferentWorldsException(); }
#endif
            for (int i = _count; i > 0; i--)//итерация в обратном порядке исключает ошибки при удалении элементов
            {
                int entityID = _dense[i];
                if (group.Has(entityID) == false)
                {
                    Remove_Internal(entityID);
                }
            }
        }
        /// <summary>as Intersect sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IntersectWith(EcsReadonlyGroup group) { IntersectWith(group.GetSource_Internal()); }
        #endregion

        #region SymmetricExceptWith
        /// <summary>as Symmetric Except sets</summary>
        public void SymmetricExceptWith(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) { Throw.Group_ArgumentDifferentWorldsException(); }
#endif
            foreach (var entityID in group)
            {
                if (Has(entityID))
                {
                    Remove_Internal(entityID);
                }
                else
                {
                    Add_Internal(entityID);
                }
            }
        }
        /// <summary>as Symmetric Except sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SymmetricExceptWith(EcsReadonlyGroup group) { SymmetricExceptWith(group.GetSource_Internal()); }
        #endregion

        #region Inverse
        public void Inverse()
        {
            if (_count == 0)
            {
                foreach (var entityID in _source.Entities)
                {
                    Add_Internal(entityID);
                }
                return;
            }
            foreach (var entityID in _source.Entities)
            {
                if (Has(entityID))
                {
                    Remove_Internal(entityID);
                }
                else
                {
                    Add_Internal(entityID);
                }
            }
        }
        #endregion

        #region SetEquals
        public bool SetEquals(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) { Throw.Group_ArgumentDifferentWorldsException(); }
#endif
            if (group.Count != Count)
            {
                return false;
            }
            foreach (var entityID in group)
            {
                if (Has(entityID) == false)
                {
                    return false;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsReadonlyGroup group) { return SetEquals(group.GetSource_Internal()); }
        public bool SetEquals(EcsSpan span)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source.id != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#endif
            if (span.Count != Count)
            {
                return false;
            }
            foreach (var entityID in span)
            {
                if (Has(entityID) == false)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region Overlaps
        public bool Overlaps(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) Throw.Group_ArgumentDifferentWorldsException();
#endif
            if (group.Count > Count)
            {
                foreach (var entityID in this)
                {
                    if (group.Has(entityID))
                    {
                        return true;
                    }
                }
            }
            else
            {
                foreach (var entityID in group)
                {
                    if (Has(entityID))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsReadonlyGroup group) { return Overlaps(group.GetSource_Internal()); }
        public bool Overlaps(EcsSpan span)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source.id != span.WorldID) Throw.Group_ArgumentDifferentWorldsException();
#endif
            foreach (var entityID in span)
            {
                if (Has(entityID))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region IsSubsetOf
        public bool IsSubsetOf(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) Throw.Group_ArgumentDifferentWorldsException();
#endif
            if (group.Count < Count)
            {
                return false;
            }
            foreach (var entityID in this)
            {
                if (group.Has(entityID) == false)
                {
                    return false;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(EcsReadonlyGroup group) { return IsSubsetOf(group.GetSource_Internal()); }
        #endregion

        #region IsSupersetOf
        public bool IsSupersetOf(EcsGroup group)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (_source != group.World) Throw.Group_ArgumentDifferentWorldsException();
#endif
            if (group.Count > Count)
            {
                return false;
            }
            foreach (var entityID in group)
            {
                if (Has(entityID) == false)
                {
                    return false;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(EcsReadonlyGroup group) { return IsSupersetOf(group.GetSource_Internal()); }
        #endregion

        #endregion

        #region Static Set operations
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Union(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (a._source != b._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#endif
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var entityID in a)
            {
                result.Add_Internal(entityID);
            }
            foreach (var entityID in b)
            {
                result.Add(entityID);
            }
            return result;
        }
        public static EcsGroup Union(EcsReadonlyGroup a, EcsReadonlyGroup b)
        {
            return Union(a.GetSource_Internal(), b.GetSource_Internal());
        }

        /// <summary>as Except sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Except(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (a._source != b._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#endif
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var entityID in a)
            {
                if (b.Has(entityID) == false)
                {
                    result.Add_Internal(entityID);
                }
            }
            return result;
        }
        public static EcsGroup Except(EcsReadonlyGroup a, EcsReadonlyGroup b)
        {
            return Except(a.GetSource_Internal(), b.GetSource_Internal());
        }

        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Intersect(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (a._source != b._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#endif
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var entityID in a)
            {
                if (b.Has(entityID))
                {
                    result.Add_Internal(entityID);
                }
            }
            return result;
        }
        public static EcsGroup Intersect(EcsReadonlyGroup a, EcsReadonlyGroup b)
        {
            return Intersect(a.GetSource_Internal(), b.GetSource_Internal());
        }

        /// <summary>as Symmetric Except sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup SymmetricExcept(EcsGroup a, EcsGroup b)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (a._source != b._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#endif
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var entityID in a)
            {
                if (!b.Has(entityID))
                {
                    result.Add_Internal(entityID);
                }
            }
            foreach (var entityID in b)
            {
                if (!a.Has(entityID))
                {
                    result.Add_Internal(entityID);
                }
            }
            return result;
        }
        public static EcsGroup SymmetricExcept(EcsReadonlyGroup a, EcsReadonlyGroup b)
        {
            return SymmetricExcept(a.GetSource_Internal(), b.GetSource_Internal());
        }

        public static EcsGroup Inverse(EcsGroup a)
        {
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var item in a._source.Entities)
            {
                if (a.Has(item) == false)
                {
                    result.Add_Internal(item);
                }
            }
            return result;
        }
        public static EcsGroup Inverse(EcsReadonlyGroup a)
        {
            return Inverse(a.GetSource_Internal());
        }
        #endregion

        #region Enumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() { return new Enumerator(this); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        IEnumerator<int> IEnumerable<int>.GetEnumerator() { return GetEnumerator(); }
        public struct Enumerator : IEnumerator<int>
        {
            private readonly int[] _dense;
            private uint _index;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(EcsGroup group)
            {
                _dense = group._dense;
                //для оптимизации компилятором
                _index = (uint)(group._count > _dense.Length ? _dense.Length : group._count) + 1;
            }
            public int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _dense[_index];
            }
            object IEnumerator.Current { get { return Current; } }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                // проверка с учтом что отсчет начинается с индекса 1 
                return --_index > 0;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() { }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() { }
        }
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int First() { return _dense[1]; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Last() { return _dense[_count]; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnWorldResize_Internal(int newSize)
        {
            Array.Resize(ref _sparse, newSize);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnReleaseDelEntityBuffer_Internal(ReadOnlySpan<int> buffer)
        {
            if (_count <= 0)
            {
                return;
            }
            foreach (var entityID in buffer)
            {
                if (Has(entityID))
                {
                    Remove_Internal(entityID);
                }
            }
        }
        public override string ToString()
        {
            return CollectionUtility.EntitiesToString(_dense.Skip(1).Take(_count), "group");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsReadonlyGroup(EcsGroup a) { return a.Readonly; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsSpan(EcsGroup a) { return a.ToSpan(); }
        internal class DebuggerProxy
        {
            private EcsGroup _group;
            public EcsWorld World { get { return _group.World; } }
            public bool IsReleased { get { return _group.IsReleased; } }
            public EntitySlotInfo[] Entities
            {
                get
                {
                    EntitySlotInfo[] result = new EntitySlotInfo[_group.Count];
                    int i = 0;
                    foreach (var e in _group)
                    {
                        result[i++] = _group.World.GetEntitySlotInfoDebug(e);
                    }
                    return result;
                }
            }
            public int Count { get { return _group.Count; } }
            public int CapacityDense { get { return _group.CapacityDense; } }
            public override string ToString() { return _group.ToString(); }
            public DebuggerProxy(EcsGroup group) { _group = group; }
            public DebuggerProxy(EcsReadonlyGroup group) : this(group.GetSource_Internal()) { }
        }
        #endregion
    }
}