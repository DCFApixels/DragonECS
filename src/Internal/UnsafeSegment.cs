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
    [DebuggerTypeProxy(typeof(UnsafeSegment<>.DebuggerProxy))]
    internal readonly unsafe struct UnsafeSegment<T> : IEquatable<UnsafeSegment<T>>, IReadOnlyList<T>
        where T : unmanaged
    {
        public static readonly UnsafeSegment<T> Empty = new UnsafeSegment<T>(null, 0);
        internal readonly T* Ptr;
        internal readonly int Length;
        int IReadOnlyCollection<T>.Count { get { return Length; } }
        T IReadOnlyList<T>.this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)Length)
                {
                    Throw.ArgumentOutOfRange();
                }
                return Ptr[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeSegment(T* ptr, int length)
        {
            this.Ptr = ptr;
            Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeSegment<T> Slice(int start)
        {
            if ((uint)start > (uint)Length)
            {
                Throw.ArgumentOutOfRange();
            }
            return new UnsafeSegment<T>(Ptr + start, Length - start);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeSegment<T> Slice(int start, int length)
        {
            if ((uint)start > (uint)Length || (uint)length > (uint)(Length - start))
            {
                Throw.ArgumentOutOfRange();
            }
            return new UnsafeSegment<T>(Ptr + start, length);
        }

        public void CopyFromArray_Unchecked(T[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                Ptr[i] = array[i];
            }
        }
        public void Clear()
        {
            AsSpan().Clear();
        }
        public void CopyTo(Span<T> a)
        {
            AsSpan().CopyTo(a);
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null) { throw new ArgumentNullException(nameof(array)); }
            if ((uint)arrayIndex > (uint)array.Length) { throw new ArgumentOutOfRangeException(nameof(arrayIndex)); }
            if (Length > array.Length - arrayIndex) { throw new ArgumentException("The destination array is too small.", nameof(array)); }
            AsSpan().CopyTo(new Span<T>(array, arrayIndex, Length));
        }
        public bool Contains(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < Length; i++)
            {
                if (comparer.Equals(Ptr[i], item)) { return true; }
            }
            return false;
        }
        public void Fill(T value)
        {
            AsSpan().Fill(value);
        }

        public override string ToString()
        {
            T* ptr = this.Ptr;
            var elements = new T[Length];
            for (int i = 0; i < Length; i++)
            {
                elements[i] = ptr[i];
            }
            return CollectionUtility.AutoToString(elements, "span");
        }
        public Span<T> AsSpan() { return new Span<T>(Ptr, Length); }
        public T[] ToArray() { return AsSpan().ToArray(); }

        public static implicit operator Span<T>(UnsafeSegment<T> a) { return a.AsSpan(); }
        public static implicit operator ReadOnlySpan<T>(UnsafeSegment<T> a) { return a.AsSpan(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(UnsafeSegment<T> other)
        {
            return Ptr == other.Ptr && Length == other.Length;
        }
        public override bool Equals(object obj)
        {
            return obj is UnsafeSegment<T> other && Equals(other);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return ((IntPtr)Ptr).GetHashCode() * 397 ^ Length;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UnsafeSegment<T> left, UnsafeSegment<T> right)
        {
            return left.Equals(right);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UnsafeSegment<T> left, UnsafeSegment<T> right)
        {
            return !left.Equals(right);
        }

        #region Enumerator
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() { return new Enumerator(Ptr, Length); }
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T* _ptr;
            private readonly int _length;
            private int _index;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(T* ptr, int length)
            {
                _ptr = ptr;
                _length = length;
                _index = -1;
            }
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return _ptr[_index]; }
            }
            object IEnumerator.Current { get { return Current; } }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() { return ++_index < _length; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IDisposable.Dispose() { }
            void IEnumerator.Reset() { _index = -1; }
        }
        #endregion

        #region Debug
        internal class DebuggerProxy
        {
            public T[] Items;
            public int Length;
            public DebuggerProxy(UnsafeSegment<T> instance)
            {
                Length = instance.Length;
                Items = new T[Length];
                for (int i = 0; i < Length; i++)
                {
                    Items[i] = instance.Ptr[i];
                }
            }
        }
        #endregion
    }
}
