using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace DCFApixels.DragonECS
{
    public class SparseSet : IEnumerable<int>, ICollection<int>, IReadOnlyCollection<int>
    {
        public const int DEFAULT_DENSE_CAPACITY = 8;
        public const int DEFAULT_SPARSE_CAPACITY = 16;

        public const int MIN_CAPACITY = 4;

        public const int MAX_CAPACITY = int.MaxValue;

        private int[] _dense;
        private int[] _sparse;

        private int _count;

        #region Properties
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
        public int CapacitySparse
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _sparse.Length;
        }

        public int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if DEBUG
                ThrowHalper.CheckOutOfRange(this, index);
#endif
                return _dense[index];
            }
        }
        #endregion

        #region Constructors
        public SparseSet() : this(DEFAULT_DENSE_CAPACITY, DEFAULT_SPARSE_CAPACITY) { }
        public SparseSet(int denseCapacity, int sparseCapacity)
        {
            denseCapacity = denseCapacity < MIN_CAPACITY ? MIN_CAPACITY : NormalizeCapacity(denseCapacity);
            sparseCapacity = sparseCapacity < MIN_CAPACITY ? MIN_CAPACITY : NormalizeCapacity(sparseCapacity);

            _dense = new int[denseCapacity];
            _sparse = new int[sparseCapacity];

            Reset();
        }
        #endregion

        #region Add/AddRange
        public void Add<T>(int value, ref T[] normalizedArray)
        {
            Add(value);
            Normalize(ref normalizedArray);
        }

        public void Add(int value)
        {
#if DEBUG
            ThrowHalper.CheckValueIsPositive(value);
            ThrowHalper.CheckValueNotContained(this, value);
#endif
            if (_count >= _dense.Length)
                Array.Resize(ref _dense, _dense.Length << 1);

            if (value >= _sparse.Length)
            {
                int neadedSpace = _sparse.Length;
                while (value >= neadedSpace)
                    neadedSpace <<= 1;
                int i = _sparse.Length;
                Array.Resize(ref _sparse, neadedSpace);
                //loop unwinding
                for (; i < neadedSpace;)
                {
                    _sparse[i++] = -1;
                    _sparse[i++] = -1;
                    _sparse[i++] = -1;
                    _sparse[i++] = -1;
                }
            }

            _dense[_count] = value;
            _sparse[value] = _count++;
        }

        public bool TryAdd<T>(int value, ref T[] normalizedArray)
        {
            if (Contains(value)) return false;
            Add(value);
            Normalize(ref normalizedArray);
            return true;
        }
        public bool TryAdd(int value)
        {
            if (Contains(value)) return false;
            Add(value);
            return true;
        }

        public void AddRange<T>(IEnumerable<int> range, ref T[] normalizedArray)
        {
            AddRange(range);
            Normalize(ref normalizedArray);
        }
        public void AddRange(IEnumerable<int> range)
        {
            foreach (var item in range)
            {
                if (Contains(item)) continue;
                Add(item);
            }
        }
        #endregion

        #region Contains
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int value)
        {
            return value >= 0 && value < CapacitySparse && _sparse[value] >= 0;
        }
        #endregion

        #region Remove
        public void Remove(int value)
        {
#if DEBUG
            ThrowHalper.CheckValueContained(this, value);
#endif
            _dense[_sparse[value]] = _dense[--_count];
            _sparse[_dense[_count]] = _sparse[value];
            _sparse[value] = -1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRemove(int value)
        {
            if (!Contains(value)) return false;
            Remove(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index)
        {
#if DEBUG
            ThrowHalper.CheckOutOfRange(this, index);
#endif
            Remove(_dense[index]);
        }
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize<T>(ref T[] array)
        {
            if (array.Length < CapacityDense) Array.Resize(ref array, CapacityDense);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int value)
        {
            if (value < 0 || !Contains(value)) return -1;
            return _sparse[value];
        }

        public void Sort()
        {
            int increment = 0;
            for (int i = 0; i < CapacitySparse; i++)
            {
                if (_sparse[i] < _count)
                {
                    _sparse[i] = increment;
                    _dense[increment++] = i;
                }
            }
        }

        public void HardSort()
        {
            int inc = 0;
            int inc2 = _count;
            for (int i = 0; i < CapacitySparse; i++)
            {
                if (_sparse[i] >= 0)
                {
                    _sparse[i] = inc;
                    _dense[inc++] = i;
                }
                else
                {
                    _dense[inc2++] = i;
                }
            }
        }

        public void CopyTo(SparseSet other)
        {
            other._count = _count;
            if (CapacitySparse != other.CapacitySparse)
                Array.Resize(ref other._sparse, CapacitySparse);
            if (CapacityDense != other.CapacityDense)
                Array.Resize(ref other._dense, CapacityDense);
            _sparse.CopyTo(other._sparse, 0);
            _dense.CopyTo(other._dense, 0);
        }

        public void CopyTo(int[] array, int arrayIndex)
        {
#if DEBUG
            if (arrayIndex < 0) throw new ArgumentException("arrayIndex is less than 0");
            if (arrayIndex + _count >= array.Length) throw new ArgumentException("The number of elements in the source List<T> is greater than the available space from arrayIndex to the end of the destination array.");
#endif
            for (int i = 0; i < _count; i++, arrayIndex++)
            {
                array[arrayIndex] = this[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int NormalizeCapacity(int value)
        {
            return value + (MIN_CAPACITY - (value % MIN_CAPACITY));
        }
        #endregion

        #region Clear/Reset
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _count = 0;
        public void Reset()
        {
            Clear();
            //loop unwinding
            for (int i = 0; i < _sparse.Length;)
            {
                _sparse[i++] = -1;
                _sparse[i++] = -1;
                _sparse[i++] = -1;
                _sparse[i++] = -1;
            }
        }

        public void Reset(int newDenseCapacity, int newSparseCapacity)
        {
            newDenseCapacity = newDenseCapacity < MIN_CAPACITY ? MIN_CAPACITY : NormalizeCapacity(newDenseCapacity);
            newSparseCapacity = newSparseCapacity < MIN_CAPACITY ? MIN_CAPACITY : NormalizeCapacity(newSparseCapacity);

            if (CapacitySparse != newSparseCapacity)
                Array.Resize(ref _sparse, newSparseCapacity);
            if (CapacityDense != newDenseCapacity)
                Array.Resize(ref _dense, newDenseCapacity);
            Reset();
        }
        #endregion

        #region Enumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefEnumerator GetEnumerator() => new RefEnumerator(_dense, _count);

        public ref struct RefEnumerator
        {
            private readonly int[] _dense;
            private readonly int _count;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public RefEnumerator(int[] values, int count)
            {
                _dense = values;
                _count = count;
                _index = -1;
            }

            public int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _dense[_index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_index < _count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() => _index = -1;
        }

        IEnumerator<int> IEnumerable<int>.GetEnumerator() => new Enumerator(_dense, _count);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_dense, _count);
        public struct Enumerator : IEnumerator<int> //to implement the IEnumerable interface and use the ref structure, 2 Enumerators were created.
        {
            private readonly int[] _dense;
            private readonly int _count;
            private int _index;
            public Enumerator(int[] values, int count)
            {
                _dense = values;
                _count = count;
                _index = -1;
            }
            public int Current => _dense[_index];
            object IEnumerator.Current => _dense[_index];
            public void Dispose() { }
            public bool MoveNext() => ++_index < _count;
            public void Reset() => _index = -1;
        }
        #endregion

        #region ICollection
        bool ICollection<int>.IsReadOnly => false;

        bool ICollection<int>.Remove(int value) => TryRemove(value);
        #endregion

        #region Debug
        public string Log()
        {
            StringBuilder logbuild = new StringBuilder();
            for (int i = 0; i < CapacityDense; i++)
            {
                logbuild.Append(_dense[i] + ", ");
            }
            logbuild.Append("\n\r");
            for (int i = 0; i < CapacitySparse; i++)
            {
                logbuild.Append(_sparse[i] + ", ");
            }
            logbuild.Append("\n\r --------------------------");
            logbuild.Append("\n\r");
            for (int i = 0; i < CapacityDense; i++)
            {
                logbuild.Append((i < _count ? _dense[i].ToString() : "_") + ", ");
            }
            logbuild.Append("\n\r");
            for (int i = 0; i < CapacitySparse; i++)
            {
                logbuild.Append((_sparse[i] >= 0 ? _sparse[i].ToString() : "_") + ", ");
            }
            logbuild.Append("\n\r Count: " + _count);
            logbuild.Append("\n\r Capacity: " + CapacitySparse);
            logbuild.Append("\n\r IsValide: " + IsValide_Debug());

            logbuild.Append("\n\r");
            return logbuild.ToString();
        }

        public bool IsValide_Debug()
        {
            bool isPass = true;
            for (int index = 0; index < _count; index++)
            {
                int value = _dense[index];
                isPass = isPass && _sparse[value] == index;
            }
            return isPass;
        }

#if DEBUG
        private static class ThrowHalper
        {
            public static void CheckCapacity(int capacity)
            {
                if (capacity < 0)
                    throw new ArgumentException("Capacity cannot be a negative number");
            }
            public static void CheckValueIsPositive(int value)
            {
                if (value < 0)
                    throw new ArgumentException("The SparseSet can only contain positive numbers");
            }
            public static void CheckValueContained(SparseSet source, int value)
            {
                if (!source.Contains(value))
                    throw new ArgumentException($"Value {value} is not contained");
            }
            public static void CheckValueNotContained(SparseSet source, int value)
            {
                if (source.Contains(value))
                    throw new ArgumentException($"Value {value} is already contained");
            }
            public static void CheckOutOfRange(SparseSet source, int index)
            {
                if (index < 0 || index >= source.Count)
                    throw new ArgumentOutOfRangeException($"Index {index} was out of range. Must be non-negative and less than the size of the collection.");
            }
        }
#endif
        #endregion
    }
}