using System;
using System.Runtime.CompilerServices;
#if UNITY_2020_3_OR_NEWER
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#endif
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS.Core.Internal
{
#if !UNITY_2020_3_OR_NEWER
    internal unsafe struct TempAllocatorBlock
    {
        public byte* Allocation;
        public TempAllocatorBlock* Next;
        public int Capacity;
        public int Offset;
    }

    internal unsafe struct TempAllocatorState
    {
        public TempAllocatorBlock* First;
        public TempAllocatorBlock* Current;
        public TempAllocatorBlock* Last;
    }

    internal sealed class TempAllocatorStateFinalizer : IDisposable
    {
        private TempAllocatorState _stateSnapshot;

        public void Capture(TempAllocatorState state)
        {
            _stateSnapshot = state;
        }

        ~TempAllocatorStateFinalizer()
        {
            TempAllocator.Release(ref _stateSnapshot);
        }

        public void Dispose()
        {
            TempAllocator.Release(ref _stateSnapshot);
            GC.SuppressFinalize(this);
        }
    }
#endif

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal static unsafe class TempAllocator
    {
        private const int DEFAULT_ALIGNMENT = 16;

#if !UNITY_2020_3_OR_NEWER
        private const int INITIAL_CAPACITY_IN_BYTES = 4096;
        private const int BLOCK_ALIGNMENT = 16;

        [ThreadStatic] private static TempAllocatorState _state;
        [ThreadStatic] private static TempAllocatorStateFinalizer _stateFinalizer;
#endif

        #region AllocAndInit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HMem<T> AllocAndInit<T>(int count) where T : unmanaged
        {
            int byteLength = GetByteLength<T>(count);
            return new HMem<T>(AllocAndInit_Internal(byteLength, DragonUnsafe.AlignOf<T>()), count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HMem<T> AllocAndInit<T>(int count, int alignment) where T : unmanaged
        {
            int byteLength = GetByteLength<T>(count);
            return new HMem<T>(AllocAndInit_Internal(byteLength, ResolveAlignment<T>(alignment)), count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HMem<byte> AllocAndInit(int byteLength)
        {
            return new HMem<byte>(AllocAndInit_Internal(byteLength, DEFAULT_ALIGNMENT), byteLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HMem<byte> AllocAndInit(int byteLength, int alignment)
        {
            return new HMem<byte>(AllocAndInit_Internal(byteLength, alignment), byteLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static HPtr AllocAndInit_Internal(int byteLength, int alignment)
        {
            HPtr handle = Alloc_Internal(byteLength, alignment);
            DragonUnsafe.ClearMemory(handle.RawPtr, 0, byteLength);
            return handle;
        }
        #endregion

        #region Alloc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HMem<T> Alloc<T>() where T : unmanaged
        {
            return Alloc<T>(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HMem<T> Alloc<T>(int count) where T : unmanaged
        {
            int byteLength = GetByteLength<T>(count);
            return new HMem<T>(Alloc_Internal(byteLength, DragonUnsafe.AlignOf<T>()), count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HMem<T> Alloc<T>(int count, int alignment) where T : unmanaged
        {
            int byteLength = GetByteLength<T>(count);
            return new HMem<T>(Alloc_Internal(byteLength, ResolveAlignment<T>(alignment)), count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HMem<byte> Alloc(int byteLength)
        {
            return new HMem<byte>(Alloc_Internal(byteLength, DEFAULT_ALIGNMENT), byteLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HMem<byte> Alloc(int byteLength, int alignment)
        {
            return new HMem<byte>(Alloc_Internal(byteLength, alignment), byteLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static HPtr Alloc_Internal(int byteLength, int alignment)
        {
            return HPtr.FromDataPtr(AllocRaw(byteLength, alignment));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void* AllocRaw(int byteLength, int alignment)
        {
            ValidateAllocation(byteLength, alignment);
            if (byteLength == 0) { byteLength = 1; }

#if UNITY_2020_3_OR_NEWER
            return UnsafeUtility.Malloc(byteLength, alignment, Allocator.Temp);
#else
            TempAllocatorBlock* block = _state.Current;
            while (block != null)
            {
                void* result;
                if (TryAlloc(block, byteLength, alignment, out result))
                {
                    _state.Current = block;
                    return result;
                }
                block = block->Next;
            }

            TempAllocatorStateFinalizer stateFinalizer = GetOrCreateStateFinalizer();
            block = AppendBlock(ref _state, GetRequiredCapacity(byteLength, alignment));
            _state.Current = block;
            stateFinalizer.Capture(_state);
            void* allocation;
            if (TryAlloc(block, byteLength, alignment, out allocation) == false)
            {
                throw new InvalidOperationException("The temporary allocator created a block that is too small for the requested allocation.");
            }
            return allocation;
#endif
        }
        #endregion

        #region ReallocAndInit
        public static HMem<T> ReallocAndInit<T>(T* target, int oldCount, int newCount) where T : unmanaged
        {
            return ReallocAndInit<T>(HPtr.FromDataPtr(target), oldCount, newCount);
        }

        public static HMem<byte> ReallocAndInit(void* target, int oldByteLength, int newByteLength)
        {
            return ReallocAndInit(HPtr.FromDataPtr(target), oldByteLength, newByteLength);
        }

        public static HMem<T> ReallocAndInit<T>(HMem<T> target, int newCount) where T : unmanaged
        {
            int oldByteLength = GetByteLength<T>(target.Length);
            int newByteLength = GetByteLength<T>(newCount);
            return new HMem<T>(ReallocAndInit_Internal(target, oldByteLength, newByteLength, DragonUnsafe.AlignOf<T>()), newCount);
        }

        public static HMem<T> ReallocAndInit<T>(HPtr target, int oldCount, int newCount) where T : unmanaged
        {
            int oldByteLength = GetByteLength<T>(oldCount);
            int newByteLength = GetByteLength<T>(newCount);
            return new HMem<T>(ReallocAndInit_Internal(target, oldByteLength, newByteLength, DragonUnsafe.AlignOf<T>()), newCount);
        }

        public static HMem<byte> ReallocAndInit(HPtr target, int oldByteLength, int newByteLength)
        {
            return new HMem<byte>(ReallocAndInit_Internal(target, oldByteLength, newByteLength, DEFAULT_ALIGNMENT), newByteLength);
        }

        private static HPtr ReallocAndInit_Internal(HPtr target, int oldByteLength, int newByteLength, int alignment)
        {
            ValidateReallocation(oldByteLength, newByteLength);
            int initializedByteLength = target.IsCreated ? Math.Min(oldByteLength, newByteLength) : 0;
            HPtr handle = Realloc_Internal(target, oldByteLength, newByteLength, alignment);
            if (newByteLength > initializedByteLength)
            {
                DragonUnsafe.ClearMemory(
                    handle.RawPtr,
                    initializedByteLength,
                    newByteLength - initializedByteLength);
            }
            return handle;
        }
        #endregion

        #region Realloc
        public static HMem<T> Realloc<T>(T* target, int oldCount, int newCount) where T : unmanaged
        {
            return Realloc<T>(HPtr.FromDataPtr(target), oldCount, newCount);
        }

        public static HMem<byte> Realloc(void* target, int oldByteLength, int newByteLength)
        {
            return Realloc(HPtr.FromDataPtr(target), oldByteLength, newByteLength);
        }

        public static HMem<T> Realloc<T>(HMem<T> target, int newCount) where T : unmanaged
        {
            int oldByteLength = GetByteLength<T>(target.Length);
            int newByteLength = GetByteLength<T>(newCount);
            return new HMem<T>(Realloc_Internal(target, oldByteLength, newByteLength, DragonUnsafe.AlignOf<T>()), newCount);
        }

        public static HMem<T> Realloc<T>(HPtr target, int oldCount, int newCount) where T : unmanaged
        {
            int oldByteLength = GetByteLength<T>(oldCount);
            int newByteLength = GetByteLength<T>(newCount);
            return new HMem<T>(Realloc_Internal(target, oldByteLength, newByteLength, DragonUnsafe.AlignOf<T>()), newCount);
        }

        public static HMem<byte> Realloc(HPtr target, int oldByteLength, int newByteLength)
        {
            return new HMem<byte>(Realloc_Internal(target, oldByteLength, newByteLength, DEFAULT_ALIGNMENT), newByteLength);
        }

        private static HPtr Realloc_Internal(HPtr target, int oldByteLength, int newByteLength, int alignment)
        {
            ValidateReallocation(oldByteLength, newByteLength);
            if (target.IsCreated == false)
            {
                return Alloc_Internal(newByteLength, alignment);
            }
            if (oldByteLength == newByteLength)
            {
                return target;
            }

            HPtr result = Alloc_Internal(newByteLength, alignment);
            int bytesToCopy = Math.Min(oldByteLength, newByteLength);
            if (bytesToCopy > 0)
            {
                Buffer.MemoryCopy(
                    source: target.As<byte>(),
                    destination: result.As<byte>(),
                    destinationSizeInBytes: newByteLength,
                    sourceBytesToCopy: bytesToCopy);
            }
            Free(target);
            return result;
        }
        #endregion

        #region Clone
        public static HMem<T> From<T>(HMem<T> source) where T : unmanaged
        {
            HMem<T> result = Alloc<T>(source.Length);
            source.AsSpan().CopyTo(result.AsSpan());
            return result;
        }

        public static HMem<T> From<T>(T* ptr, int length) where T : unmanaged
        {
            return From(new ReadOnlySpan<T>(ptr, length));
        }

        public static HMem<T> From<T>(T[] source) where T : unmanaged
        {
            return From(new ReadOnlySpan<T>(source));
        }

        public static HMem<T> From<T>(ReadOnlySpan<T> source) where T : unmanaged
        {
            HMem<T> result = Alloc<T>(source.Length);
            source.CopyTo(result.AsSpan());
            return result;
        }
        #endregion

        #region Free
        /// <summary>
        /// Releases the allocation on Unity. The .NET bump backend reclaims memory only
        /// through <see cref="Reset"/> or <see cref="Release"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(HPtr target)
        {
            FreeRaw(target.Data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FreeAndClear<T>(ref HMem<T> target) where T : unmanaged
        {
            Free(target.Handle);
            target = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FreeAndClear(ref HPtr target)
        {
            Free(target);
            target = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(void* allocation)
        {
            FreeRaw(allocation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FreeRaw(void* allocation)
        {
#if UNITY_2020_3_OR_NEWER
            if (allocation != null)
            {
                UnsafeUtility.Free(allocation, Allocator.Temp);
            }
#endif
        }
        #endregion

        #region Lifetime
        /// <summary>
        /// Rewinds the allocator owned by the current thread. All handles returned by
        /// the .NET backend become invalid. Unity resets Allocator.Temp at an engine-controlled
        /// boundary, so this method is a no-op there.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reset()
        {
#if !UNITY_2020_3_OR_NEWER
            if (_state.First == null) { return; }

            TempAllocatorBlock* block = _state.First;
            while (block != null)
            {
                block->Offset = 0;
                block = block->Next;
            }
            _state.Current = _state.First;
#endif
        }

        /// <summary>
        /// Frees all memory owned by the current thread in the .NET backend.
        /// Unity owns the lifetime of Allocator.Temp, so this method is a no-op there.
        /// </summary>
        public static void Release()
        {
#if !UNITY_2020_3_OR_NEWER
            TempAllocatorStateFinalizer stateFinalizer = _stateFinalizer;
            _state = default;
            _stateFinalizer = null;

            if (stateFinalizer != null)
            {
                stateFinalizer.Dispose();
            }
#endif
        }
        #endregion

        #region Validation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ResolveAlignment<T>(int requestedAlignment) where T : unmanaged
        {
            ValidateAlignment(requestedAlignment);
            int typeAlignment = DragonUnsafe.AlignOf<T>();
            return requestedAlignment < typeAlignment ? typeAlignment : requestedAlignment;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetByteLength<T>(int count) where T : unmanaged
        {
            if (count < 0) { throw new ArgumentOutOfRangeException(nameof(count)); }
            long byteLength = (long)sizeof(T) * count;
            if (byteLength > int.MaxValue)
            {
                throw new OutOfMemoryException("The requested temporary allocation is too large.");
            }
            return (int)byteLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateAllocation(int byteLength, int alignment)
        {
            if (byteLength < 0) { throw new ArgumentOutOfRangeException(nameof(byteLength)); }
            ValidateAlignment(alignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateAlignment(int alignment)
        {
            if (alignment <= 0 || (alignment & (alignment - 1)) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(alignment), "Alignment must be a positive power of two.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateReallocation(int oldByteLength, int newByteLength)
        {
            if (oldByteLength < 0) { throw new ArgumentOutOfRangeException(nameof(oldByteLength)); }
            if (newByteLength < 0) { throw new ArgumentOutOfRangeException(nameof(newByteLength)); }
        }
        #endregion

#if !UNITY_2020_3_OR_NEWER
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TempAllocatorStateFinalizer GetOrCreateStateFinalizer()
        {
            TempAllocatorStateFinalizer stateFinalizer = _stateFinalizer;
            if (stateFinalizer != null) { return stateFinalizer; }
            return CreateStateFinalizer();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static TempAllocatorStateFinalizer CreateStateFinalizer()
        {
            TempAllocatorStateFinalizer stateFinalizer = new TempAllocatorStateFinalizer();
            _stateFinalizer = stateFinalizer;
            return stateFinalizer;
        }

        internal static void Release(ref TempAllocatorState state)
        {
            TempAllocatorBlock* block = state.First;
            state = default;
            while (block != null)
            {
                TempAllocatorBlock* next = block->Next;
                MemoryAllocator.Free(block->Allocation);
                block = next;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryAlloc(TempAllocatorBlock* block, int byteLength, int alignment, out void* allocation)
        {
            byte* data = (byte*)(block + 1);
            ulong unalignedAddress = (ulong)(data + block->Offset);
            ulong alignedAddress = (unalignedAddress + (uint)alignment - 1) & ~((ulong)(uint)alignment - 1);
            long endOffset = (long)(alignedAddress - (ulong)data) + byteLength;
            if (endOffset > block->Capacity)
            {
                allocation = null;
                return false;
            }

            block->Offset = (int)endOffset;
            allocation = (void*)alignedAddress;
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static TempAllocatorBlock* AppendBlock(ref TempAllocatorState state, int requiredCapacity)
        {
            int maximumCapacity = int.MaxValue - sizeof(TempAllocatorBlock) - (BLOCK_ALIGNMENT - 1);
            if (requiredCapacity > maximumCapacity)
            {
                throw new OutOfMemoryException("The requested temporary allocation is too large.");
            }

            TempAllocatorBlock* last = state.Last;
            int capacity = last == null ? INITIAL_CAPACITY_IN_BYTES : last->Capacity;
            if (last != null)
            {
                if (capacity > maximumCapacity >> 1)
                {
                    if (requiredCapacity > capacity)
                    {
                        capacity = requiredCapacity;
                    }
                }
                else
                {
                    capacity <<= 1;
                }
            }
            while (capacity < requiredCapacity)
            {
                if (capacity > maximumCapacity >> 1)
                {
                    capacity = requiredCapacity;
                    break;
                }
                capacity <<= 1;
            }

            int totalByteLength = sizeof(TempAllocatorBlock) + capacity + BLOCK_ALIGNMENT - 1;
            byte* allocation = MemoryAllocator.Alloc(totalByteLength).Ptr;
            ulong blockAddress = ((ulong)allocation + BLOCK_ALIGNMENT - 1) & ~(BLOCK_ALIGNMENT - 1UL);
            TempAllocatorBlock* block = (TempAllocatorBlock*)blockAddress;
            *block = default;
            block->Allocation = allocation;
            block->Capacity = capacity;

            if (last == null)
            {
                state.First = block;
            }
            else
            {
                last->Next = block;
            }
            state.Last = block;
            return block;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetRequiredCapacity(int byteLength, int alignment)
        {
            long requiredCapacity = (long)byteLength + alignment - 1;
            if (requiredCapacity > int.MaxValue)
            {
                throw new OutOfMemoryException("The requested temporary allocation is too large.");
            }
            return (int)requiredCapacity;
        }
#endif

        #region Handles
        [System.Diagnostics.DebuggerDisplay("Length = {Length}")]
        public readonly struct HMem<T> : IHMem<T>, IEquatable<HMem<T>> where T : unmanaged
        {
            public readonly T* Ptr;
            public readonly int Length;

            #region Properties
            public bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Ptr != null; }
            }
            public IntPtr RawPtr
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return new IntPtr(Ptr); }
            }
            public HPtr Handle
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return HPtr.FromDataPtr(Ptr); }
            }
            T* IHMem<T>.Ptr { get { return Ptr; } }
            int IHMem.Length { get { return Length; } }
            int IHMem.ByteLength { get { return checked(Length * sizeof(T)); } }
            void* IHMem.AlignedPtr { get { return Ptr; } }
            #endregion

            #region Constructors
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal HMem(HPtr handle, int length)
            {
                Ptr = handle.As<T>();
                Length = length;
            }
            #endregion

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public HMem<U> As<U>() where U : unmanaged
            {
                if (IsCreated == false) { return default; }

                long totalBytes = (long)Length * sizeof(T);
                long newLengthLong = totalBytes / sizeof(U);
#if DEBUG
                if (totalBytes % sizeof(U) != 0)
                {
                    throw new InvalidOperationException($"Cannot cast Memory<{typeof(T).Name}> to Memory<{typeof(U).Name}> because the size of the underlying memory ({totalBytes} bytes) is not a multiple of the size of {typeof(U).Name} ({sizeof(U)} bytes).");
                }
                if (newLengthLong > int.MaxValue)
                {
                    throw new InvalidOperationException($"Resulting length ({newLengthLong}) exceeds int.MaxValue.");
                }
#endif
                return new HMem<U>(Handle, (int)newLengthLong);
            }
            public void Dispose() { Handle.Dispose(); }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<T> AsSpan() { return new Span<T>(Ptr, Length); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<T> AsSpan(int length)
            {
#if DEBUG
                if (length > Length) { Throw.UndefinedException(); }
#endif
                return new Span<T>(Ptr, length);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public UnsafeSegment<T> AsSegment() { return new UnsafeSegment<T>(Ptr, Length); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public UnsafeSegment<T> AsSegment(int length)
            {
#if DEBUG
                if (length > Length) { Throw.UndefinedException(); }
#endif
                return new UnsafeSegment<T>(Ptr, length);
            }

            #region Other
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() { return Handle.GetHashCode(); }
            public override bool Equals(object obj) { return obj is HMem<T> h && Equals(h); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(HMem<T> other) { return Handle == other.Handle; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(HMem<T> a, HMem<T> b) { return a.Equals(b); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(HMem<T> a, HMem<T> b) { return !a.Equals(b); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator T*(HMem<T> a) { return a.Ptr; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator byte*(HMem<T> a) { return (byte*)a.RawPtr.ToPointer(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator IntPtr(HMem<T> a) { return a.RawPtr; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator HPtr(HMem<T> a) { return a.Handle; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator Span<T>(HMem<T> a) { return a.AsSpan(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator ReadOnlySpan<T>(HMem<T> a) { return a.AsSpan(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator UnsafeSegment<T>(HMem<T> a) { return a.AsSegment(); }
            #endregion
        }

        [System.Diagnostics.DebuggerDisplay("{RawPtr}")]
        public readonly struct HPtr : IHPtr, IEquatable<HPtr>
        {
            public static readonly HPtr Empty = new HPtr(null);
            internal readonly void* Data;

            #region Properties
            public bool IsCreated
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Data != null; }
            }
            public IntPtr RawPtr
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return (IntPtr)Data; }
            }
            #endregion

            #region Constructors
            private HPtr(void* data) { Data = data; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static HPtr FromDataPtr(void* ptr) { return new HPtr(ptr); }
            #endregion

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public U* As<U>() where U : unmanaged { return (U*)RawPtr; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() { Free(this); }

            #region Other
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() { return RawPtr.GetHashCode(); }
            public override bool Equals(object obj) { return obj is HPtr h && Equals(h); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(HPtr other) { return other.Data == Data; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(HPtr a, HPtr b) { return a.Equals(b); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(HPtr a, HPtr b) { return !a.Equals(b); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator void*(HPtr a) { return a.RawPtr.ToPointer(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator byte*(HPtr a) { return (byte*)a.RawPtr.ToPointer(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static implicit operator IntPtr(HPtr a) { return a.RawPtr; }
            #endregion
        }
        #endregion
    }

}
