using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Core.Internal
{
    internal interface IHPtr : IDisposable
    {
        bool IsCreated { get; }
        IntPtr RawPtr { get; }
    }

    internal unsafe interface IHMem : IHPtr
    {
        int Length { get; }
        int ByteLength { get; }
        void* AlignedPtr { get; }
    }

    internal unsafe interface IHMem<T> : IHMem where T : unmanaged
    {
        T* Ptr { get; }
        Span<T> AsSpan();
        Span<T> AsSpan(int length);
    }

    internal static class AllocatorHandleExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisposeAndReset<THandle>(this ref THandle handle)
            where THandle : struct, IHPtr
        {
            handle.Dispose();
            handle = default;
        }
    }
}
