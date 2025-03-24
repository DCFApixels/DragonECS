#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Internal
{
#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [Serializable]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    internal class IdDispenser : IEnumerable<int>, IReadOnlyCollection<int>
    {
        private const int MIN_SIZE = 4;

        private int[] _dense = Array.Empty<int>();
        private int[] _sparse = Array.Empty<int>(); //hibit free flag

        private int _usedCount;     //[uuuu|      ]
        private int _size;          //[uuuu|ffffff]

        private int _nullID;

        #region Properties
        /// <summary> Used Count </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _usedCount; }
        }
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _size; }
        }
        public int NullID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _nullID; }
        }
        #endregion

        #region Constructors
        public IdDispenser(int minCapacity = MIN_SIZE, int nullID = 0, ResizedHandler resizedHandler = null)
        {
            Resized += resizedHandler;
            if (minCapacity < MIN_SIZE)
            {
                minCapacity = MIN_SIZE;
            }
            Resize(minCapacity);
            SetNullID(nullID);
        }
        #endregion

        #region Use/Reserve/Realese
        /// <summary>Marks as used and returns next free id.</summary>
        public int UseFree()
        {
            int ptr = _usedCount;
            CheckIDOrUpsize(ptr);
            int id = _dense[ptr];
            Move_FromFree_ToUsed(id);
            return id;
        }
        /// <summary>Marks as used a free or reserved id, after this id cannot be retrieved via UseFree.</summary>
        public void Use(int id)
        {
            CheckIDOrUpsize(id);
#if DEBUG
            if (IsUsed(id) || IsNullID(id))
            {
                if (IsNullID(id)) { ThrowHalper.ThrowIsNullID(id); }
                else { ThrowHalper.ThrowIsAlreadyInUse(id); }
            }
#endif
            Move_FromFree_ToUsed(id);
        }

        public void Release(int id)
        {
            CheckIDOrUpsize(id);
#if DEBUG
            if (IsFree(id) || IsNullID(id))
            {
                if (IsFree(id)) { ThrowHalper.ThrowIsNotUsed(id); }
                else { ThrowHalper.ThrowIsNullID(id); }
            }
#endif
            Move_FromUsed_ToFree(id);
        }
        #endregion

        #region Range Methods
        public void UseFreeRange(ref int[] array, int range)
        {
            if (array.Length < range)
            {
                Array.Resize(ref array, range);
            }
            for (int i = 0; i < range; i++)
            {
                array[i] = UseFree();
            }
        }
        public void UseFreeRange(List<int> list, int range)
        {
            for (int i = 0; i < range; i++)
            {
                list.Add(UseFree());
            }
        }
        public void UseRange(IEnumerable<int> ids)
        {
            foreach (var item in ids)
            {
                Use(item);
            }
        }
        public void ReleaseRange(IEnumerable<int> ids)
        {
            foreach (var item in ids)
            {
                Release(item);
            }
        }
        public void ReleaseAll()
        {
            _usedCount = 0;
            for (int i = 0; i < _size; i++)
            {
                _sparse[i] = i;
                _dense[i] = i;
            }
            SetNullID(_nullID);
        }
        #endregion

        #region Checks
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFree(int id)
        {
            return _sparse[id] >= _usedCount;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUsed(int id)
        {
            return _sparse[id] < _usedCount;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNullID(int id)
        {
            return id == _nullID;
        }
        #endregion

        #region Sort
        /// <summary>O(n) Sort. n = Size. Allows the UseFree method to return denser ids.</summary>
        public void Sort()
        {
            int usedIndex = 0;
            int freeIndex = _usedCount;
            for (int i = 0; i < _size; i++)
            {
                if (_sparse[i] < _usedCount)
                {
                    _sparse[i] = usedIndex;
                    _dense[usedIndex++] = i;
                }
                else
                {
                    _sparse[i] = freeIndex;
                    _dense[freeIndex++] = i;
                }
            }
        }
        #endregion

        #region Upsize
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Upsize(int minSize)
        {
            if (minSize > _size)
            {
                Upsize_Internal(minSize);
            }
        }
        #endregion

        #region Internal
        private void SetNullID(int nullID)
        {
            _nullID = nullID;
            if (nullID >= 0)
            {
                CheckIDOrUpsize(nullID);
                Swap(nullID, _usedCount++);
            }
        }
        internal bool IsValid()
        {
            for (int i = 0; i < _usedCount; i++)
            {
                if (_sparse[_dense[i]] != i || _dense[_sparse[i]] != i)
                {
                    return false;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckIDOrUpsize(int id)
        {
            if (id >= _size)
            {
                Upsize_Internal(id + 1);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Move_FromFree_ToUsed(int id)
        {
            Swap(id, _usedCount++);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Move_FromUsed_ToFree(int id)
        {
            Swap(id, --_usedCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(int sparseIndex, int denseIndex)
        {
            int _dense_denseIndex_ = _dense[denseIndex];
            int _sparse_sparseIndex_ = _sparse[sparseIndex];
            _dense[denseIndex] = _dense[_sparse_sparseIndex_];
            _dense[_sparse_sparseIndex_] = _dense_denseIndex_;
            _sparse[_dense_denseIndex_] = _sparse_sparseIndex_;
            _sparse[sparseIndex] = denseIndex;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Upsize_Internal(int minSize)
        {
            Resize(ArrayUtility.NextPow2_ClampOverflow(minSize));
        }
        private void Resize(int newSize)
        {
            Array.Resize(ref _dense, newSize);
            Array.Resize(ref _sparse, newSize);
            for (int i = _size; i < newSize;)
            {
                _sparse[i] = i;
                _dense[i] = i++;
            }
            _size = newSize;
            Resized(newSize);
        }
        #endregion

        #region Enumerable
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() { return new Enumerator(_dense, 0, _usedCount); }
        IEnumerator<int> IEnumerable<int>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public struct Enumerator : IEnumerator<int>
        {
            private readonly int[] _dense;
            private readonly int _count;
            private int _index;
            public int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return _dense[_index]; }
            }
            object IEnumerator.Current { get { return Current; } }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(int[] dense, int startIndex, int count)
            {
                _dense = dense;
                _count = startIndex + count;
                _index = startIndex - 1;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() { return ++_index < _count; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IDisposable.Dispose() { }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() { _index = -1; }
        }
        #endregion

        #region UsedToEcsSpan
        public EcsSpan UsedToEcsSpan(short worldID)
        {
            return new EcsSpan(worldID, _dense, 1, _usedCount - 1);
        }
        #endregion

        #region Utils
        private enum IDState : byte
        {
            Free = 0,
            Reserved = 1,
            Used = 2,
        }
        private static class ThrowHalper
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowIsAlreadyInUse(int id) { throw new ArgumentException($"Id {id} is already in use."); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowIsHasBeenReserved(int id) { throw new ArgumentException($"Id {id} has been reserved."); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowIsNotUsed(int id) { throw new ArgumentException($"Id {id} is not used."); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowIsNotAvailable(int id) { throw new ArgumentException($"Id {id} is not available."); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowIsNullID(int id) { throw new ArgumentException($"Id {id} cannot be released because it is used as a null id."); }
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void UndefinedException() { throw new Exception(); }
        }

        private class DebuggerProxy
        {
            private IdDispenser _target;
            public DebuggerProxy(IdDispenser dispenser)
            {
                _target = dispenser;
            }
#if DEBUG
            public ReadOnlySpan<int> Used => new ReadOnlySpan<int>(_target._dense, 0, _target._usedCount);
            public ReadOnlySpan<int> Free => new ReadOnlySpan<int>(_target._dense, _target._usedCount, _target._size - _target._usedCount);
            public Pair[] Pairs
            {
                get
                {
                    Pair[] result = new Pair[_target.Capacity];
                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = new Pair(
                            _target._dense[i],
                            _target._sparse[i],
                            i < _target.Count);
                    }
                    return result;
                }
            }
            public ID[] All
            {
                get
                {
                    ID[] result = new ID[_target.Capacity];
                    for (int i = 0; i < result.Length; i++)
                    {
                        int id = _target._dense[i];
                        result[i] = new ID(id, _target.IsUsed(id) ? "Used" : "Free");
                    }
                    return result;
                }
            }
            public bool IsValid => _target.IsValid();
            public int Count => _target.Count;
            public int Capacity => _target.Capacity;
            public int NullID => _target._nullID;
            internal readonly struct ID
            {
                public readonly int id;
                public readonly string state;
                public ID(int id, string state) { this.id = id; this.state = state; }
                public override string ToString() => $"{id} - {state}";
            }
            [DebuggerDisplay("{Separator} -> {sparse} - {dense}")]
            internal readonly struct Pair
            {
                public readonly int sparse;
                public readonly int dense;
                public readonly bool isSeparator;
                public int Separator => isSeparator ? 1 : 0;
                public Pair(int dense, int sparse, bool isSeparator)
                {
                    this.dense = dense;
                    this.sparse = sparse;
                    this.isSeparator = isSeparator;
                }
                //public override string ToString() => $"{sparse} - {dense} { (isSeparator ? '>' : ' ') } ";
            }
#endif
        }
        #endregion

        #region Events
        public delegate void ResizedHandler(int newSize);
        public event ResizedHandler Resized = delegate { };
        #endregion
    }
}