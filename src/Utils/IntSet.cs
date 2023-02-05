using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;

namespace Kibnet
{
    [Serializable]
    public class IntSet : ISet<int>, IReadOnlyCollection<int>
    {
        #region IntSet
        
        public IntSet() : this(false, false) { }

        public IntSet(IEnumerable<int> items) : this(false, false)
        {
            UnionWith(items);
        }

        public IntSet(bool isFastest) : this(isFastest, false) { }

        public IntSet(bool isFastest, bool isFull)
        {
            _isFastest = isFastest;
            if (isFull)
            {
                _count = UInt32.MaxValue;
                root.Full = true;
                root.Cards = null;
            }
        }

        protected bool _isFastest;

        protected long _count;

        public long LongCount => _count;

        public bool IsFastest
        {
            get => _isFastest;
            set => _isFastest = value;
        }

        public Card root = new Card(0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void IntersectWithIntSet(ISet<int> other)
        {
            foreach (var item in this)
            {
                if (!other.Contains(item))
                {
                    InternalRemove(item, true);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void IntersectWithEnumerable(IEnumerable<int> other)
        {
            var result = new IntSet();
            foreach (var item in other)
            {
                if (Contains(item))
                {
                    result.Add(item);
                }
            }

            root = result.root;
            _count = result._count;
        }

        /// <summary>
        /// Checks if this contains of other's elements. Iterates over other's elements and
        /// returns false as soon as it finds an element in other that's not in this.
        /// Used by SupersetOf, ProperSupersetOf, and SetEquals.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool ContainsAllElements(IEnumerable<int> other)
        {
            foreach (var element in other)
            {
                if (!Contains(element))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Implementation Notes:
        /// If other is a intset and is using same equality comparer, then checking subset is
        /// faster. Simply check that each element in this is in other.
        ///
        /// Note: if other doesn't use same equality comparer, then Contains check is invalid,
        /// which is why callers must take are of this.
        ///
        /// If callers are concerned about whether this is a proper subset, they take care of that.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsSubsetOfHashSetWithSameComparer(IntSet other)
        {
            foreach (var item in this)
            {
                if (!other.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines counts that can be used to determine equality, subset, and superset. This
        /// is only used when other is an IEnumerable and not a HashSet. If other is a HashSet
        /// these properties can be checked faster without use of marking because we can assume
        /// other has no duplicates.
        ///
        /// The following count checks are performed by callers:
        /// 1. Equals: checks if unfoundCount = 0 and uniqueFoundCount = _count; i.e. everything
        /// in other is in this and everything in this is in other
        /// 2. Subset: checks if unfoundCount >= 0 and uniqueFoundCount = _count; i.e. other may
        /// have elements not in this and everything in this is in other
        /// 3. Proper subset: checks if unfoundCount > 0 and uniqueFoundCount = _count; i.e
        /// other must have at least one element not in this and everything in this is in other
        /// 4. Proper superset: checks if unfound count = 0 and uniqueFoundCount strictly less
        /// than _count; i.e. everything in other was in this and this had at least one element
        /// not contained in other.
        ///
        /// An earlier implementation used delegates to perform these checks rather than returning
        /// an ElementCount struct; however this was changed due to the perf overhead of delegates.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="returnIfUnfound">Allows us to finish faster for equals and proper superset
        /// because unfoundCount must be 0.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected (int UniqueCount, int UnfoundCount) CheckUniqueAndUnfoundElements(IEnumerable<int> other, bool returnIfUnfound)
        {
            // Need special case in case this has no elements.
            if (_count == 0)
            {
                int numElementsInOther = 0;
                foreach (int item in other)
                {
                    numElementsInOther++;
                    break; // break right away, all we want to know is whether other has 0 or 1 elements
                }

                return (UniqueCount: 0, UnfoundCount: numElementsInOther);
            }

            Debug.Assert((root.Cards != null) && (_count > 0), "root.Cards was null but count greater than 0");

            int unfoundCount = 0; // count of items in other not found in this
            int uniqueFoundCount = 0; // count of unique items in other found in this


            var otherAsSet = new IntSet();

            foreach (int item in other)
            {
                if (Contains(item))
                {
                    if (!otherAsSet.Contains(item))
                    {
                        // Item hasn't been seen yet.
                        otherAsSet.Add(item);
                        uniqueFoundCount++;
                    }
                }
                else
                {
                    unfoundCount++;
                    if (returnIfUnfound)
                    {
                        break;
                    }
                }
            }

            return (uniqueFoundCount, unfoundCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SymmetricExceptWithUniqueHashSet(ISet<int> other)
        {
            foreach (int item in other)
            {
                if (!Remove(item))
                {
                    Add(item);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SymmetricExceptWithEnumerable(IEnumerable<int> other)
        {
            var itemsToRemove = new IntSet();

            var itemsAddedFromOther = new IntSet();

            foreach (int item in other)
            {
                if (Add(item))
                {
                    // wasn't already present in collection; flag it as something not to remove
                    // *NOTE* if location is out of range, we should ignore. BitHelper will
                    // detect that it's out of bounds and not try to mark it. But it's
                    // expected that location could be out of bounds because adding the item
                    // will increase _lastIndex as soon as all the free spots are filled.
                    itemsAddedFromOther.Add(item);
                }
                else
                {
                    // already there...if not added from other, mark for remove.
                    // *NOTE* Even though BitHelper will check that location is in range, we want
                    // to check here. There's no point in checking items beyond originalCount
                    // because they could not have been in the original collection
                    if (!itemsAddedFromOther.Contains(item))
                    {
                        itemsToRemove.Add(item);
                    }
                }
            }

            // if anything marked, remove it
            foreach (var item in itemsToRemove)
            {
                Remove(item);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool InternalRemove(int item, bool isFastest)
        {
            var card = root;
            for (int i = 0; i < 5; i++)
            {
                if (card.Full)
                {
                    //TODO Split card
                    card.Init(i, true);
                    card.Full = false;
                }

                var index = Card.GetIndex(item, i);
                if (i < 4)
                {
                    if (card.Cards[index] == null)
                    {
                        return false;
                    }

                    card = card.Cards[index];
                }
                else
                {
                    var bindex = index >> 3;
                    var mask = (byte) (1 << (index & 7));
                    if ((card.Bytes[bindex] & mask) == 0)
                    {
                        return false;
                    }

                    card.Bytes[bindex] ^= mask;
                    _count--;
                    if (isFastest == false && card.Bytes[bindex] == byte.MinValue)
                    {
                        var isEmpty = card.CheckEmpty();
                        if (isEmpty)
                        {
                            var parentCard0 = root.Cards[Card.GetIndex(item, 0)];
                            var parentCard1 = parentCard0.Cards[Card.GetIndex(item, 1)];
                            var parentCard2 = parentCard1.Cards[Card.GetIndex(item, 2)];
                            var parentCard3 = parentCard2.Cards[Card.GetIndex(item, 3)];
                            //var parentCard4 = parentCard3.Cards[indexes[4]];
                            //if (parentCard4.CheckFull())
                            if (parentCard3.CheckEmpty())
                                if (parentCard2.CheckEmpty())
                                    if (parentCard1.CheckEmpty())
                                        if (parentCard0.CheckEmpty())
                                            ;
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Card

        [Serializable]
        public class Card : IEnumerable<int>, IEnumerable<byte>
        {
            public Card() { }

            public Card(int level)
            {
                Init(level);
            }

            public Card Init(int level, bool isFull = false)
            {
                if (level < 4)
                {
                    Cards = new Card[64];
                    if (isFull)
                    {
                        for (int i = 0; i < 64; i++)
                        {
                            Cards[i] = new Card { Full = true };
                        }
                    }
                }
                else
                {
                    Bytes = new byte[32];
                    if (isFull)
                    {
                        for (int i = 0; i < 32; i++)
                        {
                            Bytes[i] = byte.MaxValue;
                        }
                    }
                }
                return this;
            }

            public Card[] Cards;
            public byte[] Bytes;
            public bool Full;

            [SecuritySafeCritical]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe int GetIndex(int value, int level)
            {
                var temp = (uint)(uint*)(value);
                var index = level switch
                {
                    0 => (temp) >> 26,
                    1 => (temp << 6) >> 26,
                    2 => (temp << 12) >> 26,
                    3 => (temp << 18) >> 26,
                    4 => (temp << 24) >> 24,
                };
                //TODO переписать на получение указателей
                return (int)(int*)index;
            }

            IEnumerator<int> IEnumerable<int>.GetEnumerator()
            {
                if (Cards != null)
                {
                    for (int i = 0; i < 64; i++)
                    {
                        if (Cards[i] != null)
                        {
                            yield return i;
                        }
                    }
                }
                else if (Full)
                {
                    for (int i = 0; i < 64; i++)
                    {
                        yield return i;
                    }
                }
            }

            IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
            {
                if (Bytes != null)
                {
                    for (int i = 0; i < 32; i++)
                    {
                        yield return Bytes[i];
                    }
                }
                else if (Full)
                {
                    for (int i = 0; i < 32; i++)
                    {
                        yield return byte.MaxValue;
                    }
                }
            }

            public IEnumerator GetEnumerator()
            {
                if (Bytes != null)
                {
                    return ((IEnumerable<byte>)this).GetEnumerator();
                }
                else
                {
                    return ((IEnumerable<int>)this).GetEnumerator();
                }
            }

            public override string ToString()
            {
                if (Bytes != null)
                {
                    return String.Create(256, this, GetMask);
                }
                else
                {
                    return String.Create(64, this, GetMask);
                }
            }

            public bool CheckFull()
            {
                if (Bytes != null)
                {
                    Full = Bytes.All(b => b == byte.MaxValue);
                    if (Full == true) Bytes = null;
                }
                if (Cards != null)
                {
                    Full = Cards.All(c => c != null && c.Full);
                    if (Full == true) Cards = null;
                }
                return Full;
            }

            public bool CheckEmpty()
            {
                if (Bytes != null)
                {
                    if (Bytes.All(b => b == byte.MinValue))
                    {
                        Full = false;
                        Bytes = null;
                        return true;
                    }
                    return false;
                }
                if (Cards != null)
                {
                    if (Cards.All(c => c == null || c.CheckEmpty()))
                    {
                        Full = false;
                        Cards = null;
                        return true;
                    }
                    return false;
                }
                return !Full;
            }

            public static void GetMask(Span<char> span, Card arg)
            {
                if (arg.Full == true)
                {
                    span.Fill('1');
                }
                else
                {
                    if (arg.Cards != null)
                    {
                        var count = arg.Cards.Length;
                        for (int i = 0; i < count; i++)
                        {
                            span[i] = arg.Cards[i] != null ? '1' : '0';
                        }
                        return;
                    }
                    if (arg.Bytes != null)
                    {
                        var count = arg.Bytes.Length;
                        var bytesize = 8;
                        var p = 0;
                        for (int i = 0; i < count; i++)
                        {
                            var b = arg.Bytes[i];
                            for (int j = 0; j < bytesize; j++)
                            {
                                span[p++] = ((b & 1) == 1) ? '1' : '0';
                                b >>= 1;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region IEnumerable

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<int> GetEnumerator()
        {
            foreach (var i0 in (IEnumerable<int>)root)
            {
                var cards1 = root.Full ? root : root.Cards[i0];
                if (cards1 != null)
                {
                    foreach (var i1 in (IEnumerable<int>)cards1)
                    {
                        var cards2 = cards1.Full ? cards1 : cards1.Cards[i1];
                        if (cards2 != null)
                        {
                            foreach (var i2 in (IEnumerable<int>)cards2)
                            {
                                var cards3 = cards2.Full ? cards2 : cards2.Cards[i2];
                                if (cards3 != null)
                                {
                                    foreach (var i3 in (IEnumerable<int>)cards3)
                                    {
                                        var bytes = cards3.Full ? cards3 : cards3.Cards[i3];
                                        if (bytes != null)
                                        {
                                            var bytecount = 0;
                                            foreach (var i4 in (IEnumerable<byte>)bytes)
                                            {
                                                for (int j = 0; j < 8; j++)
                                                {
                                                    var tail = i4 & (1 << j);
                                                    if (tail != 0)
                                                    {
                                                        var value = i0 << 26;
                                                        value |= i1 << 20;
                                                        value |= i2 << 14;
                                                        value |= i3 << 8;
                                                        value |= bytecount << 3;
                                                        value |= j;
                                                        yield return value;
                                                    }
                                                }

                                                bytecount++;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region ICollection<T>

        public int Count => (int)_count;

        public bool IsReadOnly { get; }
        
        void ICollection<int>.Add(int item) => Add(item);
        
        public void Clear()
        {
            root = new Card(0);
            _count = 0;
        }
        
        bool ICollection<int>.Contains(int item) => Contains(item);

        public bool Contains(int item)
        {
            var card = root;
            for (int i = 0; i < 5; i++)
            {
                if (card.Full)
                {
                    return true;
                }

                var index = Card.GetIndex(item, i);

                if (i < 4)
                {
                    if (card.Cards?[index] == null)
                        return false;
                    card = card.Cards[index];
                }
                else
                {
                    if (card.Bytes == null)
                    {
                        return false;
                    }
                    var bindex = index >> 3;
                    var mask = (byte)(1 << (index & 7));
                    return (card.Bytes[bindex] & mask) != 0;
                }
            }
            return false;
        }

        public void CopyTo(int[] array) => CopyTo(array, 0, Count);

        public void CopyTo(int[] array, int arrayIndex) => CopyTo(array, arrayIndex, Count);

        public void CopyTo(int[] array, int arrayIndex, int count)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            // Check array index valid index into array.
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, "Non-negative number required.");
            }

            // Also throw if count less than 0.
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Non-negative number required.");
            }

            // Will the array, starting at arrayIndex, be able to hold elements? Note: not
            // checking arrayIndex >= array.Length (consistency with list of allowing
            // count of 0; subsequent check takes care of the rest)
            if (arrayIndex > array.Length || count > array.Length - arrayIndex)
            {
                throw new ArgumentException("Destination array is not long enough to copy all the items in the collection. Check array index and length.");
            }

            var enumerator = GetEnumerator();
            for (int i = 0; i < _count && count != 0; i++)
            {
                if (enumerator.MoveNext())
                {
                    array[arrayIndex++] = enumerator.Current;
                    count--;
                }
            }
        }

        public bool Remove(int item) => InternalRemove(item, _isFastest);

        #endregion

        #region ISet<T>

        public bool Add(int item)
        {
            var card = root;
            for (int i = 0; i < 5; i++)
            {
                if (card.Full)
                {
                    return false;
                }

                var index = Card.GetIndex(item, i);
                if (i < 4)
                {
                    if (card.Cards == null)
                    {
                        card.Init(i);
                    }
                    if (card.Cards[index] == null)
                    {
                        var newcard = new Card(i + 1);
                        card.Cards[index] = newcard;
                        card = newcard;
                    }
                    else
                    {
                        card = card.Cards[index];
                    }
                }
                else
                {
                    if (card.Bytes == null)
                    {
                        card.Init(i);
                    }
                    var bindex = index >> 3;
                    var mask = (byte)(1 << (index & 7));
                    if ((card.Bytes[bindex] & mask) == 0)
                    {
                        card.Bytes[bindex] |= mask;
                        _count++;
                        if (_isFastest == false && card.Bytes[bindex] == byte.MaxValue)
                        {
                            var full = card.CheckFull();
                            if (full)
                            {
                                var parentCard0 = root.Cards[Card.GetIndex(item, 0)];
                                var parentCard1 = parentCard0.Cards[Card.GetIndex(item, 1)];
                                var parentCard2 = parentCard1.Cards[Card.GetIndex(item, 2)];
                                var parentCard3 = parentCard2.Cards[Card.GetIndex(item, 3)];
                                //var parentCard4 = parentCard3.Cards[indexes[4]];
                                //if (parentCard4.CheckFull())
                                if (parentCard3.CheckFull())
                                    if (parentCard2.CheckFull())
                                        if (parentCard1.CheckFull())
                                            if (parentCard0.CheckFull()) ;
                            }
                        }
                        return true;
                    }
                }
            }
            return false;
        }
        
        public void UnionWith(IEnumerable<int> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            foreach (var item in other)
            {
                Add(item);
            }
        }

        public void IntersectWith(IEnumerable<int> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            // Intersection of anything with empty set is empty set, so return if count is 0.
            // Same if the set intersecting with itself is the same set.
            if (Count == 0 || ReferenceEquals(other, this))
            {
                return;
            }

            // If other is known to be empty, intersection is empty set; remove all elements, and we're done.
            if (other is ICollection<int> otherAsCollection)
            {
                if (otherAsCollection.Count == 0)
                {
                    Clear();
                    return;
                }

                if (other is ISet<int> otherAsSet)
                {
                    IntersectWithIntSet(otherAsSet);
                    return;
                }
            }

            IntersectWithEnumerable(other);
        }

        public void ExceptWith(IEnumerable<int> other)
        {
            foreach (var item in other)
            {
                Remove(item);
            }
        }

        public void SymmetricExceptWith(IEnumerable<int> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            // If set is empty, then symmetric difference is other.
            if (Count == 0)
            {
                UnionWith(other);
                return;
            }

            // Special-case this; the symmetric difference of a set with itself is the empty set.
            if (other == this)
            {
                Clear();
                return;
            }

            // If other is a HashSet, it has unique elements according to its equality comparer,
            // but if they're using different equality comparers, then assumption of uniqueness
            // will fail. So first check if other is a hashset using the same equality comparer;
            // symmetric except is a lot faster and avoids bit array allocations if we can assume
            // uniqueness.
            if (other is ISet<int> otherAsSet)
            {
                SymmetricExceptWithUniqueHashSet(otherAsSet);
            }
            else
            {
                SymmetricExceptWithEnumerable(other);
            }
        }

        public bool IsSubsetOf(IEnumerable<int> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            // The empty set is a subset of any set, and a set is a subset of itself.
            // Set is always a subset of itself
            if (Count == 0 || other == this)
            {
                return true;
            }

            // Faster if other has unique elements according to this equality comparer; so check
            // that other is a hashset using the same equality comparer.
            if (other is IntSet otherAsSet)
            {
                // if this has more elements then it can't be a subset
                if (Count > otherAsSet.Count)
                {
                    return false;
                }

                // already checked that we're using same equality comparer. simply check that
                // each element in this is contained in other.
                return IsSubsetOfHashSetWithSameComparer(otherAsSet);
            }

            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
            return uniqueCount == Count && unfoundCount >= 0;
        }

        public bool IsSupersetOf(IEnumerable<int> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            // A set is always a superset of itself.
            if (other == this)
            {
                return true;
            }

            // Try to fall out early based on counts.
            if (other is ICollection<int> otherAsCollection)
            {
                // If other is the empty set then this is a superset.
                if (otherAsCollection.Count == 0)
                {
                    return true;
                }

                // Try to compare based on counts alone if other is a hashset with same equality comparer.
                if (other is IntSet otherAsSet && otherAsSet.Count > Count)
                {
                    return false;
                }
            }

            return ContainsAllElements(other);
        }

        public bool IsProperSupersetOf(IEnumerable<int> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            // The empty set isn't a proper superset of any set, and a set is never a strict superset of itself.
            if (Count == 0 || other == this)
            {
                return false;
            }

            if (other is ICollection<int> otherAsCollection)
            {
                // If other is the empty set then this is a superset.
                if (otherAsCollection.Count == 0)
                {
                    // Note that this has at least one element, based on above check.
                    return true;
                }

                // Faster if other is a hashset with the same equality comparer
                if (other is IntSet otherAsSet)
                {
                    if (otherAsSet.Count >= Count)
                    {
                        return false;
                    }

                    // Now perform element check.
                    return ContainsAllElements(otherAsSet);
                }
            }

            // Couldn't fall out in the above cases; do it the long way
            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
            return uniqueCount < Count && unfoundCount == 0;
        }

        public bool IsProperSubsetOf(IEnumerable<int> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            // No set is a proper subset of itself.
            if (other == this)
            {
                return false;
            }

            if (other is ICollection<int> otherAsCollection)
            {
                // No set is a proper subset of an empty set.
                if (otherAsCollection.Count == 0)
                {
                    return false;
                }

                // The empty set is a proper subset of anything but the empty set.
                if (Count == 0)
                {
                    return otherAsCollection.Count > 0;
                }

                // Faster if other is a hashset (and we're using same equality comparer).
                if (other is IntSet otherAsSet)
                {
                    if (Count >= otherAsSet.Count)
                    {
                        return false;
                    }

                    // This has strictly less than number of items in other, so the following
                    // check suffices for proper subset.
                    return IsSubsetOfHashSetWithSameComparer(otherAsSet);
                }
            }

            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
            return uniqueCount == Count && unfoundCount > 0;
        }

        public bool Overlaps(IEnumerable<int> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (Count == 0)
            {
                return false;
            }

            // Set overlaps itself
            if (other == this)
            {
                return true;
            }

            foreach (var element in other)
            {
                if (Contains(element))
                {
                    return true;
                }
            }

            return false;
        }

        public bool SetEquals(IEnumerable<int> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            // A set is equal to itself.
            if (other == this)
            {
                return true;
            }

            // Faster if other is a hashset and we're using same equality comparer.
            if (other is IntSet otherAsSet)
            {
                // Attempt to return early: since both contain unique elements, if they have
                // different counts, then they can't be equal.
                if (Count != otherAsSet.Count)
                {
                    return false;
                }

                // Already confirmed that the sets have the same number of distinct elements, so if
                // one is a superset of the other then they must be equal.
                return ContainsAllElements(otherAsSet);
            }
            else
            {
                // If this count is 0 but other contains at least one element, they can't be equal.
                if (Count == 0 &&
                    other is ICollection<int> otherAsCollection &&
                    otherAsCollection.Count > 0)
                {
                    return false;
                }

                (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
                return uniqueCount == Count && unfoundCount == 0;
            }
        }

        #endregion
    }
}