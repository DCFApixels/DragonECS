#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
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
        public static Handler AllocAndInit<T>(int count) where T : unmanaged
        {
            return AllocAndInit_Internal(Marshal.SizeOf<T>() * count, typeof(T));
        }
        public static Handler AllocAndInit(int byteLength)
        {
            return AllocAndInit_Internal(byteLength, null);
        }
        public static Handler AllocAndInit_Internal(int byteLength, Type type)
        {
            Handler handler = Alloc_Internal(byteLength, type);
            AllocatorUtility.ClearAllocatedMemory(handler.Ptr, 0, byteLength);
            return handler;
        }
        #endregion

        #region Alloc
        public static Handler Alloc<T>(int count) where T : unmanaged
        {
            return Alloc_Internal(Marshal.SizeOf<T>() * count, typeof(T));
        }
        public static Handler Alloc(int byteLength)
        {
            return Alloc_Internal(byteLength, null);
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
                    Array.Resize(ref _debugInfos, _debugInfos.Length << 1);
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
        public static Handler ReallocAndInit<T>(void* target, int oldCount, int newCount) where T : unmanaged
        {
            return ReallocAndInit<T>(Handler.FromDataPtr(target), oldCount, newCount);
        }
        public static Handler ReallocAndInit(void* target, int oldByteLength, int newByteLength)
        {
            return ReallocAndInit(Handler.FromDataPtr(target), oldByteLength, newByteLength);
        }
        public static Handler ReallocAndInit<T>(Handler target, int oldCount, int newCount) where T : unmanaged
        {
            var size = Marshal.SizeOf<T>();
            return ReallocAndInit_Internal(target, size * oldCount, size * newCount, typeof(T));
        }
        public static Handler ReallocAndInit(Handler target, int oldByteLength, int newByteLength)
        {
            return ReallocAndInit_Internal(target, oldByteLength, newByteLength, null);
        }
        private static Handler ReallocAndInit_Internal(Handler target, int oldByteLength, int newByteLength, Type newType)
        {
            Handler handler = Realloc_Internal(target, newByteLength, newType);
            AllocatorUtility.ClearAllocatedMemory(handler.Ptr, oldByteLength, newByteLength - oldByteLength);
            return handler;
        }
        #endregion

        #region Realloc
        public static Handler Realloc<T>(void* target, int newCount) where T : unmanaged
        {
            return Realloc<T>(Handler.FromDataPtr(target), Marshal.SizeOf<T>() * newCount);
        }
        public static Handler Realloc(void* target, int newByteLength)
        {
            return Realloc(Handler.FromDataPtr(target), newByteLength);
        }
        public static Handler Realloc<T>(Handler target, int newCount) where T : unmanaged
        {
            return Realloc_Internal(target, Marshal.SizeOf<T>() * newCount, typeof(T));
        }
        public static Handler Realloc(Handler target, int newByteLength)
        {
            return Realloc_Internal(target, newByteLength, null);
        }
        private static Handler Realloc_Internal(Handler target, int newByteLength, Type newType)
        {
            newByteLength = newByteLength == 0 ? 1 : newByteLength;
            Meta* newHandledPtr = (Meta*)Marshal.ReAllocHGlobal((IntPtr)target.GetHandledPtr(), (IntPtr)newByteLength + sizeof(Meta));
            Handler handler = Handler.FromHandledPtr(newHandledPtr);
#if DEBUG
#if DRAGONECS_DEEP_DEBUG
            _debugInfos[newHandledPtr->ID].stackTrace = new System.Diagnostics.StackTrace();
#endif
            _debugInfos[newHandledPtr->ID].type = newType;
            _debugInfos[newHandledPtr->ID].handler = handler;
#endif
            return handler;
        }
        #endregion

        #region Free
        public static void Free(ref Handler target)
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

#if DEBUG
        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay()}")]
        [System.Diagnostics.DebuggerTypeProxy(typeof(DebuggerProxy))]
#endif
        public readonly struct Handler : IDisposable
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

            public bool IsEmpty { get { return Data == null; } }
            public IntPtr Ptr { get { return (IntPtr)Data; } }
            public T* As<T>() where T : unmanaged { return (T*)Ptr; }

            void IDisposable.Dispose() { Free((void*)Ptr); }

            #region Debugger
#if DEBUG
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
            internal unsafe string DebuggerDisplay()
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

                public unsafe DebuggerProxy(Handler handler)
                {
                    IsAlive = handler.Ptr.ToPointer() != null;
                    if (IsAlive == false) { return; }

                    Meta = handler.GetHandledPtr()[0];
                    _data = (byte*)handler.Ptr;
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
        public static void Dispose(this ref MemoryAllocator.Handler self)
        {
            MemoryAllocator.Free(ref self);
        }
    }
}