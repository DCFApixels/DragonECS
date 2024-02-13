//SparseArray64. Analogous to Dictionary<long, T>, but faster.
//Benchmark result of indexer.get speed test with 300 elements:
//[Dictinary: 6.705us] [SparseArray64: 2.512us].
using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS.Internal
{
    internal class SparseArray64<TValue>
    {
        public const int MIN_CAPACITY_BITS_OFFSET = 4;
        public const int MIN_CAPACITY = 1 << MIN_CAPACITY_BITS_OFFSET;
        private const int EMPTY = -1;

        private int[] _buckets = Array.Empty<int>();
        private Entry[] _entries = Array.Empty<Entry>();

        private int _count;

        private int _freeList;
        private int _freeCount;

        private int _modBitMask;

        #region Properties
        public ref TValue this[long keyX, long keyY]
        {
            get => ref _entries[FindEntry(keyX + (keyY << 32))].value;
            //set => Insert(keyX + (keyY << 32), value);
        }
        public ref TValue this[long key]
        {
            get => ref _entries[FindEntry(key)].value;
            //set => Insert(key, value);
        }

        public int Count => _count;
        #endregion

        #region Constructors
        public SparseArray64(int minCapacity = MIN_CAPACITY)
        {
            minCapacity = NormalizeCapacity(minCapacity);
            _buckets = new int[minCapacity];
            for (int i = 0; i < minCapacity; i++)
                _buckets[i] = EMPTY;
            _entries = new Entry[minCapacity];
            _modBitMask = (minCapacity - 1) & 0x7FFFFFFF;
        }
        #endregion

        #region Add
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(long keyX, long keyY, TValue value) => Add(keyX + (keyY << 32), value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(long key, TValue value)
        {
#if DEBUG
            if (Contains(key))
                throw new ArgumentException("Contains(hashKey) is true");
#endif
            Insert(key, value);
        }
        #endregion

        #region Find/Insert/Remove
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindEntry(long key)
        {
            for (int i = _buckets[unchecked((int)key & _modBitMask)]; i >= 0; i = _entries[i].next)
                if (_entries[i].hashKey == key) return i;
            return -1;
        }
        private void Insert(long key, TValue value)
        {
            int targetBucket = unchecked((int)key & _modBitMask);

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
                    targetBucket = unchecked((int)key & _modBitMask);
                }
                index = _count++;
            }

            _entries[index].next = _buckets[targetBucket];
            _entries[index].hashKey = key;
            _entries[index].value = value;
            _buckets[targetBucket] = index;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(long keyX, long keyY) => Remove(keyX + (keyY << 32));
        public bool Remove(long key)
        {
            int bucket = unchecked((int)key & _modBitMask);
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
        public bool TryGetValue(long key, out TValue value)
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
        public bool TryGetValue(long keyX, long keyY, out TValue value)
        {
            int index = FindEntry(keyX + (keyY << 32));
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
        public bool Contains(long keyX, long keyY)
        {
            return FindEntry(keyX + (keyY << 32)) >= 0;
        }
        public bool Contains(long key)
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
            _modBitMask = (newSize - 1) & 0x7FFFFFFF;

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
                    int bucket = unchecked((int)newEntries[i].hashKey & _modBitMask);
                    newEntries[i].next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }
            _buckets = newBuckets;
            _entries = newEntries;
        }

        private int NormalizeCapacity(int capacity)
        {
            int result = MIN_CAPACITY;
            while (result < capacity) result <<= 1;
            return result;
        }
        #endregion

        #region Utils
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct Entry
        {
            public int next;        // Index of next entry, -1 if last
            public long hashKey;
            public TValue value;
        }
        #endregion
    }
}