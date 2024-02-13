using DCFApixels.DragonECS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Utils
{
    [Serializable]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public class IdDispenser : IEnumerable<int>, IReadOnlyCollection<int>
    {
        private const int FREE_FLAG_BIT = ~INDEX_MASK;
        private const int INDEX_MASK = 0x7FFF_FFFF;
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
            get { return _usedCount; }
        }
        public int Size
        {
            get { return _size; }
        }
        public int NullID
        {
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
        public int UseFree()  //+
        {
            int ptr = _usedCount;
            CheckOrResize(ptr);
            int id = _dense[ptr];
            Move_FromFree_ToUsed(id);
            SetFlag_Used(id);
            return id;
        }
        /// <summary>Marks as used a free or reserved id, after this id cannot be retrieved via UseFree.</summary>
        public void Use(int id)  //+
        {
            CheckOrResize(id);
#if DEBUG
            if (IsUsed(id) || IsNullID(id))
            {
                if (IsNullID(id)) { ThrowHalper.ThrowIsNullID(id); }
                else { ThrowHalper.ThrowIsAlreadyInUse(id); }
            }
#endif
            Move_FromFree_ToUsed(id);
            SetFlag_Used(id);
        }

        public void Release(int id)  //+
        {
            CheckOrResize(id);
#if DEBUG
            if (IsFree(id) || IsNullID(id))
            {
                if (IsFree(id)) { ThrowHalper.ThrowIsNotUsed(id); }
                else { ThrowHalper.ThrowIsNullID(id); }
            }
#endif
            Move_FromUsed_ToFree(id);
            SetFlag_Free(id);
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
                _sparse[i] = SetFlag_Free_For(i);
                _dense[i] = i;
            }
            SetNullID(_nullID);
        }
        #endregion

        #region Checks
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFree(int id)
        {
            return _sparse[id] < 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUsed(int id)
        {
            return _sparse[id] >= 0;
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
                if(_sparse[i] > 0)
                {
                    _sparse[i] = usedIndex;
                    _dense[usedIndex++] = i;
                }
                else
                {
                    _sparse[i] = SetFlag_Free_For(freeIndex);
                    _dense[freeIndex++] = i;
                }
            }
        }
        #endregion

        #region UpSize
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void UpSize(int minSize)
        {
            if (minSize > _size)
            {
                UpSize_Internal(minSize);
            }
        }
        #endregion

        #region Internal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SetFlag_Used_For(int value)
        {
            return value & INDEX_MASK;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int SetFlag_Free_For(int value)
        {
            return value | FREE_FLAG_BIT;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFlag_Used(int id)
        {
            _sparse[id] &= INDEX_MASK;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetFlag_Free(int id)
        {
            _sparse[id] |= FREE_FLAG_BIT;
        }
        private void SetNullID(int nullID)
        {
            _nullID = nullID;
            if (nullID >= 0)
            {
                Swap(nullID, _usedCount++);
            }
        }
        private bool IsValid()
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
        private void CheckOrResize(int id)
        {
            if (id >= _size)
            {
                UpSize_Internal(id + 1);
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
            int _sparse_sparseIndex_ = SetFlag_Used_For(_sparse[sparseIndex]);
            _dense[denseIndex] = _dense[_sparse_sparseIndex_];
            _dense[_sparse_sparseIndex_] = _dense_denseIndex_;
            _sparse[_dense_denseIndex_] = _sparse_sparseIndex_;
            _sparse[sparseIndex] = denseIndex;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UpSize_Internal(int minSize)
        {
            Resize(ArrayUtility.NormalizeSizeToPowerOfTwo_ClampOverflow(minSize));
        }
        private void Resize(int newSize)
        {
            Array.Resize(ref _dense, newSize);
            Array.Resize(ref _sparse, newSize);
            for (int i = _size; i < newSize;)
            {
                _sparse[i] = SetFlag_Free_For(i);
                _dense[i] = i++;
            }
            _size = newSize;
            Resized(newSize);
        }
        #endregion

        #region Enumerable
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_dense, 0, _usedCount);
        }
        IEnumerator<int> IEnumerable<int>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public struct Enumerator : IEnumerator<int>
        {
            private readonly int[] _dense;
            private readonly int _count;
            private int _index;
            public int Current => _dense[_index];
            object IEnumerator.Current => Current;
            public Enumerator(int[] dense, int startIndex, int count)
            {
                _dense = dense;
                _count = startIndex + count;
                _index = startIndex - 1;
            }
            public bool MoveNext() => ++_index < _count;
            public void Dispose() { }
            public void Reset() => _index = -1;
        }
        #endregion

        #region UsedToEcsSpan
        public EcsSpan UsedToEcsSpan(int worldID)
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
            public static void ThrowIsAlreadyInUse(int id) => throw new ArgumentException($"Id {id} is already in use.");
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowIsHasBeenReserved(int id) => throw new ArgumentException($"Id {id} has been reserved.");

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowIsNotUsed(int id) => throw new ArgumentException($"Id {id} is not used.");

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowIsNotAvailable(int id) => throw new ArgumentException($"Id {id} is not available.");

            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowIsNullID(int id) => throw new ArgumentException($"Id {id} cannot be released because it is used as a null id.");

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
                    Pair[] result = new Pair[_target.Size];
                    for (int i = 0; i < result.Length; i++)
                        result[i] = new Pair(_target._dense[i], _target._sparse[i]);
                    return result;
                }
            }
            public ID[] All
            {
                get
                {
                    ID[] result = new ID[_target.Size];
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
            public int Size => _target.Size;
            public int NullID => _target._nullID;
            internal readonly struct ID
            {
                public readonly int id;
                public readonly string state;
                public ID(int id, string state) { this.id = id; this.state = state; }
                public override string ToString() => $"{id} - {state}";
            }
            internal readonly struct Pair
            {
                public readonly int dense;
                public readonly int sparse;
                public Pair(int dense, int sparse) { this.dense = dense; this.sparse = sparse; }
                public override string ToString() => $"{dense} - {sparse}";
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