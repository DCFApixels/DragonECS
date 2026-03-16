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
#if DEBUG
        private static IdDispenser _idDispenser;
        private static HandlerDebugInfo[] _debugInfos;
#endif

        static MemoryAllocator()
        {
            StaticInit();
        }
        private static void StaticInit()
        {
#if DEBUG
            _idDispenser = new IdDispenser();
            _debugInfos = new HandlerDebugInfo[32];
#endif
        }

        #region AllocAndInit
        public static HMem<T> AllocAndInit<T>(int count) where T : unmanaged
        {
            return new HMem<T>(AllocAndInit_Internal(Marshal.SizeOf<T>() * count, typeof(T)), count);
        }
        public static HMem<byte> AllocAndInit(int byteLength)
        {
            return new HMem<byte>(AllocAndInit_Internal(byteLength, null), byteLength);
        }
        public static Handler AllocAndInit_Internal(int byteLength, Type type)
        {
            Handler handler = Alloc_Internal(byteLength, type);
            AllocatorUtility.ClearAllocatedMemory(handler.RawPtr, 0, byteLength);
            return handler;
        }
        #endregion

        #region Alloc
        public static HMem<T> Alloc<T>(int count) where T : unmanaged
        {
            return new HMem<T>(Alloc_Internal(Marshal.SizeOf<T>() * count, typeof(T)), count);
        }
        public static HMem<byte> Alloc(int byteLength)
        {
            return new HMem<byte>(Alloc_Internal(byteLength, null), byteLength); ;
        }
        public static Handler Alloc_Internal(int byteLength, Type type)
        {
            byteLength = byteLength == 0 ? 1 : byteLength;
#if DEBUG
            int id = 0;
            lock (_idDispenser)
            {
                if (_debugInfos.Length <= _idDispenser.Count)
                {
                    Array.Resize(ref _debugInfos, ArrayUtility.NextPow2(_idDispenser.Count));
                }
                id = _idDispenser.UseFree();
            }
#endif
            Meta* newHandledPtr = (Meta*)Marshal.AllocHGlobal(byteLength + sizeof(Meta));
            Handler handler = Handler.FromHandledPtr(newHandledPtr);

#if DEBUG
            newHandledPtr->ID = id;
            newHandledPtr->ByteLength = byteLength;

#if DRAGONECS_DEEP_DEBUG
            _debugInfos[id].stackTrace = new System.Diagnostics.StackTrace();
#endif
            _debugInfos[id].type = type;
            _debugInfos[id].handler = handler;
#endif

            return handler;
        }
        #endregion

        #region ReallocAndInit
        public static HMem<T> ReallocAndInit<T>(void* target, int oldCount, int newCount) where T : unmanaged
        {
            return ReallocAndInit<T>(Handler.FromDataPtr(target), oldCount, newCount);
        }
        public static HMem<byte> ReallocAndInit(void* target, int oldByteLength, int newByteLength)
        {
            return ReallocAndInit(Handler.FromDataPtr(target), oldByteLength, newByteLength);
        }
        public static HMem<T> ReallocAndInit<T>(HMem<T> target, int newCount) where T : unmanaged
        {
            var size = Marshal.SizeOf<T>();
            return new HMem<T>(ReallocAndInit_Internal(target, size * target.Length, size * newCount, typeof(T)), newCount);
        }
        public static HMem<T> ReallocAndInit<T>(Handler target, int oldCount, int newCount) where T : unmanaged
        {
            var size = Marshal.SizeOf<T>();
            return new HMem<T>(ReallocAndInit_Internal(target, size * oldCount, size * newCount, typeof(T)), newCount);
        }
        public static HMem<byte> ReallocAndInit(Handler target, int oldByteLength, int newByteLength)
        {
            return new HMem<byte>(ReallocAndInit_Internal(target, oldByteLength, newByteLength, null), newByteLength);
        }
        private static Handler ReallocAndInit_Internal(Handler target, int oldByteLength, int newByteLength, Type newType)
        {
            Handler handler = Realloc_Internal(target, newByteLength, newType);
            AllocatorUtility.ClearAllocatedMemory(handler.RawPtr, oldByteLength, newByteLength - oldByteLength);
            return handler;
        }
        #endregion

        #region Realloc
        public static HMem<T> Realloc<T>(void* target, int newCount) where T : unmanaged
        {
            return Realloc<T>(Handler.FromDataPtr(target), Marshal.SizeOf<T>() * newCount);
        }
        public static HMem<byte> Realloc(void* target, int newByteLength)
        {
            return new HMem<byte>(Realloc(Handler.FromDataPtr(target), newByteLength), newByteLength);
        }
        public static HMem<T> Realloc<T>(Handler target, int newCount) where T : unmanaged
        {
            return new HMem<T>(Realloc_Internal(target, Marshal.SizeOf<T>() * newCount, typeof(T)), newCount);
        }
        public static HMem<byte> Realloc(Handler target, int newByteLength)
        {
            return new HMem<byte>(Realloc_Internal(target, newByteLength, null), newByteLength);
        }
        private static Handler Realloc_Internal(Handler target, int newByteLength, Type newType)
        {
            newByteLength = newByteLength == 0 ? 1 : newByteLength;
            if (target.IsCreated == false)
            {
                return Alloc_Internal(newByteLength, newType);
            }
#if DEBUG
            int id = 0;
            lock (_idDispenser)
            {
                if (_debugInfos.Length <= _idDispenser.Count)
                {
                    Array.Resize(ref _debugInfos, ArrayUtility.NextPow2(_idDispenser.Count));
                }
                id = _idDispenser.UseFree();
            }
#endif
            Meta* newHandledPtr = (Meta*)Marshal.ReAllocHGlobal(
                (IntPtr)target.GetHandledPtr(), 
                (IntPtr)newByteLength + sizeof(Meta));
            Handler handler = Handler.FromHandledPtr(newHandledPtr);
#if DEBUG
            newHandledPtr->ID = id;
            newHandledPtr->ByteLength = newByteLength;
#if DRAGONECS_DEEP_DEBUG
            _debugInfos[newHandledPtr->ID].stackTrace = new System.Diagnostics.StackTrace();
#endif
            _debugInfos[newHandledPtr->ID].type = newType;
            _debugInfos[newHandledPtr->ID].handler = handler;
#endif
            return handler;
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
        public static void Free(Handler target)
        {
            Free_Internal(target.GetHandledPtr());
        }
        public static void FreeAndClear<T>(ref HMem<T> target)
            where T : unmanaged
        {
            Free_Internal(target.Handler.GetHandledPtr());
            target = default;
        }
        public static void FreeAndClear(ref Handler target)
        {
            Free_Internal(target.GetHandledPtr());
            target = default;
        }
        public static void Free(void* dataPtr)
        {
            Free_Internal(((Meta*)dataPtr) - 1);
        }
        private static void Free_Internal(Meta* handledPtr)
        {
#if DEBUG
            if (handledPtr == null)
            {
                throw new ArgumentNullException();
            }
            lock (_idDispenser)
            {
                _idDispenser.Release(handledPtr->ID);
                _debugInfos[handledPtr->ID] = default;
            }
            handledPtr->ID = default;
            handledPtr->ByteLength = default;
#endif
            Marshal.FreeHGlobal((IntPtr)handledPtr);
        }
        #endregion

        #region Other
        internal static StateDebugInfo GetHandlerInfos_Debug()
        {
            StateDebugInfo result = default;
#if DEBUG
            result.idDispenser = _idDispenser;
            result.debugInfos = _debugInfos;
#endif
            return result;
        }

        internal struct Meta
        {
#if DEBUG
            public int ID;
            public int ByteLength;
#endif
        }

#if DEBUG
        [System.Diagnostics.DebuggerDisplay("{handler.DebuggerDisplay()}")]
#endif
        internal struct HandlerDebugInfo
        {
#if DEBUG
#if DRAGONECS_DEEP_DEBUG
            public System.Diagnostics.StackTrace stackTrace;
#endif
            public Type type;
            public Handler handler;
#endif
        }
        internal struct StateDebugInfo
        {
            public HandlerDebugInfo[] debugInfos;
            public IdDispenser idDispenser;
        }
        #endregion

        public readonly struct HMem<T> : IDisposable, IEquatable<HMem<T>>
            where T : unmanaged
        {
            public readonly T* Ptr;
            public readonly int Length;

            internal HMem(Handler handler, int length)
            {
                Ptr = handler.As<T>();
                Length = length;
            }

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
            public Handler Handler
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Handler.FromDataPtr(Ptr); }
            }

            public HMem<U> As<U>()
                where U : unmanaged
            {
                if (IsCreated)
                {
                    return default;
                }

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

                return new HMem<U>(Handler, (int)newLengthLong);
            }
            public void Dispose()
            {
                Handler.Dispose();
            }

            public override string ToString() { return Handler.DebuggerDisplay(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() { return RawPtr.GetHashCode(); }
            public override bool Equals(object obj) { return obj is Handler h && h == this; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(HMem<T> other) { return other.Ptr == Ptr; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(HMem<T> a, HMem<T> b) { return a.Ptr == b.Ptr; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(HMem<T> a, HMem<T> b) { return a.Ptr != b.Ptr; }

            public Span<T> AsSpan() { return new Span<T>(Ptr, Length); }
            public Span<T> AsSpan(int length)
            {
#if DEBUG
                if (length > Length) { Throw.UndefinedException(); }
#endif
                return new Span<T>(Ptr, length);
            }
            public static implicit operator Handler(HMem<T> memory) { return memory.Handler; }
        }

#if DEBUG
        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay()}")]
        [System.Diagnostics.DebuggerTypeProxy(typeof(DebuggerProxy))]
#endif
        public readonly struct Handler : IDisposable, IEquatable<Handler>
        {
            public static readonly Handler Empty = new Handler();
            internal readonly Meta* Data; // Data[-1] is meta;
            private Handler(Meta* dataPtr) { Data = dataPtr; }
            public static Handler FromHandledPtr(void* ptr) { return new Handler(((Meta*)ptr) + 1); }
            public static Handler FromDataPtr(void* ptr) { return new Handler((Meta*)ptr); }
            internal Meta* GetHandledPtr() { return Data - 1; }
            internal int GetID_Debug()
            {
#if DEBUG
                return GetHandledPtr()->ID;
#else
                return 0;
#endif
            }
            internal int GetByteLength_Debug()
            {
#if DEBUG
                return GetHandledPtr()->ByteLength;
#else
                return 0;
#endif
            }

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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T* As<T>() where T : unmanaged { return (T*)RawPtr; }

            public void Dispose() { Free((void*)RawPtr); }

            public override string ToString() { return DebuggerDisplay(); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() { return RawPtr.GetHashCode(); }
            public override bool Equals(object obj) { return obj is Handler h && h == this; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(Handler other) { return other.Data == Data; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Handler a, Handler b) { return a.Data == b.Data; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Handler a, Handler b) { return a.Data != b.Data; }

            #region Debugger
#if DEBUG
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
            internal string DebuggerDisplay()
            {
                if (Data == null)
                {
                    return "-";
                }

                Meta meta = GetHandledPtr()[0];
                HandlerDebugInfo info = _debugInfos[meta.ID];

                if (info.type == null)
                {
                    return $"Count: {meta.ByteLength} Unknown";
                }

                return $"Count: {meta.ByteLength / Marshal.SizeOf(info.type)} {info.type.Name}";
            }
            internal static Array CreateArray_Debug(Type type, int count, byte* data, int byteLength)
            {
                var array = Array.CreateInstance(type, count);
                if (array.Length > 0)
                {
                    Union union = default;
                    union.array = array;
                    fixed (byte* arrayPtr = union.bytes)
                    {
                        for (int i = 0; i < byteLength; i++)
                        {
                            arrayPtr[i] = data[i];
                        }
                    }
                }
                return array;
            }

            [StructLayout(LayoutKind.Explicit)]
            private unsafe struct Union
            {
                [FieldOffset(0)]
                public Array array;
                [FieldOffset(0)]
                public byte[] bytes;
            }
            private class DebuggerProxy
            {
                private byte* _data;
                private Type _type;
                private int _count;

                public bool IsAlive;
                public Meta Meta;
                public HandlerDebugInfo DebugInfo;
                public Array Data;

                public HandlerDebugInfo[] OtherHandlersInfo;

                public DebuggerProxy(Handler handler)
                {
                    IsAlive = handler.RawPtr.ToPointer() != null;
                    if (IsAlive == false) { return; }

                    Meta = handler.GetHandledPtr()[0];
                    _data = (byte*)handler.RawPtr;
                    DebugInfo = _debugInfos[Meta.ID];

                    if (DebugInfo.type == null)
                    {
                        _type = typeof(byte);
                    }
                    else
                    {
                        _type = DebugInfo.type;
                    }

                    var size = Marshal.SizeOf(_type);
                    _count = Meta.ByteLength / size;

                    Data = CreateArray_Debug(_type, _count, _data, Meta.ByteLength);

                    OtherHandlersInfo = _debugInfos;
                }
            }
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
#endif
            #endregion
        }
    }

    internal static class MemoryAllocatorHandlerExtensions
    {
        public static void DisposeAndReset(this ref MemoryAllocator.Handler self)
        {
            MemoryAllocator.FreeAndClear(ref self);
        }
        public static void DisposeAndReset<T>(this ref MemoryAllocator.HMem<T> self)
            where T : unmanaged
        {
            self.Dispose();
            self = default;
        }
    }
}