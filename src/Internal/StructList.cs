#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Core.Internal
{
    [DebuggerDisplay("Count: {Count}")]
    internal struct StructList<T>
    {
        internal readonly static bool _IsManaged = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

        internal T[] _items;
        internal int _count;

        #region Properties
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count; }
        }
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _items.Length; }
            set
            {
                if (value <= _items.Length) { return; }
                value = ArrayUtility.CeilPow2Safe(value);
                Array.Resize(ref _items, value);
            }
        }
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _items == null; }
        }
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if DEBUG
                if (index < 0 || index >= _count) { Throw.ArgumentOutOfRange(); }
#endif
                return _items[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
#if DEBUG
                if (index < 0 || index >= _count) { Throw.ArgumentOutOfRange(); }
#endif
                _items[index] = value;
            }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StructList(int capacity)
        {
            _items = new T[ArrayUtility.CeilPow2Safe(capacity)];
            _count = 0;
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if (_count >= _items.Length)
            {
                Array.Resize(ref _items, _items.Length << 1);
            }
            AddFixed(item);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFixed(T item)
        {
            _items[_count++] = item;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(T item)
        {
            return Array.IndexOf(_items, item, 0, _count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T item)
        {
            return _count != 0 && IndexOf(item) >= 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SwapAt(int idnex1, int idnex2)
        {
            T tmp = _items[idnex1];
            _items[idnex1] = _items[idnex2];
            _items[idnex2] = tmp;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FastRemoveAt(int index)
        {
#if DEBUG
            if (index < 0 || index >= _count) { Throw.ArgumentOutOfRange(); }
#endif
            _items[index] = _items[--_count];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index)
        {
#if DEBUG
            if (index < 0 || index >= _count) { Throw.ArgumentOutOfRange(); }
#endif
            _items[index] = _items[--_count];
            if (_IsManaged)
            {
                _items[_count] = default;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAtWithOrder(int index)
        {
#if DEBUG
            if (index < 0 || index >= _count) { Throw.ArgumentOutOfRange(); }
#endif
            for (int i = index; i < _count;)
            {
                _items[i++] = _items[i];
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveWithOrder(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAtWithOrder(index);
                return true;
            }
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek()
        {
#if DEBUG
            if (_count <= 0) { Throw.EmptyStack(); }
#endif
            return _items[_count - 1];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out T result)
        {
            if (_count <= 0)
            {
                result = default;
                return false;
            }
            result = _items[_count - 1];
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Pop()
        {
#if DEBUG
            if (_count <= 0) { Throw.EmptyStack(); }
#endif

            T result = _items[--_count];
            if (_IsManaged)
            {
                _items[_count] = default;
            }
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPop(out T result)
        {
            if (_count <= 0)
            {
                result = default;
                return false;
            }
            result = _items[--_count];
            if (_IsManaged)
            {
                _items[_count] = default;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FastClear()
        {
            _count = 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (_IsManaged)
            {
                Array.Clear(_items, 0, _count);
            }
            _count = 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Recreate()
        {
            _items = new T[_items.Length];
            _count = 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Recreate(int newSize)
        {
            _items = new T[ArrayUtility.CeilPow2Safe(newSize)];
            _count = 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T>.Enumerator GetEnumerator()
        {
            return new ReadOnlySpan<T>(_items, 0, _count).GetEnumerator();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan()
        {
            return new ReadOnlySpan<T>(_items, 0, _count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> ToEnumerable()
        {
            return _items.Take(_count);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] ToArray()
        {
            if (_count <= 0) { return Array.Empty<T>(); }
            T[] result = new T[_count];
            Array.Copy(_items, result, _count);
            return result;
        }
    }
}