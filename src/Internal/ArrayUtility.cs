using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS.Internal
{
    internal static class ArrayUtility
    {
        private static int GetHighBitNumber(uint bits)
        {
            if (bits == 0)
            {
                return -1;
            }
            int bit = 0;
            if ((bits & 0xFFFF0000) != 0)
            {
                bits >>= 16;
                bit |= 16;
            }
            if ((bits & 0xFF00) != 0)
            {
                bits >>= 8;
                bit |= 8;
            }
            if ((bits & 0xF0) != 0)
            {
                bits >>= 4;
                bit |= 4;
            }
            if ((bits & 0xC) != 0)
            {
                bits >>= 2;
                bit |= 2;
            }
            if ((bits & 0x2) != 0)
            {
                bit |= 1;
            }
            return bit;
        }
        public static int NormalizeSizeToPowerOfTwo(int minSize)
        {
            unchecked
            {
                return 1 << (GetHighBitNumber((uint)minSize - 1u) + 1);
            }
        }
        public static int NormalizeSizeToPowerOfTwo_ClampOverflow(int minSize)
        {
            unchecked
            {
                int hibit = (GetHighBitNumber((uint)minSize - 1u) + 1);
                if (hibit >= 32)
                {
                    return int.MaxValue;
                }
                return 1 << hibit;
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
        public static EnumerableInt Range(int start, int length) => new EnumerableInt(start, length);
        public static EnumerableInt StartEnd(int start, int end) => new EnumerableInt(start, end - start);
        IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() => new Enumerator(start, start + length);
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
                get => _current;
            }
            object IEnumerator.Current => Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++_current < _max;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() { }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() { }
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
                *(newPointer + i) = 0;

            return (T*)newPointer;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NewAndInit<T>(out T* ptr, int capacity) where T : unmanaged
        {
            int newSize = MetaCache<T>.Size * capacity;
            byte* newPointer = (byte*)Marshal.AllocHGlobal(newSize).ToPointer();

            for (int i = 0; i < newSize; i++)
                *(newPointer + i) = 0;

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