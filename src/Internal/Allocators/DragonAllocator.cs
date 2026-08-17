using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Core.Internal
{
    internal enum DragonAllocator : byte
    {
        Temp,
        Permanent,
    }

    internal static unsafe class DragonAllocatorExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Alloc<T>(this DragonAllocator allocator, int count) where T : unmanaged
        {
            switch (allocator)
            {
                case DragonAllocator.Temp:
                    return TempAllocator.Alloc<T>(count).Ptr;
                case DragonAllocator.Permanent:
                    return MemoryAllocator.Alloc<T>(count).Ptr;
                default:
                    return ThrowInvalidAllocator<T>(allocator);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Alloc(this DragonAllocator allocator, int byteLength)
        {
            switch (allocator)
            {
                case DragonAllocator.Temp:
                    return TempAllocator.Alloc(byteLength).Ptr;
                case DragonAllocator.Permanent:
                    return MemoryAllocator.Alloc(byteLength).Ptr;
                default:
                    return ThrowInvalidAllocator<byte>(allocator);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Realloc<T>(this DragonAllocator allocator, T* target, int oldCount, int newCount) where T : unmanaged
        {
            switch (allocator)
            {
                case DragonAllocator.Temp:
                    return TempAllocator.Realloc(target, oldCount, newCount).Ptr;
                case DragonAllocator.Permanent:
                    if (oldCount < 0) { throw new ArgumentOutOfRangeException(nameof(oldCount)); }
                    return MemoryAllocator.Realloc(target, newCount).Ptr;
                default:
                    return ThrowInvalidAllocator<T>(allocator);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Realloc(this DragonAllocator allocator, void* target, int oldByteLength, int newByteLength)
        {
            switch (allocator)
            {
                case DragonAllocator.Temp:
                    return TempAllocator.Realloc(target, oldByteLength, newByteLength).Ptr;
                case DragonAllocator.Permanent:
                    if (oldByteLength < 0) { throw new ArgumentOutOfRangeException(nameof(oldByteLength)); }
                    return MemoryAllocator.Realloc(target, newByteLength).Ptr;
                default:
                    return ThrowInvalidAllocator<byte>(allocator);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(this DragonAllocator allocator, void* target)
        {
            if (target == null) { return; }

            switch (allocator)
            {
                case DragonAllocator.Temp:
                    TempAllocator.Free(target);
                    return;
                case DragonAllocator.Permanent:
                    MemoryAllocator.Free(target);
                    return;
                default:
                    ThrowInvalidAllocator<byte>(allocator);
                    return;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T* ThrowInvalidAllocator<T>(DragonAllocator allocator) where T : unmanaged
        {
            throw new ArgumentOutOfRangeException(nameof(allocator), allocator, "Unknown allocator type.");
        }
    }

    [System.Diagnostics.DebuggerDisplay("{ToString()}")]
    internal readonly struct AllocatorHandleIdentity : IEquatable<AllocatorHandleIdentity>
    {
        public readonly int ID;
        public readonly uint Generation;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AllocatorHandleIdentity(int id, uint generation)
        {
            ID = id;
            Generation = generation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(AllocatorHandleIdentity other)
        {
            return ID == other.ID && Generation == other.Generation;
        }
        public override bool Equals(object obj)
        {
            return obj is AllocatorHandleIdentity other && Equals(other);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            unchecked { return (ID * 397) ^ (int)Generation; }
        }
        public override string ToString() { return $"{ID}:{Generation}"; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(AllocatorHandleIdentity a, AllocatorHandleIdentity b) { return a.Equals(b); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(AllocatorHandleIdentity a, AllocatorHandleIdentity b) { return !a.Equals(b); }
    }
}
