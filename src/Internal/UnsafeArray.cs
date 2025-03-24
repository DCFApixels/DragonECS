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

namespace DCFApixels.DragonECS.Internal
{
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    public static unsafe class TempBuffer<T> where T : unmanaged
    {
        [ThreadStatic] private static T* _ptr;
        [ThreadStatic] private static int _size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Get(int size)
        {
            if (_size < size)
            {
                if (_ptr != null)
                {
                    UnmanagedArrayUtility.Free(_ptr);
                }
                _ptr = UnmanagedArrayUtility.New<T>(size);
                _size = size;
            }
            return _ptr;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear()
        {
            for (int i = 0; i < _size; i++)
            {
                _ptr[i] = default;
            }
        }
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [DebuggerTypeProxy(typeof(UnsafeArray<>.DebuggerProxy))]
    internal unsafe struct UnsafeArray<T> : IDisposable, IEnumerable<T>
        where T : unmanaged
    {
        internal T* ptr;
        internal int Length;

        public static UnsafeArray<T> FromArray(T[] array)
        {
            UnsafeArray<T> result = new UnsafeArray<T>(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                result.ptr[i] = array[i];
            }
            return result;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray(int length)
        {
            UnmanagedArrayUtility.New(out ptr, length);
            Length = length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray(int length, bool isInit)
        {
            UnmanagedArrayUtility.NewAndInit(out ptr, length);
            Length = length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UnsafeArray(T* ptr, int length)
        {
            this.ptr = ptr;
            Length = length;
        }

        public static UnsafeArray<T> Manual(T* ptr, int length)
        {
            return new UnsafeArray<T>(ptr, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray<T> Slice(int start)
        {
            if ((uint)start > (uint)Length)
            {
                Throw.ArgumentOutOfRange();
            }
            return new UnsafeArray<T>(ptr + start, Length - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray<T> Slice(int start, int length)
        {
            if ((uint)start > (uint)Length || (uint)length > (uint)(Length - start))
            {
                Throw.ArgumentOutOfRange();
            }
            return new UnsafeArray<T>(ptr + start, length);
        }

        public void CopyFromArray_Unchecked(T[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                ptr[i] = array[i];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray<T> Clone()
        {
            return new UnsafeArray<T>(UnmanagedArrayUtility.Clone(ptr, Length), Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            UnmanagedArrayUtility.Free(ref ptr, ref Length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadonlyDispose()
        {
            UnmanagedArrayUtility.Free(ptr);
        }
        public override string ToString()
        {
            T* ptr = this.ptr;
            var elements = new T[Length];
            for (int i = 0; i < Length; i++)
            {
                elements[i] = ptr[i];
            }
            return CollectionUtility.AutoToString(elements, "ua");
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() { return new Enumerator(ptr, Length); }
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
            void IEnumerator.Reset() { throw new NotSupportedException(); }
        }

        internal class DebuggerProxy
        {
            public T[] elements;
            public int length;
            public DebuggerProxy(UnsafeArray<T> instance)
            {
                length = instance.Length;
                elements = new T[length];
                for (int i = 0; i < length; i++)
                {
                    elements[i] = instance.ptr[i];
                }
            }
        }
    }
}
