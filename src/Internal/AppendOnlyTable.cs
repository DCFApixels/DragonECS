#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS.Core.Internal
{
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal static class AppendOnlyTable<TKey, TValue> where TKey : IEquatable<TKey>
    {
        private const float LoadFactor = 0.75f;

        private static TKey[] _keys;
        private static TValue[] _values;
        private static bool[] _occupied;

        private static int _count;
        private static int _capacity;

        private static int _maxLoadCount;
        private static int _mask;
        private static bool _isInitialized = false;
        //private static int _version;

        private static readonly bool _isUnmanaged = 
            RuntimeHelpers.IsReferenceOrContainsReferences<TKey>() == false && 
            RuntimeHelpers.IsReferenceOrContainsReferences<TValue>() == false;

        #region Public
        public readonly struct Provider : IEnumerable<KeyValuePair<TKey, TValue>>
        {
            // избежание запуска статического конструктора
            private readonly bool _isInitialized;
            public bool IsInitialized
            {
                get { return  _isInitialized; }
            }
            public int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return AppendOnlyTable<TKey, TValue>.Count; }
            }
            public int Capacity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return AppendOnlyTable<TKey, TValue>.Capacity; }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Provider(int initialCapacity)
            {
                if (AppendOnlyTable<TKey, TValue>.IsInitialized == false)
                {
                    Initialize(initialCapacity);
                }
                _isInitialized = true;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add(TKey key, TValue value) { AppendOnlyTable<TKey, TValue>.Add(key, value); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryAdd(TKey key, TValue value) { return AppendOnlyTable<TKey, TValue>.TryAdd(key, value); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TValue Get(TKey key) { return AppendOnlyTable<TKey, TValue>.Get(key); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGet(TKey key, out TValue value) { return AppendOnlyTable<TKey, TValue>.TryGet(key, out value); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Has(TKey key) { return AppendOnlyTable<TKey, TValue>.Has(key); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Clear() { AppendOnlyTable<TKey, TValue>.Clear(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator() { return new Enumerator(); }
            IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() { return GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly int _version;
            private int _index;
            private KeyValuePair<TKey, TValue> _current;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(int versionSnapshot)
            {
                _version = versionSnapshot;
                _index = -1;
                _current = default;
            }
            public KeyValuePair<TKey, TValue> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]

                get { return _current; }
            }
            object IEnumerator.Current { get { return _current; } }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                //if (AppendOnlyTable<TKey, TValue>._version != _version) ThrowVersionChanged();

                while (++_index < _capacity)
                {
                    if (_occupied[_index])
                    {
                        _current = new KeyValuePair<TKey, TValue>(_keys[_index], _values[_index]);
                        return true;
                    }
                }
                _current = default;
                return false;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                //if (AppendOnlyTable<TKey, TValue>._version != _version) ThrowVersionChanged();
                _index = -1;
                _current = default;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() { }
        }

        public static bool IsInitialized
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isInitialized; }
        }
        public static int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count; }
        }
        public static int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _capacity; }
        }
        public static void Initialize(int initialCapacity)
        {
            if (_isInitialized)
            {
                ThrowAlreadyInitialized();
            }

            if (initialCapacity < 1)
            {
                ThrowInvalidCapacity(initialCapacity);
            }

            _capacity = NextPowerOfTwo(initialCapacity);
            _mask = _capacity - 1;
            _maxLoadCount = (int)(_capacity * LoadFactor);
            _maxLoadCount = _maxLoadCount < 4 ? 4 : _maxLoadCount;

            _keys = new TKey[_capacity];
            _values = new TValue[_capacity];
            _occupied = new bool[_capacity];
            _count = 0;
            _isInitialized = true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(TKey key, TValue value)
        {
            Debug.Assert(_isInitialized, NotInitializedMessage);
            if (FindSlot(key, out int index, out bool found))
            {
                if (found)
                {
                    ThrowKeyAlreadyExists(key);
                }
            }
            else
            {
                Resize();
                Add(key, value);
                return;
            }

            _keys[index] = key;
            _values[index] = value;
            _occupied[index] = true;
            _count++;

            if (_count >= _maxLoadCount)
            {
                Resize();
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdd(TKey key, TValue value)
        {
            Debug.Assert(_isInitialized, NotInitializedMessage);
            if (FindSlot(key, out int index, out bool found))
            {
                if (found)
                {
                    _values[index] = value;
                    return false;
                }
            }
            else
            {
                Resize();
                return TryAdd(key, value);
            }

            _keys[index] = key;
            _values[index] = value;
            _occupied[index] = true;
            _count++;

            if (_count >= _maxLoadCount)
            {
                Resize();
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet(TKey key, out TValue value)
        {
            Debug.Assert(_isInitialized, NotInitializedMessage);
            if (FindSlot(key, out int index, out bool found) && found)
            {
                value = _values[index];
                return true;
            }
            value = default;
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue Get(TKey key)
        {
            Debug.Assert(_isInitialized, NotInitializedMessage);
            if (TryGet(key, out TValue value))
            {
                return value;
            }

            ThrowKeyNotFound(key);
            return default; // never reached, but compiler requires
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has(TKey key)
        {
            Debug.Assert(_isInitialized, NotInitializedMessage);
            return FindSlot(key, out _, out bool found) && found;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear()
        {
            Debug.Assert(_isInitialized, NotInitializedMessage);
            if(_count <= 0)
            {
                _count = 0;
                return;
            }
            if (_isUnmanaged)
            {
                Array.Fill(_occupied, false);
                _count = 0;
                return; 
            }
            for (int i = 0; i < _occupied.Length; i++)
            {
                if (_occupied[i])
                {
                    _keys[i] = default;
                    _values[i] = default;
                    _occupied[i] = false;
                    _count--;
                    if(_count == 0)
                    {
                        break;
                    }
                }
            }
            _count = 0;
        }
        #endregion

        #region Throw
        private const string NotInitializedMessage = "Table is not initialized. Call Initialize before using.";
        private static void ThrowAlreadyInitialized()
        {
            throw new InvalidOperationException("Table is already initialized. To reinitialize use Reset (if implemented) or restart the application.");
        }
        private static void ThrowInvalidCapacity(int capacity)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Initial capacity must be greater than 0.");
        }
        private static void ThrowKeyAlreadyExists(TKey key)
        {
            throw new ArgumentException($"An element with the key '{key}' already exists.");
        }
        private static void ThrowKeyNotFound(TKey key)
        {
            throw new KeyNotFoundException($"The key '{key}' was not found in the dictionary.");
        }
        //private static void ThrowVersionChanged()
        //{
        //    throw new InvalidOperationException("Collection was modified during enumeration.");
        //}
        #endregion

        #region Internal
        private static bool FindSlot(TKey key, out int index, out bool found)
        {
            int hash = (key == null) ? 0 : key.GetHashCode();
            int startIndex = hash & _mask;
            int i = 0;
            index = startIndex;

            while (_occupied[index])
            {
                if (_keys[index].Equals(key))
                {
                    found = true;
                    return true;
                }
                i++;
                if (i >= _capacity)
                {
                    break;
                }
                index = (startIndex + i) & _mask;
            }

            if (!_occupied[index])
            {
                found = false;
                return true;
            }

            found = false;
            index = -1;
            return false;
        }

        private static void Resize()
        {
            int newCapacity = _capacity * 2;
            TKey[] oldKeys = _keys;
            TValue[] oldValues = _values;
            bool[] oldOccupied = _occupied;

            _capacity = newCapacity;
            _mask = _capacity - 1;
            _maxLoadCount = (int)(_capacity * LoadFactor);
            _maxLoadCount = _maxLoadCount < 4 ? 4 : _maxLoadCount;

            _keys = new TKey[newCapacity];
            _values = new TValue[newCapacity];
            _occupied = new bool[newCapacity];
            _count = 0;

            for (int i = 0; i < oldOccupied.Length; i++)
            {
                if (oldOccupied[i])
                {
                    ForceInsertInternal(oldKeys[i], oldValues[i]);
                }
            }
        }
        private static void ForceInsertInternal(TKey key, TValue value)
        {
            int hash = (key == null) ? 0 : key.GetHashCode();
            int index = hash & _mask;
            int i = 0;

            while (_occupied[index])
            {
                i++;
                index = (hash + i) & _mask;
            }

            _keys[index] = key;
            _values[index] = value;
            _occupied[index] = true;
            _count++;
        }
        private static int NextPowerOfTwo(int v)
        {
            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            return v < 4 ? 4 : v;
        }
        #endregion
    }
}