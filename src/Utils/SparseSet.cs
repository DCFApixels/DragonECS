using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using coretype = System.Int32;

namespace DCFApixels.DragonECS
{
    public class SparseSet : IEnumerable<coretype>, ICollection<coretype>, IReadOnlyCollection<coretype>
    {
        public const int DEFAULT_CAPACITY = 16;
        public const int MAX_CAPACITY = coretype.MaxValue;

        private coretype[] _dense;
        private coretype[] _sparse;

        private coretype _count;

        private coretype _denseCapacity;

        #region Properties
        public int Count => _count;
        public int CapacityDense
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _denseCapacity;
        }
        public int CapacitySparse
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _dense.Length;
        }

        public coretype this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if DEBUG
            get
            {
                ThrowHalper.CheckOutOfRange(this, (coretype)index);
                return _dense[index];
            }
#else
            get => _dense[index];
#endif
        }
        #endregion

        #region Constructors
        public SparseSet() : this(DEFAULT_CAPACITY) { }
        public SparseSet(coretype capacity)
        {
#if DEBUG
            ThrowHalper.CheckCapacity(capacity);
#endif 
            _dense = new coretype[capacity];
            _sparse = new coretype[capacity];
            for (coretype i = 0; i < _sparse.Length; i++)
            {
                _dense[i] = i;
                _sparse[i] = i;
            }
            _count = 0;
            _denseCapacity = 0;
        }
        #endregion

        #region Add/AddRange/GetFree
        public void Add<T>(coretype value, ref T[] normalizedArray)
        {
            Add(value);
            Normalize(ref normalizedArray);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(coretype value)
        {
#if DEBUG
            ThrowHalper.CheckValueIsPositive(value);
            ThrowHalper.CheckValueNotContained(this, value);
#endif
            if (value > CapacitySparse)
            {
                coretype neadedSpace = (coretype)_dense.Length;
                while (value >= neadedSpace) neadedSpace <<= 1;
                Resize(neadedSpace);
            }

            Swap(value, _count++);
            if (_count > _denseCapacity) _denseCapacity <<= 1;
        }

        public bool TryAdd<T>(coretype value, ref T[] normalizedArray)
        {
            if (Contains(value)) return false;
            Add(value);
            Normalize(ref normalizedArray);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(coretype value)
        {
            if (Contains(value)) return false;
            Add(value);
            return true;
        }

        public void AddRange<T>(IEnumerable<coretype> range, ref T[] normalizedArray)
        {
            AddRange(range);
            Normalize(ref normalizedArray);
        }
        public void AddRange(IEnumerable<coretype> range)
        {
            foreach (var item in range)
            {
                if (Contains(item)) continue;
                Add(item);
            }
        }

        /// <summary>Adds a value between 0 and Capacity to the array and returns it.</summary>
        /// <returns>Value between 0 and Capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public coretype GetFree<T>(ref T[] normalizedArray)
        {
            coretype result = GetFree();
            Normalize(ref normalizedArray);
            return result;
        }
        /// <summary>Adds a value between 0 and Capacity to the array and returns it.</summary>
        /// <returns>Value between 0 and Capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public coretype GetFree()
        {
            if (++_count >= CapacitySparse) AddSpaces();
            if (_count > _denseCapacity) _denseCapacity <<= 1;
            return _dense[_count - 1];
        }
        #endregion

        #region Contains
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(coretype value)
        {
            return value >= 0 && value < CapacitySparse && _sparse[value] < _count;
        }
        #endregion

        #region Remove
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(coretype value)
        {
#if DEBUG
            ThrowHalper.CheckValueContained(this, value);
#endif
            Swap(_sparse[value], --_count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRemove(coretype value)
        {
            if (!Contains(value)) return false;
            Remove(value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(coretype index)
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
        public int IndexOf(coretype value)
        {
            if (value < 0 || !Contains(value)) return -1;
            return _sparse[value];
        }

        public void Sort()
        {
            coretype increment = 0;
            for (coretype i = 0; i < CapacitySparse; i++)
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
            coretype inc = 0;
            coretype inc2 = _count;
            for (coretype i = 0; i < CapacitySparse; i++)
            {
                if (_sparse[i] < _count)
                {
                    _sparse[i] = inc;
                    _dense[inc++] = i;
                }
                else
                {
                    _sparse[i] = inc2;
                    _dense[inc2++] = i;
                }
            }
        }

        public void CopyTo(SparseSet other)
        {
            other._count = _count;
            if (CapacitySparse != other.CapacitySparse)
            {
                other.Resize(CapacitySparse);
            }
            _dense.CopyTo(other._dense, 0);
            _sparse.CopyTo(other._sparse, 0);
        }

        public void CopyTo(coretype[] array, int arrayIndex)
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
        #endregion

        #region Clear/Reset
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => _count = 0;
        public void Reset()
        {
            Clear();
            for (coretype i = 0; i < _dense.Length; i++)
            {
                _dense[i] = i;
                _sparse[i] = i;
            }
        }
        public void Reset(coretype newCapacity)
        {
#if DEBUG
            ThrowHalper.CheckCapacity(newCapacity);
#endif 
            Reset();
            Resize(newCapacity);
        }
        #endregion

        #region AddSpace/Resize
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddSpaces() => Resize((_count << 1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize(int newSpace)
        {
            coretype oldspace = (short)_dense.Length;
            Array.Resize(ref _dense, newSpace);
            Array.Resize(ref _sparse, newSpace);

            for (coretype i = oldspace; i < newSpace; i++)
            {
                _dense[i] = i;
                _sparse[i] = i;
            }
        }
        #endregion

        #region Swap
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(coretype fromIndex, coretype toIndex)
        {
            coretype value = _dense[toIndex];
            coretype oldValue = _dense[fromIndex];

            _dense[toIndex] = oldValue;
            _dense[fromIndex] = value;
            _sparse[_dense[fromIndex]] = fromIndex;
            _sparse[_dense[toIndex]] = toIndex;
        }
        #endregion

        #region Enumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefEnumerator GetEnumerator() => new RefEnumerator(_dense, _count);

        public ref struct RefEnumerator
        {
            private readonly coretype[] _dense;
            private readonly coretype _count;
            private coretype _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public RefEnumerator(coretype[] values, coretype count)
            {
                _dense = values;
                _count = count;
                _index = -1;
            }

            public coretype Current
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

        IEnumerator<coretype> IEnumerable<coretype>.GetEnumerator() => new Enumerator(_dense, _count);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_dense, _count);
        public struct Enumerator : IEnumerator<coretype> //to implement the IEnumerable interface and use the ref structure, 2 Enumerators were created.
        {
            private readonly coretype[] _dense;
            private readonly coretype _count;
            private coretype _index;
            public Enumerator(coretype[] values, coretype count)
            {
                _dense = values;
                _count = count;
                _index = -1;
            }
            public coretype Current => _dense[_index];
            object IEnumerator.Current => _dense[_index];
            public void Dispose() { }
            public bool MoveNext() => ++_index < _count;
            public void Reset() => _index = -1;
        }
        #endregion

        #region ICollection
        bool ICollection<coretype>.IsReadOnly => false;

        bool ICollection<coretype>.Remove(coretype value) => TryRemove(value);
        #endregion

        #region Debug
        public string Log()
        {
            StringBuilder logbuild = new StringBuilder();
            for (int i = 0; i < CapacitySparse; i++)
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
            for (int i = 0; i < CapacitySparse; i++)
            {
                logbuild.Append((i < _count ? _dense[i].ToString() : "_") + ", ");
            }
            logbuild.Append("\n\r");
            for (int i = 0; i < CapacitySparse; i++)
            {
                logbuild.Append((_sparse[i] < _count ? _sparse[i].ToString() : "_") + ", ");
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
            for (int index = 0; index < CapacitySparse; index++)
            {
                int value = _dense[index];
                isPass = isPass && _sparse[value] == index;
            }
            return isPass;
        }

#if DEBUG
        private static class ThrowHalper
        {
            public static void CheckCapacity(coretype capacity)
            {
                if (capacity < 0)
                    throw new ArgumentException("Capacity cannot be a negative number");
            }
            public static void CheckValueIsPositive(coretype value)
            {
                if (value < 0)
                    throw new ArgumentException("The SparseSet can only contain positive numbers");
            }
            public static void CheckValueContained(SparseSet source, coretype value)
            {
                if (!source.Contains(value))
                    throw new ArgumentException($"Value {value} is not contained");
            }
            public static void CheckValueNotContained(SparseSet source, coretype value)
            {
                if (source.Contains(value))
                    throw new ArgumentException($"Value {value} is already contained");
            }
            public static void CheckOutOfRange(SparseSet source, coretype index)
            {
                if (index < 0 || index >= source.Count)
                    throw new ArgumentOutOfRangeException($"Index {index} was out of range. Must be non-negative and less than the size of the collection.");
            }
        }
#endif
        #endregion
    }
}