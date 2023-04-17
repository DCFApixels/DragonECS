using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels
{
    public class SparseArray<TValue>
    {
        public const int MIN_CAPACITY = 16;
        private const int EMPTY = -1;

        private int[] _buckets = Array.Empty<int>();
        private Entry[] _entries = Array.Empty<Entry>();

        private int _count;

        private int _freeList;
        private int _freeCount;

        #region Properties
        public TValue this[int key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _entries[FindEntry(key)].value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Insert(key, value);
        }

        public int Count => _count;
        #endregion

        #region Constructors
        public SparseArray(int capacity = MIN_CAPACITY)
        {
            _buckets = new int[capacity];
            for (int i = 0; i < capacity; i++)
                _buckets[i] = EMPTY;
            _entries = new Entry[capacity];
        }
        #endregion

        #region Add
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int key, TValue value)
        {
#if DEBUG
            if (Contains(key))
                throw new ArgumentException("Contains(hashKey) is true");
#endif
            Insert(key, value);
        }
        #endregion

        #region Find/Insert/Remove
        private int FindEntry(int key)
        {
            key &= 0x7FFFFFFF;
            for (int i = _buckets[key % _buckets.Length]; i >= 0; i = _entries[i].next)
            {
                if (_entries[i].hashKey == key) return i;
            }
            return -1;
        }
        private void Insert(int key, TValue value)
        {
            key &= 0x7FFFFFFF;
            int targetBucket = key % _buckets.Length;

            for (int i = _buckets[targetBucket]; i >= 0; i = _entries[i].next)
            {
                if (_entries[i].hashKey == key)
                {
                    _entries[i].value = value;
                    return;
                }
            }

            int index;
            if (_freeCount > 0)
            {
                index = _freeList;
                _freeList = _entries[index].next;
                _freeCount--;
            }
            else
            {
                if (_count == _entries.Length)
                {
                    Resize();
                    targetBucket = key % _buckets.Length;
                }
                index = _count;
                _count++;
            }

            _entries[index].next = _buckets[targetBucket];
            _entries[index].hashKey = key;
            _entries[index].value = value;
            _buckets[targetBucket] = index;
        }
        public bool Remove(int key)
        {
            key &= 0x7FFFFFFF;
            int bucket = key % _buckets.Length;
            int last = -1;
            for (int i = _buckets[bucket]; i >= 0; last = i, i = _entries[i].next)
            {
                if (_entries[i].hashKey == key)
                {
                    if (last < 0)
                    {
                        _buckets[bucket] = _entries[i].next;
                    }
                    else
                    {
                        _entries[last].next = _entries[i].next;
                    }
                    _entries[i].next = _freeList;
                    _entries[i].hashKey = -1;
                    _entries[i].value = default;
                    _freeList = i;
                    _freeCount++;
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region TryGetValue
        public bool TryGetValue(int key, out TValue value)
        {
            int index = FindEntry(key);
            if (index < 0)
            {
                value = default;
                return false;
            }
            value = _entries[index].value;
            return true;
        }
        #endregion

        #region Contains
        public bool Contains(int key)
        {
            return FindEntry(key) >= 0;
        }
        #endregion

        #region Clear
        public void Clear()
        {
            if (_count > 0)
            {
                for (int i = 0; i < _buckets.Length; i++)
                {
                    _buckets[i] = -1;
                }
                Array.Clear(_entries, 0, _count);
                _count = 0;
            }
        }
        #endregion

        #region Resize
        private void Resize()
        {
            int newSize = _buckets.Length << 1;

            Contract.Assert(newSize >= _entries.Length);
            int[] newBuckets = new int[newSize];
            for (int i = 0; i < newBuckets.Length; i++)
                newBuckets[i] = EMPTY;

            Entry[] newEntries = new Entry[newSize];
            Array.Copy(_entries, 0, newEntries, 0, _count);
            for (int i = 0; i < _count; i++)
            {
                if (newEntries[i].hashKey >= 0)
                {
                    int bucket = newEntries[i].hashKey % newSize;
                    newEntries[i].next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }
            _buckets = newBuckets;
            _entries = newEntries;
        }
        #endregion

        #region Utils
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct Entry
        {
            public int next;        // Index of next entry, -1 if last
            public int hashKey;        
            public TValue value;
        }
        #endregion
    }
}