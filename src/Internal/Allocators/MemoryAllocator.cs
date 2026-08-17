#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS.Core.Internal
{
    internal unsafe static class MemoryAllocator
    {
        private const int UNKNOWN_ALIGNMENT = 0;

#if DEBUG
        private static ulong _inrement = 0;
        private static IdDispenser _idDispenser;
        private static HandleDebugInfo[] _debugInfos;
        private static int _releaseIDsCounter = 0;
#endif

        static MemoryAllocator()
        {
            StaticInit();
        }
        private static void StaticInit()
        {
#if DEBUG
            _idDispenser = new IdDispenser();
            _debugInfos = new HandleDebugInfo[32];
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetAlignmentPadding(int count, int alignment)
        {
            if (count < 0) { throw new ArgumentOutOfRangeException(nameof(count)); }
            return count > 0 ? alignment - 1 : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetByteLength<T>(int count) where T : unmanaged
        {
            long byteLength = (long)sizeof(T) * count;
#if DEBUG
            if (count < 0) { throw new ArgumentOutOfRangeException(nameof(count)); }
#endif
            if (byteLength > int.MaxValue) { ThrowAllocationTooLarge(); }
            return (int)byteLength;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetAllocationByteLength(int byteLength, int alignmentPadding)
        {
            if (byteLength < 0) { throw new ArgumentOutOfRangeException(nameof(byteLength)); }
            long allocationByteLength = (long)Meta.Size + byteLength + alignmentPadding;
            if (allocationByteLength > int.MaxValue) { ThrowAllocationTooLarge(); }
            return (int)allocationByteLength;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowAllocationTooLarge()
        {
            throw new OutOfMemoryException("The requested allocation is too large.");
        }

        private static HandleDebugInfo[] CurrentHandlesList
        {
            get { return CreateCurrentHandlesList_Debug(); }
        }
        internal static HandleDebugInfo[] CreateCurrentHandlesList_Debug()
        {
#if DEBUG
            var result = new HandleDebugInfo[_idDispenser.Count];
            int i = 0;
            foreach (var id in _idDispenser)
            {
                result[i++] = _debugInfos[id];
            }
            SortHalper.SortBy<HandleDebugInfo, ulong>(result, o => o.Increment);
            return result;
#else
            return Array.Empty<HandleDebugInfo>();
#endif
        }

        #region AllocAndInit
        public static HMem<T> AllocAndInit<T>(int count) where T : unmanaged
        {
            int alignment = DragonUnsafe.AlignOf<T>();
            int alignmentPadding = GetAlignmentPadding(count, alignment);
            return new HMem<T>(AllocAndInit_Internal(GetByteLength<T>(count), typeof(T), sizeof(T), alignment, alignmentPadding), count);
        }
        public static HMem<byte> AllocAndInit(int byteLength)
        {
            return new HMem<byte>(AllocAndInit_Internal(byteLength, null, sizeof(byte)), byteLength);
        }
        private static HPtr AllocAndInit_Internal(int byteLength, Type type, int elementSize, int alignment = UNKNOWN_ALIGNMENT, int alignmentPadding = 0)
        {
            HPtr handle = Alloc_Internal(byteLength, type, elementSize, alignment, alignmentPadding);
            DragonUnsafe.ClearMemory(handle.RawPtr, 0, byteLength + alignmentPadding);
            return handle;
        }
        #endregion

        #region Alloc
        public static HMem<T> Alloc<T>(int count) where T : unmanaged
        {
            int alignment = DragonUnsafe.AlignOf<T>();
            int alignmentPadding = GetAlignmentPadding(count, alignment);
            return new HMem<T>(Alloc_Internal(GetByteLength<T>(count), typeof(T), sizeof(T), alignment, alignmentPadding), count);
        }
        public static HMem<byte> Alloc(int byteLength)
        {
            return new HMem<byte>(Alloc_Internal(byteLength, null, sizeof(byte)), byteLength); ;
        }
        private static HPtr Alloc_Internal(int byteLength, Type type, int elementSize, int alignment = UNKNOWN_ALIGNMENT, int alignmentPadding = 0)
        {
            byteLength = byteLength == 0 ? 1 : byteLength;
            Meta* newHandledPtr = (Meta*)Marshal.AllocHGlobal(GetAllocationByteLength(byteLength, alignmentPadding));
#if DEBUG
            lock (_idDispenser)
            {
                int id = _idDispenser.UseFree();
                if (_debugInfos.Length <= id)
                {
                    Array.Resize(ref _debugInfos, ArrayUtility.NextPow2(id + 1));
                }

                ref HandleDebugInfo info = ref _debugInfos[id];
                uint generation = unchecked(info.Identity.Generation + 1u);
                if (generation == 0)
                {
                    generation = 1;
                }
                AllocatorHandleIdentity identity = new AllocatorHandleIdentity(id, generation);

                newHandledPtr->ID = identity.ID;
                newHandledPtr->ByteLength = byteLength;
                HPtr handle = HPtr.FromHandledPtr(newHandledPtr, identity);

#if DRAGONECS_DEEP_DEBUG
                info.StackTrace = new System.Diagnostics.StackTrace();
#endif
                info.Increment = ++_inrement;
                info.Identity = identity;
                info.Type = type;
                info.ElementSize = elementSize;
                info.Alignment = alignment;
                info.Handle = handle;

                return handle;
            }
#else
            return HPtr.FromHandledPtr(newHandledPtr);
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
            int alignment = DragonUnsafe.AlignOf<T>();
            int alignmentPadding = GetAlignmentPadding(newCount, alignment);
            return new HMem<T>(ReallocAndInit_Internal(target, GetByteLength<T>(target.Length), GetByteLength<T>(newCount), typeof(T), sizeof(T), alignment, alignmentPadding), newCount);
        }
        public static HMem<T> ReallocAndInit<T>(HPtr target, int oldCount, int newCount) where T : unmanaged
        {
            int alignment = DragonUnsafe.AlignOf<T>();
            int alignmentPadding = GetAlignmentPadding(newCount, alignment);
            return new HMem<T>(ReallocAndInit_Internal(target, GetByteLength<T>(oldCount), GetByteLength<T>(newCount), typeof(T), sizeof(T), alignment, alignmentPadding), newCount);
        }
        public static HMem<byte> ReallocAndInit(HPtr target, int oldByteLength, int newByteLength)
        {
            return new HMem<byte>(ReallocAndInit_Internal(target, oldByteLength, newByteLength, null, sizeof(byte), UNKNOWN_ALIGNMENT, 0), newByteLength);
        }
        private static HPtr ReallocAndInit_Internal(HPtr target, int oldByteLength, int newByteLength, Type newType, int elementSize, int alignment, int alignmentPadding)
        {
#if DEBUG
            if (oldByteLength < 0) { throw new ArgumentOutOfRangeException(nameof(oldByteLength)); }
#endif
            int initializedByteLength = target.IsCreated ? Math.Min(oldByteLength, newByteLength) : 0;
            HPtr handler = Realloc_Internal(target, newByteLength, newType, elementSize, alignment, alignmentPadding);
            int allocatedDataByteLength = newByteLength + alignmentPadding;
            if (allocatedDataByteLength > initializedByteLength)
            {
                DragonUnsafe.ClearMemory(
                    handler.RawPtr,
                    initializedByteLength,
                    allocatedDataByteLength - initializedByteLength);
            }
            return handler;
        }
        #endregion

        #region Realloc
        public static HMem<T> Realloc<T>(T* target, int newCount) where T : unmanaged
        {
            return Realloc<T>(HPtr.FromDataPtr(target), newCount);
        }
        public static HMem<byte> Realloc(void* target, int newByteLength)
        {
            return new HMem<byte>(Realloc(HPtr.FromDataPtr(target), newByteLength), newByteLength);
        }
        public static HMem<T> Realloc<T>(HMem<T> target, int newCount) where T : unmanaged
        {
            int alignment = DragonUnsafe.AlignOf<T>();
            int alignmentPadding = GetAlignmentPadding(newCount, alignment);
            return new HMem<T>(Realloc_Internal(target, GetByteLength<T>(newCount), typeof(T), sizeof(T), alignment, alignmentPadding), newCount);
        }
        public static HMem<T> Realloc<T>(HPtr target, int newCount) where T : unmanaged
        {
            int alignment = DragonUnsafe.AlignOf<T>();
            int alignmentPadding = GetAlignmentPadding(newCount, alignment);
            return new HMem<T>(Realloc_Internal(target, GetByteLength<T>(newCount), typeof(T), sizeof(T), alignment, alignmentPadding), newCount);
        }
        public static HMem<byte> Realloc(HPtr target, int newByteLength)
        {
            return new HMem<byte>(Realloc_Internal(target, newByteLength, null, sizeof(byte), UNKNOWN_ALIGNMENT, 0), newByteLength);
        }
        private static HPtr Realloc_Internal(HPtr target, int newByteLength, Type newType, int elementSize, int alignment, int alignmentPadding)
        {
            newByteLength = newByteLength == 0 ? 1 : newByteLength;
            if (target.IsCreated == false)
            {
                return Alloc_Internal(newByteLength, newType, elementSize, alignment, alignmentPadding);
            }
#if DEBUG
            ValidateHandle_Debug(target);
            AllocatorHandleIdentity identity = target._identity;
#endif

            Meta* newHandledPtr = (Meta*)Marshal.ReAllocHGlobal(
                (IntPtr)target.GetHandledPtr(),
                (IntPtr)GetAllocationByteLength(newByteLength, alignmentPadding));
#if DEBUG
            newHandledPtr->ID = identity.ID;
            newHandledPtr->ByteLength = newByteLength;
            HPtr handler = HPtr.FromHandledPtr(newHandledPtr, identity);
            lock (_idDispenser)
            {
#if DRAGONECS_DEEP_DEBUG
                _debugInfos[identity.ID].StackTrace = new System.Diagnostics.StackTrace();
#endif
                _debugInfos[identity.ID].Increment = ++_inrement;
                _debugInfos[identity.ID].Type = newType;
                _debugInfos[identity.ID].ElementSize = elementSize;
                _debugInfos[identity.ID].Alignment = alignment;
                _debugInfos[identity.ID].Handle = handler;
            }
            return handler;
#else
            return HPtr.FromHandledPtr(newHandledPtr);
#endif
        }
        #endregion

        #region Clone
        public static HMem<T> From<T>(HMem<T> source)
            where T : unmanaged
        {
            var result = Alloc<T>(source.Length);
            source.AsSpan().CopyTo(result.AsSpan());
            return result;
        }
        public static HMem<T> From<T>(T* ptr, int length)
            where T : unmanaged
        {
            return From<T>(new ReadOnlySpan<T>(ptr, length));
        }
        public static HMem<T> From<T>(T[] source)
            where T : unmanaged
        {
            return From(new ReadOnlySpan<T>(source));
        }
        public static HMem<T> From<T>(ReadOnlySpan<T> source)
            where T : unmanaged
        {
            var result = Alloc<T>(source.Length);
            source.CopyTo(result.AsSpan());
            return result;
        }
        #endregion

        #region Free
        public static void Free(HPtr target)
        {
            Free_Internal(target);
        }
        public static void FreeAndClear<T>(ref HMem<T> target)
            where T : unmanaged
        {
            Free_Internal(target.Handle);
            target = default;
        }
        public static void FreeAndClear(ref HPtr target)
        {
            Free_Internal(target);
            target = default;
        }
        public static void Free(void* dataPtr)
        {
            Free_Internal(HPtr.FromDataPtr(dataPtr));
        }
        private static void Free_Internal(HPtr target)
        {
            Meta* handledPtr;
#if DEBUG
            lock (_idDispenser)
            {
                const int DensifyThreshold = 256;
                ValidateHandleWithoutLock_Debug(target);

                handledPtr = target.GetHandledPtr();
                int id = target._identity.ID;
                _releaseIDsCounter++;
                _idDispenser.Release(id);
                if (_releaseIDsCounter >= DensifyThreshold)
                {
                    _idDispenser.Sort();
                    _releaseIDsCounter = 0;
                }

                AllocatorHandleIdentity identity = _debugInfos[id].Identity;
                _debugInfos[id] = default;
                _debugInfos[id].Identity = identity;

                handledPtr->ID = default;
                handledPtr->ByteLength = default;
            }
#endif
            handledPtr = target.GetHandledPtr();
            Marshal.FreeHGlobal((IntPtr)handledPtr);
        }

#if DEBUG
        private static void ValidateHandle_Debug(HPtr target)
        {
            lock (_idDispenser)
            {
                ValidateHandleWithoutLock_Debug(target);
            }
        }
        private static void ValidateHandleWithoutLock_Debug(HPtr target)
        {
            if (target.Data == null)
            {
                throw new ArgumentNullException(nameof(target), "Cannot release an empty memory handle.");
            }
            AllocatorHandleIdentity identity = target._identity;
            if (identity.ID < 0 || identity.ID >= _debugInfos.Length || _idDispenser.IsNullID(identity.ID))
            {
                throw new InvalidOperationException($"Memory handle has an invalid allocation ID {identity.ID}.");
            }
            if (_idDispenser.IsFree(identity.ID))
            {
                throw new InvalidOperationException($"Memory handle {identity} has already been released.");
            }

            ref HandleDebugInfo info = ref _debugInfos[identity.ID];
            if (info.Identity != identity)
            {
                throw new InvalidOperationException($"Memory handle {identity} is stale. The current identity is {info.Identity}.");
            }
            if (info.Handle.Data != target.Data)
            {
                throw new InvalidOperationException($"Memory handle {identity} points to an obsolete allocation.");
            }
            if (target.GetHandledPtr()->ID != identity.ID)
            {
                throw new InvalidOperationException($"Memory handle {identity} does not match its allocation metadata.");
            }
        }
#endif
        #endregion

        #region Other
        internal static StateDebugInfo GetHandlerInfos_Debug()
        {
            StateDebugInfo result = default;
#if DEBUG
            result.IDDispenser = _idDispenser;
            result.DebugInfos = _debugInfos;
#endif
            return result;
        }

        internal struct Meta
        {
#if DEBUG
            public static int Size { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => sizeof(Meta); }
            public int ID;
            public int ByteLength;
#else
            public static int Size { [MethodImpl(MethodImplOptions.AggressiveInlining)]get => 0; }
#endif
        }



#if DEBUG
        [System.Diagnostics.DebuggerDisplay("{" + nameof(Handle) + "." + nameof(HPtr.DebuggerDisplay) + "()}")]
#endif
        internal struct HandleDebugInfo
        {
#if DEBUG
#if DRAGONECS_DEEP_DEBUG
            public System.Diagnostics.StackTrace StackTrace;
#endif
            public ulong Increment;
            public AllocatorHandleIdentity Identity;
            public Type Type;
            public int ElementSize;
            public int Alignment;
            public HPtr Handle;
#endif
        }
        internal struct StateDebugInfo
        {
            public HandleDebugInfo[] DebugInfos;
            public IdDispenser IDDispenser;
        }
        #endregion

        #region Handles
#if DEBUG
        [System.Diagnostics.DebuggerDisplay("{" + nameof(DebuggerDisplay) + "()}")]
        [System.Diagnostics.DebuggerTypeProxy(typeof(HMem<>.DebuggerProxy))]
#endif
        public readonly struct HMem<T> : IHMem<T>, IEquatable<HMem<T>>
            where T : unmanaged
        {
            public readonly T* Ptr;
            public readonly int Length;
#if DEBUG
            internal readonly AllocatorHandleIdentity _identity;
#endif

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
                get
                {
#if DEBUG
                    return HPtr.FromDataPtr(Ptr, _identity);
#else
                    return HPtr.FromDataPtr(Ptr);
#endif
                }
            }
            T* IHMem<T>.Ptr { get { return Ptr; } }
            int IHMem.Length { get { return Length; } }
            int IHMem.ByteLength { get { return checked(Length * sizeof(T)); } }
            void* IHMem.AlignedPtr { get { return TypeAlignment<T>.Align(Ptr); } }
            #endregion

            #region Constructors
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal HMem(HPtr handle, int length)
            {
                Ptr = handle.As<T>();
                Length = length;
#if DEBUG
                _identity = handle._identity;
#endif
            }
            #endregion

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Meta* GetHandledPtr() { return Handle.GetHandledPtr(); }
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
#if DEBUG
            public override string ToString() { return Handle.DebuggerDisplay(); }
#endif
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

            #region Debug
#if DEBUG
            internal string DebuggerDisplay()
            {
                if (IsCreated == false) { return "-"; }
                try
                {
                    ValidateHandle_Debug(this);
                }
                catch (Exception exception) { return exception.Message; }

                Meta meta = GetHandledPtr()[0];
                HandleDebugInfo info = _debugInfos[_identity.ID];

                if (info.Type == null)
                {
                    return $"[{_identity}] Count: {meta.ByteLength} Unknown";
                }
                return $"[{_identity}] Count: {meta.ByteLength / info.ElementSize} {info.Type.Name}";
            }
            private class DebuggerProxy
            {
                public bool IsAlive;
                public Meta Meta;
                public HandleDebugInfo DebugInfo;
                public T[] Data;
                public HandleDebugInfo[] OtherHandlesInfo;
                public DebuggerProxy(HMem<T> handle)
                {
                    if (handle.RawPtr.ToPointer() == null) { return; }
                    try
                    {
                        ValidateHandle_Debug(handle);
                    }
                    catch { return; }
                    IsAlive = true;
                    Meta = handle.GetHandledPtr()[0];
                    Data = handle.AsSpan().ToArray();
                    DebugInfo = _debugInfos[handle._identity.ID];
                    OtherHandlesInfo = _debugInfos;
                }
            }
#endif
            #endregion
        }

#if DEBUG
        [System.Diagnostics.DebuggerDisplay("{" + nameof(DebuggerDisplay) + "()}")]
        [System.Diagnostics.DebuggerTypeProxy(typeof(DebuggerProxy))]
#endif
        public readonly struct HPtr : IHPtr, IEquatable<HPtr>
        {
            public static readonly HPtr Empty = new HPtr(null, default);
            internal readonly Meta* Data;
#if DEBUG
            internal readonly AllocatorHandleIdentity _identity;
#endif

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
            private HPtr(Meta* dataPtr, AllocatorHandleIdentity identity)
            {
                Data = dataPtr;
#if DEBUG
                _identity = identity;
#endif
            }
            public static HPtr FromHandledPtr(void* ptr)
            {
                if (ptr == null)
                {
                    return default;
                }
                return FromDataPtr((byte*)ptr + Meta.Size);
            }
            public static HPtr FromDataPtr(void* ptr)
            {
                Meta* dataPtr = (Meta*)ptr;
#if DEBUG
                if (dataPtr == null)
                {
                    return default;
                }

                int id = ((Meta*)((byte*)dataPtr - Meta.Size))->ID;
                lock (_idDispenser)
                {
                    if (id < 0 || id >= _debugInfos.Length || _idDispenser.IsNullID(id) || _idDispenser.IsFree(id))
                    {
                        throw new InvalidOperationException($"Pointer refers to an inactive memory allocation ID {id}.");
                    }

                    ref HandleDebugInfo info = ref _debugInfos[id];
                    if (info.Handle.Data != dataPtr)
                    {
                        throw new InvalidOperationException($"Pointer does not match the active memory allocation ID {id}.");
                    }
                    return new HPtr(dataPtr, info.Identity);
                }
#else
                return new HPtr(dataPtr, default);
#endif
            }
            internal static HPtr FromHandledPtr(void* ptr, AllocatorHandleIdentity identity)
            {
                return new HPtr((Meta*)((byte*)ptr + Meta.Size), identity);
            }
            internal static HPtr FromDataPtr(void* ptr, AllocatorHandleIdentity identity)
            {
                return new HPtr((Meta*)ptr, identity);
            }
            #endregion

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Meta* GetHandledPtr() { return (Meta*)((byte*)Data - Meta.Size); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public U* As<U>() where U : unmanaged { return (U*)RawPtr; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() { Free(this); }

            internal int GetByteLength_Debug()
            {
#if DEBUG
                ValidateHandle_Debug(this);
                return GetHandledPtr()->ByteLength;
#else
                return 0;
#endif
            }

            #region Other
#if DEBUG
            public override string ToString() { return DebuggerDisplay(); }
#endif
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() { return RawPtr.GetHashCode(); }
            public override bool Equals(object obj) { return obj is HPtr h && Equals(h); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(HPtr other)
            {
                bool result = other.Data == Data;
#if DEBUG
                if (result && (other._identity != _identity))
                {
                    throw new InvalidOperationException(
                        $"The handles reference the same memory address but have different identities. " +
                        $"Expected {_identity}, actual {other._identity}. " +
                        $"The allocation was likely released and its address was reused.");
                }
#endif
                return result;
            }

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

            #region Debug
#if DEBUG
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
            internal string DebuggerDisplay()
            {
                if (IsCreated == false) { return "-"; }
                try
                {
                    ValidateHandle_Debug(this);
                }
                catch (Exception exception) { return exception.Message; }

                Meta meta = GetHandledPtr()[0];
                HandleDebugInfo info = _debugInfos[_identity.ID];

                if (info.Type == null)
                {
                    return $"[{_identity}] Count: {meta.ByteLength} Unknown";
                }
                return $"[{_identity}] Count: {meta.ByteLength / info.ElementSize} {info.Type.Name}";
            }

            private class DebuggerProxy
            {
                private readonly byte* _data;
                private readonly Type _type;
                public bool IsAlive;
                public Meta Meta;
                public HandleDebugInfo DebugInfo;
                public Array Data;

                public HandleDebugInfo[] OtherHandlesInfo;

                public DebuggerProxy(HPtr handle)
                {
                    if (handle.RawPtr.ToPointer() == null) { return; }
                    try
                    {
                        ValidateHandle_Debug(handle);
                    }
                    catch { return; }

                    IsAlive = true;

                    Meta = handle.GetHandledPtr()[0];
                    _data = (byte*)handle.RawPtr;
                    DebugInfo = _debugInfos[handle._identity.ID];

                    _type = DebugInfo.Type == null ? typeof(byte) : DebugInfo.Type;

                    Data = DragonUnsafe.CreateArray_Debug(_type, DebugInfo.ElementSize, _data, Meta.ByteLength);

                    OtherHandlesInfo = _debugInfos;
                }
            }
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#endif
            #endregion
        }
        #endregion
    }
}
