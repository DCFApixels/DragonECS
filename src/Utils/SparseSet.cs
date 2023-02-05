//  _sparse[value] == index
//  _dense[index]  == value
//
//  int[] _dense  => |2|4|1|_|_|
//  int[] _sparse => |_|2|0|_|1|
//
//  indexator => [0]2, [1]4, [2]1
//
//  can use foreach
//  implements IEnumerable<int>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace DCFApixels.DragonECS
{
    public class SparseSet : IEnumerable<int>, ICollection<int>, IReadOnlyCollection<int>
    {
        public const int DEFAULT_CAPACITY = 16;

        private int[] _dense;
        private int[] _sparse;

        private int _count;

        #region Properties
        public int Count => _count;
        public int Capacity => _dense.Length;

        public int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if DEBUG
            get
            {
                ThrowHalper.CheckOutOfRange(this, index);
                return _dense[index];
            }
#else
            get => _dense[index];
#endif
        }

        public IndexesCollection Indexes => new IndexesCollection(_sparse);
        #endregion

        #region Constructors
        public SparseSet() : this(DEFAULT_CAPACITY) { }
        public SparseSet(int capacity)
        {
#if DEBUG
            ThrowHalper.CheckCapacity(capacity);
#endif 
            _dense = new int[capacity];
            _sparse = new int[capacity];
            for (int i = 0; i < _sparse.Length; i++)
            {
                _dense[i] = i;
                _sparse[i] = i;
            }
            _count = 0;
        }
        #endregion

        #region Add/AddRange/GetFree
        public void Add<T>(int value, ref T[] normalizedArray)
        {
            Add(value);
            Normalize(ref normalizedArray);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int value)
        {
#if DEBUG
            ThrowHalper.CheckValueIsPositive(value);
            ThrowHalper.CheckValueNotContained(this, value);
#endif

            int neadedSpace = _dense.Length;
            while (value >= neadedSpace)
                neadedSpace <<= 1;

            if (neadedSpace != _dense.Length)
                Resize(neadedSpace);

            if (Contains(value))
            {
                return;
            }

            Swap(value, _count++);
        }

        public bool TryAdd<T>(int value, ref T[] normalizedArray)
        {
            if (Contains(value))
                return false;

            Add(value, ref normalizedArray);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(int value)
        {
            if (Contains(value))
                return false;

            Add(value);
            return true;
        }

        public void AddRange<T>(IEnumerable<int> range, ref T[] normalizedArray)
        {
            foreach (var item in range)
            {
                if (Contains(item))
                    continue;

                Add(item);
            }
            Normalize(ref normalizedArray);
        }

        public void AddRange(IEnumerable<int> range)
        {
            foreach (var item in range)
            {
                if (Contains(item))
                    continue;

                Add(item);
            }
        }
        /// <summary>
        /// Adds a value between 0 and Capacity to the array and returns it.
        /// </summary>
        /// <returns>Value between 0 and Capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetFree<T>(ref T[] normalizedArray)
        {
            int result = GetFree();
            Normalize(ref normalizedArray);
            return result;
        }
        /// <summary>
        /// Adds a value between 0 and Capacity to the array and returns it.
        /// </summary>
        /// <returns>Value between 0 and Capacity</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetFree()
        {
            if (++_count >= _dense.Length)
                AddSpaces();

            return _dense[_count - 1];
        }
        #endregion

        #region Contains
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int value)
        {
            return value >= 0 && value < Capacity && _sparse[value] < _count;
        }
        #endregion

        #region Remove
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(int value)
        {
#if DEBUG
            ThrowHalper.CheckValueContained(this, value);
#endif
            Swap(_sparse[value], --_count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRemove(int value)
        {
            if (!Contains(value))
                return false;

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
            if (array.Length != _dense.Length)
                Array.Resize(ref array, _dense.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int value)
        {
            if (value < 0 || !Contains(value))
                return -1;

            return _sparse[value];
        }

        public void Sort()
        {
            int increment = 0;
            for (int i = 0; i < Capacity; i++)
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
            for (int i = 0; i < Capacity; i++)
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
            if (Capacity != other.Capacity)
            {
                other.Resize(Capacity);
            }
            _dense.CopyTo(other._dense, 0);
            _sparse.CopyTo(other._sparse, 0);
        }

        public void CopyTo(int[] array, int arrayIndex)
        {
#if DEBUG
            if (arrayIndex < 0)
                throw new ArgumentException("arrayIndex is less than 0");
            if (arrayIndex + _count >= array.Length)
                throw new ArgumentException("The number of elements in the source List<T> is greater than the available space from arrayIndex to the end of the destination array.");
#endif
            for (int i = 0; i < _count; i++, arrayIndex++)
            {
                array[arrayIndex] = this[i];
            }
        }
        #endregion

        #region Clear/Reset
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _count = 0;
        }

        public void Reset()
        {
            Clear();
            for (int i = 0; i < _dense.Length; i++)
            {
                _dense[i] = i;
                _sparse[i] = i;
            }
        }
        public void Reset(int newCapacity)
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
        private void AddSpaces() => Resize(_count << 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Resize(int newSpace)
        {
            int oldspace = _dense.Length;
            Array.Resize(ref _dense, newSpace);
            Array.Resize(ref _sparse, newSpace);

            for (int i = oldspace; i < newSpace; i++)
            {
                _dense[i] = i;
                _sparse[i] = i;
            }
        }
        #endregion

        #region Swap
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Swap(int fromIndex, int toIndex)
        {
            int value = _dense[toIndex];
            int oldValue = _dense[fromIndex];

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

        #region Utils
        public ref struct IndexesCollection
        {
            private readonly int[] _indexes;

            public IndexesCollection(int[] indexes)
            {
                _indexes = indexes;
            }

            public int this[int value]
            {
                get => _indexes[value];
            }
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
            for (int i = 0; i < Capacity; i++)
            {
                logbuild.Append(_dense[i] + ", ");
            }
            logbuild.Append("\n\r");
            for (int i = 0; i < Capacity; i++)
            {
                logbuild.Append(_sparse[i] + ", ");
            }
            logbuild.Append("\n\r --------------------------");
            logbuild.Append("\n\r");
            for (int i = 0; i < Capacity; i++)
            {
                logbuild.Append((i < _count ? _dense[i].ToString() : "_") + ", ");
            }
            logbuild.Append("\n\r");
            for (int i = 0; i < Capacity; i++)
            {
                logbuild.Append((_sparse[i] < _count ? _sparse[i].ToString() : "_") + ", ");
            }
            logbuild.Append("\n\r Count: " + _count);
            logbuild.Append("\n\r Capacity: " + Capacity);
            logbuild.Append("\n\r IsValide: " + IsValide_Debug());

            logbuild.Append("\n\r");
            return logbuild.ToString();
        }

        public bool IsValide_Debug()
        {
            bool isPass = true;
            for (int index = 0; index < Capacity; index++)
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