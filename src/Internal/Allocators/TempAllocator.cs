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
        public TempAllocatorBlock* Previous;
        public TempAllocatorBlock* Next;
        public TempAllocatorAllocationHeader* LastAllocation;
        public int Capacity;
        public int Offset;
        public int ActiveMarkersCount;
    }

    internal unsafe struct TempAllocatorAllocationHeader
    {
        public TempAllocatorBlock* Block;
        public int PreviousHeaderOffset;
        public int PreviousOffsetAndState;
#if DEBUG
        public uint State;
#endif
    }

    internal unsafe struct TempAllocatorState
    {
        public TempAllocatorBlock* First;
        public TempAllocatorBlock* Current;
        public TempAllocatorBlock* Last;
        public uint ActiveMarkerVersion;
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
#if DEBUG
        private const uint ALLOCATION_STATE_ACTIVE = 0xD6A6EC51;
        private const uint ALLOCATION_STATE_FREED = 0xD6A6EC5F;
#endif
        private const int NO_PREVIOUS_HEADER = -1;
        private const int FREED_OFFSET_MASK = int.MinValue;
        private const int OFFSET_VALUE_MASK = int.MaxValue;

        [ThreadStatic] private static TempAllocatorState _state;
        [ThreadStatic] private static TempAllocatorStateFinalizer _stateFinalizer;
        [ThreadStatic] private static uint _markerVersion;
#endif

        public readonly struct Marker
        {
#if !UNITY_2020_3_OR_NEWER
            internal readonly TempAllocatorBlock* Block;
            internal readonly TempAllocatorAllocationHeader* LastAllocation;
            internal readonly int Offset;
            internal readonly uint Version;
            internal readonly uint PreviousVersion;
            internal readonly int ThreadID;

            internal Marker(
                TempAllocatorBlock* block,
                TempAllocatorAllocationHeader* lastAllocation,
                int offset,
                uint version,
                uint previousVersion)
            {
                Block = block;
                LastAllocation = lastAllocation;
                Offset = offset;
                Version = version;
                PreviousVersion = previousVersion;
                ThreadID = Environment.CurrentManagedThreadId;
            }
#endif
        }

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
            ValidateByteLength(byteLength);
            return new HMem<byte>(AllocAndInit_Internal(byteLength, DEFAULT_ALIGNMENT), byteLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HMem<byte> AllocAndInit(int byteLength, int alignment)
        {
            ValidateAllocation(byteLength, alignment);
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
            ValidateByteLength(byteLength);
            return new HMem<byte>(Alloc_Internal(byteLength, DEFAULT_ALIGNMENT), byteLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HMem<byte> Alloc(int byteLength, int alignment)
        {
            ValidateAllocation(byteLength, alignment);
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
            if (byteLength == 0) { byteLength = 1; }

#if UNITY_2020_3_OR_NEWER
            return UnsafeUtility.Malloc(byteLength, alignment, Allocator.Temp);
#else
            TempAllocatorBlock* block = _state.Current;
            void* allocation;
            if (block != null && TryAlloc(block, byteLength, alignment, out allocation))
            {
                return allocation;
            }
            return AllocRawSlow(byteLength, alignment, block);
#endif
        }

#if !UNITY_2020_3_OR_NEWER
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void* AllocRawSlow(int byteLength, int alignment, TempAllocatorBlock* currentBlock)
        {
            TempAllocatorStateFinalizer stateFinalizer = GetOrCreateStateFinalizer();
            TempAllocatorBlock* block = AppendBlock(ref _state, GetRequiredCapacity(byteLength, alignment));
            stateFinalizer.Capture(_state);
            void* result;
            if (TryAlloc(block, byteLength, alignment, out result) == false)
            {
                throw new InvalidOperationException("The temporary allocator created a block that is too small for the requested allocation.");
            }

            // An empty main block is retained for reuse. Once a larger block replaces it,
            // it becomes retired and can be returned to the native allocator immediately.
            TryReleaseRetiredBlock(currentBlock);
            return result;
        }
#endif
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
        /// Releases the allocation. The .NET bump backend immediately rewinds allocations
        /// freed in reverse order and defers out-of-order allocations until the newer ones
        /// are released.
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
#else
            if (allocation == null) { return; }

            TempAllocatorAllocationHeader* header = GetAllocationHeader(allocation);
#if DEBUG
            if (header->State != ALLOCATION_STATE_ACTIVE)
            {
                throw new InvalidOperationException("The temporary allocation has already been freed or does not belong to this allocator.");
            }
#endif

            TempAllocatorBlock* block = header->Block;
            if (block->LastAllocation != header)
            {
                MarkAllocationFreed(header);
                return;
            }

#if DEBUG
            header->State = ALLOCATION_STATE_FREED;
#endif

            do
            {
                TempAllocatorAllocationHeader* last = block->LastAllocation;
                block->Offset = GetPreviousOffset(last);
                block->LastAllocation = GetPreviousAllocation(block, last);
            }
            while (block->LastAllocation != null && IsAllocationFreed(block->LastAllocation));

            if (block->LastAllocation == null && block != _state.Current)
            {
                TryReleaseRetiredBlock(block);
            }
#endif
        }
        #endregion

        #region Lifetime
        /// <summary>
        /// Captures the current allocation frontier. Markers may be nested and must be
        /// rewound in reverse order. Allocations that predate a marker must not be freed
        /// before that marker is rewound.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Marker Mark()
        {
#if UNITY_2020_3_OR_NEWER
            return default;
#else
            uint version = unchecked(++_markerVersion);
            if (version == 0)
            {
                version = unchecked(++_markerVersion);
            }

            TempAllocatorBlock* block = _state.Current;
            if (block != null)
            {
                block->ActiveMarkersCount++;
            }
            Marker marker = new Marker(
                block,
                block == null ? null : block->LastAllocation,
                block == null ? 0 : block->Offset,
                version,
                _state.ActiveMarkerVersion);
            _state.ActiveMarkerVersion = version;
            return marker;
#endif
        }

        /// <summary>
        /// Restores the allocation frontier captured by <see cref="Mark"/>. On Unity,
        /// Allocator.Temp memory is reclaimed at the engine-controlled boundary and this
        /// method is a no-op, matching <see cref="Reset"/>.
        /// </summary>
        public static void Rewind(Marker marker)
        {
#if !UNITY_2020_3_OR_NEWER
            if (marker.ThreadID != Environment.CurrentManagedThreadId)
            {
                throw new InvalidOperationException("A temporary allocator marker must be rewound on the thread that created it.");
            }
            if (_state.ActiveMarkerVersion != marker.Version)
            {
                throw new InvalidOperationException("Temporary allocator markers must be rewound once and in reverse order.");
            }

            TempAllocatorBlock* markerBlock = marker.Block;
            if (markerBlock == null)
            {
                ClearAndKeepCurrentBlock(ref _state);
            }
            else
            {
                RewindToMarker(ref _state, markerBlock, marker.LastAllocation, marker.Offset);
            }
            _state.ActiveMarkerVersion = marker.PreviousVersion;
            CaptureStateForFinalizer();
#endif
        }

        /// <summary>
        /// Rewinds the allocator owned by the current thread. All handles returned by
        /// the .NET backend become invalid. Unity resets Allocator.Temp at an engine-controlled
        /// boundary, so this method is a no-op there.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reset()
        {
#if !UNITY_2020_3_OR_NEWER
            _state.ActiveMarkerVersion = 0;
            if (_state.First == null) { return; }

            ClearAndKeepCurrentBlock(ref _state);
            CaptureStateForFinalizer();
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
            int elementSize = sizeof(T);
            if (count > int.MaxValue / elementSize)
            {
                throw new OutOfMemoryException("The requested temporary allocation is too large.");
            }
            return count * elementSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateByteLength(int byteLength)
        {
            if (byteLength < 0) { throw new ArgumentOutOfRangeException(nameof(byteLength)); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateAllocation(int byteLength, int alignment)
        {
            ValidateByteLength(byteLength);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CaptureStateForFinalizer()
        {
            TempAllocatorStateFinalizer stateFinalizer = _stateFinalizer;
            if (stateFinalizer != null)
            {
                stateFinalizer.Capture(_state);
            }
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

        /// <summary>
        /// Returns the number of native blocks retained by the current thread.
        /// Intended for allocator diagnostics and regression tests.
        /// </summary>
        internal static int GetRetainedBlockCount()
        {
            int result = 0;
            TempAllocatorBlock* block = _state.First;
            while (block != null)
            {
                result++;
                block = block->Next;
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TryReleaseRetiredBlock(TempAllocatorBlock* block)
        {
            if (block == null ||
                block == _state.Current ||
                block->LastAllocation != null ||
                block->ActiveMarkersCount != 0)
            {
                return;
            }

            TempAllocatorBlock* previous = block->Previous;
            TempAllocatorBlock* next = block->Next;
            if (previous == null)
            {
                _state.First = next;
            }
            else
            {
                previous->Next = next;
            }
            if (next == null)
            {
                _state.Last = previous;
            }
            else
            {
                next->Previous = previous;
            }

            byte* allocation = block->Allocation;
            CaptureStateForFinalizer();
            MemoryAllocator.Free(allocation);
        }

        private static void RewindToMarker(
            ref TempAllocatorState state,
            TempAllocatorBlock* markerBlock,
            TempAllocatorAllocationHeader* markerLastAllocation,
            int markerOffset)
        {
            TempAllocatorBlock* current = state.Current;
            if (current != markerBlock)
            {
                TempAllocatorBlock* releasedBlock = markerBlock->Next;
                while (releasedBlock != current)
                {
                    TempAllocatorBlock* next = releasedBlock->Next;
                    MemoryAllocator.Free(releasedBlock->Allocation);
                    releasedBlock = next;
                }

                markerBlock->Next = current;
                current->Previous = markerBlock;
                current->LastAllocation = null;
                current->Offset = 0;
            }

            markerBlock->LastAllocation = markerLastAllocation;
            markerBlock->Offset = markerOffset;
            markerBlock->ActiveMarkersCount--;
            state.Last = current;

            if (markerBlock != current && markerBlock->LastAllocation == null)
            {
                TryReleaseRetiredBlock(markerBlock);
            }
        }

        private static void ClearAndKeepCurrentBlock(ref TempAllocatorState state)
        {
            TempAllocatorBlock* current = state.Current;
            TempAllocatorBlock* block = state.First;
            while (block != null)
            {
                TempAllocatorBlock* next = block->Next;
                if (block != current)
                {
                    MemoryAllocator.Free(block->Allocation);
                }
                block = next;
            }

            if (current == null)
            {
                state.First = null;
                state.Last = null;
                return;
            }

            current->Previous = null;
            current->Next = null;
            current->LastAllocation = null;
            current->Offset = 0;
            current->ActiveMarkersCount = 0;
            state.First = current;
            state.Last = current;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryAlloc(TempAllocatorBlock* block, int byteLength, int alignment, out void* allocation)
        {
            byte* data = (byte*)(block + 1);
            int previousOffset = block->Offset;
            int effectiveAlignment = GetEffectiveAlignment(alignment);
            ulong unalignedAddress = (ulong)(data + previousOffset + sizeof(TempAllocatorAllocationHeader));
            ulong alignedAddress = (unalignedAddress + (uint)effectiveAlignment - 1) & ~((ulong)(uint)effectiveAlignment - 1);
            long endOffset = (long)(alignedAddress - (ulong)data) + byteLength;
            if (endOffset > block->Capacity)
            {
                allocation = null;
                return false;
            }

            TempAllocatorAllocationHeader* header = (TempAllocatorAllocationHeader*)(alignedAddress - (uint)sizeof(TempAllocatorAllocationHeader));
            header->Block = block;
            header->PreviousHeaderOffset = block->LastAllocation == null
                ? NO_PREVIOUS_HEADER
                : (int)((byte*)block->LastAllocation - data);
            header->PreviousOffsetAndState = previousOffset;
#if DEBUG
            header->State = ALLOCATION_STATE_ACTIVE;
#endif

            block->LastAllocation = header;
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

            TempAllocatorBlock* last = state.Current;
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
                block->Previous = last;
            }
            state.Current = block;
            state.Last = block;
            return block;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetRequiredCapacity(int byteLength, int alignment)
        {
            int effectiveAlignment = GetEffectiveAlignment(alignment);
            long requiredCapacity = (long)sizeof(TempAllocatorAllocationHeader) + byteLength + effectiveAlignment - 1;
            if (requiredCapacity > int.MaxValue)
            {
                throw new OutOfMemoryException("The requested temporary allocation is too large.");
            }
            return (int)requiredCapacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetEffectiveAlignment(int alignment)
        {
            return alignment < IntPtr.Size ? IntPtr.Size : alignment;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TempAllocatorAllocationHeader* GetAllocationHeader(void* allocation)
        {
            return (TempAllocatorAllocationHeader*)((byte*)allocation - sizeof(TempAllocatorAllocationHeader));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TempAllocatorAllocationHeader* GetPreviousAllocation(
            TempAllocatorBlock* block,
            TempAllocatorAllocationHeader* allocation)
        {
            int offset = allocation->PreviousHeaderOffset;
            return offset == NO_PREVIOUS_HEADER
                ? null
                : (TempAllocatorAllocationHeader*)((byte*)(block + 1) + offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPreviousOffset(TempAllocatorAllocationHeader* allocation)
        {
            return allocation->PreviousOffsetAndState & OFFSET_VALUE_MASK;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsAllocationFreed(TempAllocatorAllocationHeader* allocation)
        {
#if DEBUG
            return allocation->State == ALLOCATION_STATE_FREED;
#else
            return allocation->PreviousOffsetAndState < 0;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MarkAllocationFreed(TempAllocatorAllocationHeader* allocation)
        {
#if DEBUG
            allocation->State = ALLOCATION_STATE_FREED;
#else
            allocation->PreviousOffsetAndState |= FREED_OFFSET_MASK;
#endif
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() { TempAllocator.Free(Ptr); }

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
            public void Dispose() { TempAllocator.Free(Data); }

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
