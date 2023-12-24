using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

namespace DCFApixels.DragonECS.Utils
{
    internal static class ArrayUtility
    {
        public static void Fill<T>(T[] array, T value, int startIndex = 0, int length = -1)
        {
            if (length < 0)
                length = array.Length;
            else
                length = startIndex + length;
            for (int i = startIndex; i < length; i++)
                array[i] = value;
        }
    }

    internal static unsafe class UnmanagedArrayUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* New<T>(int capacity) where T : struct
        {
            return Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)) * capacity).ToPointer();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* NewAndInit<T>(int capacity) where T : struct
        {
            int newSize = Marshal.SizeOf(typeof(T)) * capacity;
            byte* newPointer = (byte*)Marshal.AllocHGlobal(newSize).ToPointer();

            for (int i = 0; i < newSize; i++)
                *(newPointer + i) = 0;

            return newPointer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(void* pointer)
        {
            Marshal.FreeHGlobal(new IntPtr(pointer));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Resize<T>(void* oldPointer, int newCount) where T : struct
        {
            return (Marshal.ReAllocHGlobal(
                new IntPtr(oldPointer),
                new IntPtr(Marshal.SizeOf(typeof(T)) * newCount))).ToPointer();
        }
    }
}
