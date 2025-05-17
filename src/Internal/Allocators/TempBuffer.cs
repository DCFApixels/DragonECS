#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS.Core.Internal
{
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    public static unsafe class TempBuffer<TContext, T> where T : unmanaged
    {
        [ThreadStatic] private static T* _ptr;
        [ThreadStatic] private static int _size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Get(int size)
        {
            if (_size < size)
            {
                _ptr = TempBufferMemory<TContext>.Get<T>(size);
                _size = size;
            }
            return _ptr;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear()
        {
            TempBufferMemory<TContext>.Clear();
        }
    }
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal static unsafe class TempBufferMemory<TContext>
    {
        [ThreadStatic] private static byte* _ptr;
        [ThreadStatic] private static int _byteSize;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Get<T>(int size) where T : unmanaged
        {
            int byteSize = size * Marshal.SizeOf<T>();
            if (_byteSize < byteSize)
            {
                if (_ptr != null)
                {
                    MemoryAllocator.Free(_ptr);
                }
                _ptr = MemoryAllocator.Alloc<byte>(byteSize).As<byte>();
                _byteSize = byteSize;
            }
            return (T*)_ptr;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear()
        {
            AllocatorUtility.ClearAllocatedMemory(_ptr, 0, _byteSize);
        }
    }
}
