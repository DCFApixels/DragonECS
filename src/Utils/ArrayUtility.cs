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
        public static T* New<T>(int capacity) where T : unmanaged
        {
            return (T*)Marshal.AllocHGlobal(Marshal.SizeOf<T>() * capacity).ToPointer();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* NewAndInit<T>(int capacity) where T : unmanaged
        {
            int newSize = Marshal.SizeOf(typeof(T)) * capacity;
            byte* newPointer = (byte*)Marshal.AllocHGlobal(newSize).ToPointer();

            for (int i = 0; i < newSize; i++)
                *(newPointer + i) = 0;

            return (T*)newPointer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(void* pointer)
        {
            Marshal.FreeHGlobal(new IntPtr(pointer));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Resize<T>(void* oldPointer, int newCount) where T : unmanaged
        {
            return (T*)(Marshal.ReAllocHGlobal(
                new IntPtr(oldPointer),
                new IntPtr(Marshal.SizeOf(typeof(T)) * newCount))).ToPointer();
        }
    }
}
