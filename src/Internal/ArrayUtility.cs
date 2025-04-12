#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS.Internal
{
    internal interface ILinkedNext
    {
        int Next { get; }
    }
    internal readonly struct LinkedListCountIterator<T> : IEnumerable<T>
        where T : ILinkedNext
    {
        public readonly T[] Array;
        public readonly int Count;
        public readonly int StartIndex;
        public LinkedListCountIterator(T[] array, int count, int startIndex)
        {
            Array = array;
            Count = count;
            StartIndex = startIndex;
        }
        public Enumerator GetEnumerator()
        {
            return new Enumerator(Array, Count, StartIndex);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _array;
            private readonly int _count;
            private int _index;
            private int _counter;
            public Enumerator(T[] array, int count, int index)
            {
                _array = array;
                _count = count;
                _index = index;
                _counter = 0;
            }
            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return ref _array[_index]; }
            }
            T IEnumerator<T>.Current { get { return _array[_index]; } }
            object IEnumerator.Current { get { return Current; } }
            public bool MoveNext()
            {
                if (++_counter > _count) { return false; }
                if (_counter > 1)
                {
                    _index = _array[_index].Next;
                }
                return true;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IDisposable.Dispose() { }
            void IEnumerator.Reset() { throw new NotSupportedException(); }
        }
    }
    internal readonly struct LinkedListIterator<T> : IEnumerable<T>
        where T : ILinkedNext
    {
        public readonly T[] Array;
        public readonly int EndIndex;
        public readonly int StartIndex;
        public LinkedListIterator(T[] array, int endIndex, int startIndex)
        {
            Array = array;
            EndIndex = endIndex;
            StartIndex = startIndex;
        }
        public Enumerator GetEnumerator()
        {
            return new Enumerator(Array, EndIndex, StartIndex);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] _array;
            private readonly int _endIndex;
            private readonly int _startIndex;
            private int _nextIndex;
            private int _index;
            public ref T Current { get { return ref _array[_index]; } }
            T IEnumerator<T>.Current { get { return Current; } }
            object IEnumerator.Current { get { return Current; } }
            public Enumerator(T[] array, int endIndex, int head)
            {
                _array = array;
                _startIndex = head;
                _nextIndex = _startIndex;
                _endIndex = endIndex;
                _index = _endIndex;
            }
            public bool MoveNext()
            {
                if (_nextIndex < 0) { return false; }
                _index = _nextIndex;
                _nextIndex = _array[_index].Next;
                return true;
            }
            public void Dispose() { }
            public void Reset()
            {
                _nextIndex = _startIndex;
                _index = _endIndex;
            }
        }
    }


    internal static class ArrayUtility
    {
        //TODO потестить
        public static void ResizeOrCreate<T>(ref T[] array, int newSize)
        {
            if (array == null)
            {
                array = new T[newSize];
            }
            {
                Array.Resize(ref array, newSize);
            }
        }
        internal static void UpsizeTwoHead<T>(ref T[] array, int newLength, int separationIndex)
        {
            UpsizeTwoHead(ref array, newLength, separationIndex, array.Length - separationIndex);
        }
        internal static void UpsizeTwoHead<T>(ref T[] array, int newLength, int leftHeadLength, int rightHeadLength)
        {
            if (array.Length > newLength) { return; }
            var result = new T[newLength];
            Array.Copy(array, result, leftHeadLength); // copy left head
            Array.Copy(array, array.Length - rightHeadLength, result, array.Length - rightHeadLength, rightHeadLength); // copy right head
            array = result;
        }

        public static int NextPow2(int v)
        {
            unchecked
            {
                v--;
                v |= v >> 1;
                v |= v >> 2;
                v |= v >> 4;
                v |= v >> 8;
                v |= v >> 16;
                return ++v;
            }
        }
        public static int NextPow2_ClampOverflow(int v)
        {
            unchecked
            {
                const int NO_SIGN_HIBIT = 0x40000000;
                if ((v & NO_SIGN_HIBIT) != 0)
                {
                    return int.MaxValue;
                }
                return NextPow2(v);
            }
        }

        public static void Fill<T>(T[] array, T value, int startIndex = 0, int length = -1)
        {
            if (length < 0)
            {
                length = array.Length;
            }
            else
            {
                length = startIndex + length;
            }
            for (int i = startIndex; i < length; i++)
            {
                array[i] = value;
            }
        }


        public static void UpsizeWithoutCopy<T>(ref T[] array, int minSize)
        {
            if (array == null || minSize > array.Length)
            {
                array = new T[minSize];
            }
        }
        public static void Upsize<T>(ref T[] array, int minSize)
        {
            if (array == null)
            {
                array = new T[minSize];
            }
            else if (minSize > array.Length)
            {
                Array.Resize(ref array, minSize);
            }
        }
        public static void UpsizeToNextPow2<T>(ref T[] array, int minSize)
        {
            if (array == null)
            {
                minSize = NextPow2(minSize);
                array = new T[minSize];
            }
            else if (minSize > array.Length)
            {
                minSize = NextPow2(minSize);
                Array.Resize(ref array, minSize);
            }
        }
    }
    internal readonly struct EnumerableInt : IEnumerable<int>
    {
        public readonly int start;
        public readonly int length;
        private EnumerableInt(int start, int length)
        {
            this.start = start;
            this.length = length;
        }
        public static EnumerableInt Range(int start, int length) { return new EnumerableInt(start, length); }
        public static EnumerableInt StartEnd(int start, int end) { return new EnumerableInt(start, end - start); }
        IEnumerator<int> IEnumerable<int>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() { return new Enumerator(start, start + length); }
        public struct Enumerator : IEnumerator<int>
        {
            private readonly int _max;
            private int _current;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(int max, int current)
            {
                _max = max;
                _current = current - 1;
            }
            public int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return _current; }
            }
            object IEnumerator.Current { get { return Current; } }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() { return ++_current < _max; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IDisposable.Dispose() { }
            void IEnumerator.Reset() { throw new NotSupportedException(); }

        }
    }
    internal static unsafe class UnmanagedArrayUtility
    {
        private static class MetaCache<T>
        {
            public readonly static int Size;
            static MetaCache()
            {
                T def = default;
                Size = Marshal.SizeOf(def);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* New<T>(int capacity) where T : unmanaged
        {
            //Console.WriteLine($"{typeof(T).Name} - {Marshal.SizeOf<T>()} - {capacity} - {Marshal.SizeOf<T>() * capacity}");
            return (T*)Marshal.AllocHGlobal(Marshal.SizeOf<T>() * capacity).ToPointer();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void New<T>(out T* ptr, int capacity) where T : unmanaged
        {
            ptr = (T*)Marshal.AllocHGlobal(Marshal.SizeOf<T>() * capacity).ToPointer();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* NewAndInit<T>(int capacity) where T : unmanaged
        {
            int newSize = MetaCache<T>.Size * capacity;
            byte* newPointer = (byte*)Marshal.AllocHGlobal(newSize).ToPointer();

            for (int i = 0; i < newSize; i++)
            {
                *(newPointer + i) = 0;
            }

            return (T*)newPointer;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NewAndInit<T>(out T* ptr, int capacity) where T : unmanaged
        {
            int newSize = MetaCache<T>.Size * capacity;
            byte* newPointer = (byte*)Marshal.AllocHGlobal(newSize).ToPointer();

            for (int i = 0; i < newSize; i++)
            {
                *(newPointer + i) = 0;
            }

            ptr = (T*)newPointer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(void* pointer)
        {
            Marshal.FreeHGlobal(new IntPtr(pointer));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free<T>(ref T* pointer, ref int length) where T : unmanaged
        {
            Marshal.FreeHGlobal(new IntPtr(pointer));
            pointer = null;
            length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Clone<T>(T* sourcePtr, int length) where T : unmanaged
        {
            T* clone = New<T>(length);
            for (int i = 0; i < length; i++)
            {
                clone[i] = sourcePtr[i];
            }
            return clone;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Resize<T>(void* oldPointer, int newCount) where T : unmanaged
        {
            return (T*)(Marshal.ReAllocHGlobal(
                new IntPtr(oldPointer),
                new IntPtr(MetaCache<T>.Size * newCount))).ToPointer();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* ResizeAndInit<T>(void* oldPointer, int oldSize, int newSize) where T : unmanaged
        {
            int sizeT = MetaCache<T>.Size;
            T* result = (T*)Marshal.ReAllocHGlobal(
                new IntPtr(oldPointer),
                new IntPtr(sizeT * newSize)).ToPointer();
            Init((byte*)result, sizeT * oldSize, sizeT * newSize);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Init(byte* pointer, int startByteIndex, int endByteIndex)
        {
            for (int i = startByteIndex; i < endByteIndex; i++)
            {
                *(pointer + i) = 0;
            }
        }
    }

    public static class CollectionUtility
    {
        public static string EntitiesToString(IEnumerable<int> range, string name)
        {
            return $"{name}({range.Count()}) {{{string.Join(", ", range.OrderBy(o => o))}}})";
        }
        public static string AutoToString<T>(IEnumerable<T> range, string name)
        {
            return $"{name}({range.Count()}) {{{string.Join(", ", range.Select(o => o.ToString()))}}})";
        }
    }
}