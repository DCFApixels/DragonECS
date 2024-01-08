#if ENABLE_DUMMY_SPAN
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using EditorBrowsableAttribute = System.ComponentModel.EditorBrowsableAttribute;
using EditorBrowsableState = System.ComponentModel.EditorBrowsableState;

namespace DCFApixels.DragonECS
{
    internal static class ThrowHelper
    {
        public static void ThrowIndexOutOfRangeException() => throw new IndexOutOfRangeException();
        public static void ThrowArgumentOutOfRangeException() => throw new ArgumentOutOfRangeException();
        public static void ThrowInvalidOperationException() => throw new InvalidOperationException();
    }
    [DebuggerDisplay("{ToString(),raw}")]
    public readonly ref struct ReadOnlySpan<T>
    {
        public static ReadOnlySpan<T> Empty => new ReadOnlySpan<T>(null);

        internal readonly T[] _array;
        private readonly int _start;
        private readonly int _length;

        #region Properties
        public int Length => _length;
        public bool IsEmpty => _length == 0;
        public ref readonly T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)_length || (uint)index < 0)
                    ThrowHelper.ThrowIndexOutOfRangeException();
                return ref _array[index + _start];
            }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan(T[] array)
        {
            _array = array ?? Array.Empty<T>();
            _start = 0;
            _length = array.Length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan(T[] array, int start, int length)
        {
            if (array == null)
            {
                if (start != 0 || length != 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException();
                _array = Array.Empty<T>();
                _start = 0;
                _length = 0;
                return;
            }

            if ((uint)start > (uint)array.Length || (uint)length > (uint)(array.Length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();

            _array = array;
            _start = start;
            _length = length;
        }
        #endregion

        #region Object
#pragma warning disable CS0809 // Устаревший член переопределяет неустаревший член
        [Obsolete("Equals() on ReadOnlySpan will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new NotSupportedException();
        [Obsolete("GetHashCode() on ReadOnlySpan will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new NotSupportedException();
#pragma warning restore CS0809 // Устаревший член переопределяет неустаревший член
        public override string ToString()
        {
            //if (typeof(T) == typeof(char))
            //    return new string(new ReadOnlySpan<char>(ref Unsafe.As<T, char>(ref _reference), _length));
            return $"System.ReadOnlySpan<{typeof(T).Name}>[{_length}]";
        }
        #endregion

        #region operators
        public static bool operator !=(ReadOnlySpan<T> left, ReadOnlySpan<T> right) => !(left == right);

        public static implicit operator ReadOnlySpan<T>(T[] array) => new ReadOnlySpan<T>(array);

        public static implicit operator ReadOnlySpan<T>(ArraySegment<T> segment) => new ReadOnlySpan<T>(segment.Array, segment.Offset, segment.Count);
        public static bool operator ==(ReadOnlySpan<T> left, ReadOnlySpan<T> right) => left._length == right._length && left._array == right._array;
        #endregion

        #region Enumerator
        public Enumerator GetEnumerator() => new Enumerator(this);
        public ref struct Enumerator
        {
            private readonly ReadOnlySpan<T> _span;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(ReadOnlySpan<T> span)
            {
                _span = span;
                _index = span._start - 1;
            }
            public ref readonly T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _span[_index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                int index = _index + 1;
                if (index < _span.Length)
                {
                    _index = index;
                    return true;
                }
                return false;
            }
        }
        #endregion

        #region Other
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref readonly T GetPinnableReference()
        {
            if (_length != 0) ThrowHelper.ThrowInvalidOperationException();
            return ref _array[0];
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public void CopyTo(Span<T> destination)
        //{
        //    if ((uint)_length <= (uint)destination.Length)
        //    {
        //        Buffer.Memmove(ref destination._reference, ref _reference, (uint)_length);
        //    }
        //    else
        //    {
        //        ThrowHelper.ThrowArgumentException_DestinationTooShort();
        //    }
        //}

        //public bool TryCopyTo(Span<T> destination)
        //{
        //    bool retVal = false;
        //    if ((uint)_length <= (uint)destination.Length)
        //    {
        //        Buffer.Memmove(ref destination._reference, ref _reference, (uint)_length);
        //        retVal = true;
        //    }
        //    return retVal;
        //}
        public void CopyTo(T[] array)
        {
            if (_length > array.Length)
            {
                throw new ArgumentOutOfRangeException();
            }

            for (int i = 0; i < _length; i++)
            {
                array[i] = _array[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> Slice(int start)
        {
            if ((uint)start > (uint)_length)
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new ReadOnlySpan<T>(_array, _start + start, _length - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> Slice(int start, int length)
        {
            if ((uint)start > (uint)_length || (uint)length > (uint)(_length - start))
                ThrowHelper.ThrowArgumentOutOfRangeException();

            return new ReadOnlySpan<T>(_array, _start + start, length);
        }

        public T[] ToArray()
        {
            if (_length == 0)
                return Array.Empty<T>();
            var result = new T[_length];
            Array.Copy(_array, _start, result, 0, _length);
            return result;
        }
        #endregion
    }
}
#endif