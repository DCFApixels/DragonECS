// Sparse Set based ID dispenser, with the ability to reserve IDs.
// Warning! Release version omits error exceptions, incorrect use may lead to unstable state.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DCFApixels
{
    [Serializable]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public class IdDispenser : IEnumerable<int>, IReadOnlyCollection<int>
    {
        private const int MIN_SIZE = 4;

        private int[] _dense = Array.Empty<int>();
        private int[] _sparse = Array.Empty<int>();
        private IDState[] _sparseState = Array.Empty<IDState>();

        private int _usedCount;     //[   |uuuu|      ]
        private int _reservedCount; //[rrr|    |      ]
        private int _size;          //[rrr|uuuu|ffffff]

        private int _nullID;

        #region Properties
        /// <summary> Used Count </summary>
        public int Count => _usedCount;
        public int ReservedCount => _reservedCount;
        public int Size => _size;
        public int NullID => _nullID;
        #endregion

        public IdDispenser(int capacity, int nullID = 0)
        {
            if (capacity % MIN_SIZE > 0)
                capacity += MIN_SIZE;
            Resize(capacity);
            SetNullID(nullID);

            Reserved = new ReservedSpan(this);
            Used = new UsedSpan(this);
        }

        #region Use/Reserve/Release
        /// <summary>Marks as used and returns next free id.</summary>
        public int UseFree()
        {
            int count = _usedCount + _reservedCount;
            CheckOrResize(count + 1);
            int id = _dense[count];
            Add(id);
            _sparseState[id] = IDState.Used;
            return id;
        }
        public void UseFreeRange(ref int[] array, int range)
        {
            if (array.Length < range)
                Array.Resize(ref array, range);
            for (int i = 0; i < range; i++)
                array[i] = UseFree();
        }
        public void UseFreeRange(List<int> list, int range)
        {
            for (int i = 0; i < range; i++)
                list.Add(UseFree());
        }
        /// <summary>Marks as used a free or reserved id, after this id cannot be retrieved via UseFree.</summary>
        public void Use(int id)
        {
            CheckOrResize(id);
#if DEBUG
            if (IsUsed(id) || IsReserved(id))
            {
                if (IsUsed(id)) ThrowHalper.ThrowIsAlreadyInUse(id);
                else ThrowHalper.ThrowIsHasBeenReserved(id);
            }
#endif
            if (IsFree(id))
                Add(id);
            _sparseState[id] = IDState.Used;
        }
        public void UseRange(IEnumerable<int> ids)
        {
            foreach (var item in ids)
                Use(item);
        }
        /// <summary>Marks as reserved and returns next free id, after this id cannot be retrieved via UseFree.</summary>
        public int ReserveFree()
        {
            int count = _usedCount + _reservedCount;
            CheckOrResize(count + 1);
            int id = _dense[count];
            _sparseState[id] = IDState.Reserved;
            AddReserved(id);
            return id;
        }
        /// <summary>Marks as reserved a free id, after this id cannot be retrieved via UseFree.</summary>
        public void Reserve(int id)
        {
            CheckOrResize(id);
#if DEBUG
            if (!IsFree(id)) ThrowHalper.ThrowIsNotAvailable(id);
#endif
            _sparseState[id] = IDState.Reserved;
            AddReserved(id);
        }
        public void ReserveRange(IEnumerable<int> ids)
        {
            foreach (var item in ids)
                Reserve(item);
        }
        public void Release(int id)
        {
            CheckOrResize(id);
#if DEBUG
            if (IsFree(id) || IsNullID(id))
            {
                if (IsFree(id)) ThrowHalper.ThrowIsNotUsed(id);
                else ThrowHalper.ThrowIsNullID(id);
            }
#endif
            if (_sparseState[id] == IDState.Used)
                Remove(id);
            else
                RemoveReserved(id);
            _sparseState[id] = IDState.Free;
        }
        public void ReleaseRange(IEnumerable<int> ids)
        {
            foreach (var item in ids)
                Release(item);
        }
        public void ReleaseAll()
        {
            _usedCount = 0;
            _reservedCount = 0;
            for (int i = 0; i < _size;)
            {
                _sparse[i] = i;
                _sparseState[i] = IDState.Free;
                _dense[i] = i++;
                _sparse[i] = i;
                _sparseState[i] = IDState.Free;
                _dense[i] = i++;
                _sparse[i] = i;
                _sparseState[i] = IDState.Free;
                _dense[i] = i++;
                _sparse[i] = i;
                _sparseState[i] = IDState.Free;
                _dense[i] = i++;
            }
            SetNullID(_nullID);
        }
        #endregion

        #region Checks
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFree(int id) => _sparseState[id] == IDState.Free;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsReserved(int id) => _sparseState[id] == IDState.Reserved;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUsed(int id) => _sparseState[id] == IDState.Used;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNullID(int id) => id == _nullID;

        #endregion

        #region Sort
        /// <summary>O(n) Sort. n = Size. Allows the UseFree method to return denser ids.</summary>
        public void Sort()
        {
            int usedInc = _reservedCount;
            int reservedInc = 0;
            int freeInc = _reservedCount + _usedCount;
            for (int i = 0; i < _size; i++)
            {
                switch (_sparseState[i])
                {
                    case IDState.Free:
                        _sparse[i] = freeInc;
                        _dense[freeInc++] = i;
                        break;
                    case IDState.Reserved:
                        _sparse[i] = reservedInc;
                        _dense[reservedInc++] = i;
                        break;
                    case IDState.Used:
                        _sparse[i] = usedInc;
                        _dense[usedInc++] = i;
                        break;
                }
            }
        }
        #endregion

        #region Other
        private void SetNullID(int nullID)
        {
            _nullID = nullID;
            if (nullID >= 0)
            {
                AddReserved(nullID);
                _sparseState[nullID] = IDState.Reserved;
            }
        }
        private bool IsValid()
        {
            for (int i = 0; i < _usedCount; i++)
            {
                if (_sparse[_dense[i]] != i || _dense[_sparse[i]] != i)
                    return false;
            }
            return true;
        }
        private void CheckOrResize(int id)
        {
            if (id > _size)
            {
                int leftBit = 0;
                while (id != 0)
                {
                    id >>= 1;
                    id &= int.MaxValue;
                    leftBit++;
                }
                if (leftBit >= 32)
                    Resize(int.MaxValue);
                else
                    Resize(1 << leftBit);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Add(int value)
        {
            Swap(value, _reservedCount + _usedCount++);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Remove(int value)
        {
            Swap(value, _reservedCount + --_usedCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddReserved(int value)
        {
            Swap(value, _reservedCount + _usedCount);
            Swap(value, _reservedCount++);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RemoveReserved(int value)
        {
            Swap(value, --_reservedCount);
            Swap(value, _reservedCount + _usedCount);
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
        private void Resize(int newSize)
        {
            if (newSize < MIN_SIZE)
                newSize = MIN_SIZE;
            Array.Resize(ref _dense, newSize);
            Array.Resize(ref _sparse, newSize);
            Array.Resize(ref _sparseState, newSize);
            for (int i = _size; i < newSize;)
            {
                _sparse[i] = i;
                _dense[i] = i++;
                _sparse[i] = i;
                _dense[i] = i++;
                _sparse[i] = i;
                _dense[i] = i++;
                _sparse[i] = i;
                _dense[i] = i++;
            }
            _size = newSize;
            Resized(newSize);
        }
        #endregion

        public delegate void ResizedHandler(int newSize);
        public event ResizedHandler Resized = delegate { };

        internal enum IDState : byte
        {
            Free,
            Reserved,
            Used,
        }

        #region Enumerable
        public UsedSpan Used;
        public ReservedSpan Reserved;
        public Enumerator GetEnumerator() => new Enumerator(_dense, _reservedCount, _usedCount);
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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
        public readonly struct UsedSpan : IEnumerable<int>
        {
            private readonly IdDispenser _instance;
            public int Count => _instance._usedCount;
            internal UsedSpan(IdDispenser instance) => _instance = instance;
            public Enumerator GetEnumerator() => new Enumerator(_instance._dense, _instance._reservedCount, _instance._usedCount);
            IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        public readonly struct ReservedSpan : IEnumerable<int>
        {
            private readonly IdDispenser _instance;
            public int Count => _instance._reservedCount;
            internal ReservedSpan(IdDispenser instance) => _instance = instance;
            public Enumerator GetEnumerator() => new Enumerator(_instance._dense, 0, _instance._reservedCount);
            IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        #endregion

        #region Utils
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
        }

        internal class DebuggerProxy
        {
            private IdDispenser _dispenser;
            public DebuggerProxy(IdDispenser dispenser) => _dispenser = dispenser;
#if DEBUG
            public IEnumerable<int> Used => _dispenser.Used;
            public IEnumerable<int> Reserved => _dispenser.Reserved;
            public Pair[] Pairs
            {
                get
                {
                    Pair[] result = new Pair[_dispenser.Size];
                    for (int i = 0; i < result.Length; i++)
                        result[i] = new Pair(_dispenser._dense[i], _dispenser._sparse[i]);
                    return result;
                }
            }
            public ID[] All
            {
                get
                {
                    ID[] result = new ID[_dispenser.Size];
                    for (int i = 0; i < result.Length; i++)
                    {
                        int id = _dispenser._dense[i];
                        result[i] = new ID(id, _dispenser._sparseState[id].ToString());
                    }
                    return result;
                }
            }
            public bool IsValid => _dispenser.IsValid();
            public int Count => _dispenser.ReservedCount;
            public int Size => _dispenser.Size;
            public int NullID => _dispenser._nullID;
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
    }
}