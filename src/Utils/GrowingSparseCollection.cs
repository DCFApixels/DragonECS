using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;

namespace DCFApixels
{
    public class GrowingSparseCollection<TValue>
    {
        private const int EMPTY = -1;

        private int[] _buckets = Array.Empty<int>();
        private Entry[] _entries = Array.Empty<Entry>();

        private int _capacity;
        private int _count;

        #region Properties
        public TValue this[int key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _entries[FindEntry(key)].value;
        }
        #endregion

        #region Add
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int key, TValue value)
        {
#if DEBUG
            if (Contains(key))
                throw new ArgumentException("Contains(key) is true");
#endif
            Insert(key, value);
        }
        #endregion

        #region Getter
        public bool TryGetValue(int key, out TValue value)
        {
            int index = IndexOfKey(key);
            if (index < 0)
            {
                value = default;
                return false;
            }
            value = _entries[index].value;
            return true;
        }
        #endregion

        #region Constructors
        public GrowingSparseCollection(int capacity)
        {
            Initialize(capacity);
        }
        #endregion

        #region Initialize
        private void Initialize(int capacity)
        {
            _capacity = HashHelpers.GetPrime(capacity);
            _buckets = new int[_capacity];

            for (int i = 0; i < _capacity; i++)
                _buckets[i] = EMPTY;

            _entries = new Entry[_capacity];
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

        #region Contains
        public bool Contains(int key)
        {
            return IndexOfKey(key) >= 0;
        }
        #endregion

        #region IndexOfKey/Find/Insert
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOfKey(int key)
        {
            key &= ~int.MinValue;
            for (int i = _buckets[key % _capacity]; i >= 0; i = _entries[i].next)
            {
                if (_entries[i].key == key)
                    return i;
            }
            return -1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindEntry(int key)
        {
            key &= ~int.MinValue;
            for (int i = _buckets[key % _capacity]; i >= 0; i = _entries[i].next)
            {
                if (_entries[i].key == key)
                    return i;
            }
            throw new KeyNotFoundException();
        }

        private void Insert(int key, TValue value)
        {
            key &= ~int.MinValue;
            int targetBucket = key % _capacity;

            for (int i = _buckets[targetBucket]; i >= 0; i = _entries[i].next)
            {
                if (_entries[i].key == key)
                {
                    _entries[i].value = value;
                    return;
                }
            }

            if (_count >= _entries.Length)
            {
                Resize();
                targetBucket = key % _capacity;
            }
            int index = _count;
            _count++;

            _entries[index].next = _buckets[targetBucket];
            _entries[index].key = key;
            _entries[index].value = value;
            _buckets[targetBucket] = index;
        }
        #endregion

        #region Resize
        private void Resize()
        {
            Resize(HashHelpers.ExpandPrime(_count), false);
        }
        private void Resize(int newSize, bool forceNewHashCodes)
        {
            _capacity = newSize;
            Contract.Assert(newSize >= _entries.Length);
            int[] newBuckets = new int[newSize];
            for (int i = 0; i < newBuckets.Length; i++)
            {
                newBuckets[i] = EMPTY;
            }
            Entry[] newEntries = new Entry[newSize];
            Array.Copy(_entries, 0, newEntries, 0, _count);
            if (forceNewHashCodes)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (newEntries[i].key != -1)
                    {
                        newEntries[i].key = newEntries[i].key;
                    }
                }
            }
            for (int i = 0; i < _count; i++)
            {
                if (newEntries[i].key >= 0)
                {
                    int bucket = newEntries[i].key % newSize;
                    newEntries[i].next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }
            _buckets = newBuckets;
            _entries = newEntries;
        }
        #endregion

        #region Utils
        private struct Entry
        {
            public int next;        // Index of next entry, -1 if last
            public int key;         // key & hash
            public TValue value;
        }
        #endregion
    }

    #region HashHelpers
    internal static class HashHelpers
    {
        public const int MaxPrimeArrayLength = 0x7FEFFFFD;
        public static readonly int[] primes = {
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
            17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
            187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369};

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static bool IsPrime(int candidate)
        {
            if ((candidate & 1) != 0)
            {
                int limit = (int)Math.Sqrt(candidate);
                for (int divisor = 3; divisor <= limit; divisor += 2)
                {
                    if ((candidate % divisor) == 0)
                        return false;
                }
                return true;
            }
            return (candidate == 2);
        }

        public static int ExpandPrime(int oldSize)
        {
            int newSize = 2 * oldSize;

            if ((uint)newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize)
            {
                Contract.Assert(MaxPrimeArrayLength == GetPrime(MaxPrimeArrayLength), "Invalid MaxPrimeArrayLength");
                return MaxPrimeArrayLength;
            }

            return GetPrime(newSize);
        }
        internal const int HashtableHashPrime = 101;

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static int GetPrime(int min)
        {
            if (min < 0)
            {
                throw new ArgumentException("min < 0"); //TODO
            }
            Contract.EndContractBlock();

            for (int i = 0; i < primes.Length; i++)
            {
                int prime = primes[i];
                if (prime >= min)
                    return prime;
            }

            for (int i = (min | 1); i < int.MaxValue; i += 2)
            {
                if (IsPrime(i) && ((i - 1) % HashtableHashPrime != 0))
                    return i;
            }
            return min;
        }
    }
    #endregion
}
