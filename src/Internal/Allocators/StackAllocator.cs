#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS.Core.Internal
{
    internal unsafe class StackAllocator
    {
        [ThreadStatic]
        private static MemoryAllocator.Handler _stackPtrHandler;
        [ThreadStatic]
        private static int _stackByteLength;
        [ThreadStatic]
        private static byte* _stackPtr;
        [ThreadStatic]
        private static int _currentByteLength;
        [ThreadStatic]
        private static byte* _currentPtr;
#if DEBUG
        [ThreadStatic]
        private static int _increment;
#endif
        [ThreadStatic]
        private static HandlerDebugInfo[] _debugInfos;

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
        /*
        SAMPLE
        using (StackAllocator.HybridAlloc(requiredSize, THRESHOLD, out SomeData* preSortingBuffer))
        {
            if (requiredSize < THRESHOLD)
            {
                SomeData* ptr = stackalloc SomeData[requiredSize];
                preSortingBuffer = ptr;
            }

        } 
        */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Handler HybridAlloc<T>(int count, int threshold, out T* ptr) where T : unmanaged
        {
            if (count < threshold)
            {
                ptr = null;
                return Handler.FromHandledPtr(_currentPtr);
            }
            else
            {
                return Alloc(count, out ptr);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Handler Alloc<T>(int count, out T* ptr) where T : unmanaged
        {
            var handler = Alloc_Internal(Marshal.SizeOf<T>() * count, typeof(T));
            ptr = handler.As<T>();
            return handler;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Handler Alloc(int byteLength, out byte* ptr)
        {
            var handler = Alloc_Internal(byteLength, null);
            ptr = handler.As<byte>();
            return handler;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Handler Alloc_Internal(int byteLength, Type type)
        {
            byteLength = byteLength == 0 ? 1 : byteLength;
            var requiredByteLength = byteLength + sizeof(Meta);
#if DEBUG
            int id = _increment++;
#endif
            if (_currentByteLength < requiredByteLength)
            {
                Upsize(requiredByteLength);
            }
            Meta* newHandledPtr = (Meta*)_currentPtr;
            _currentPtr += requiredByteLength;
            Handler handler = Handler.FromHandledPtr(newHandledPtr);

#if DEBUG
            newHandledPtr->ID = id;
            newHandledPtr->ByteLength = byteLength;

            _debugInfos[id].stackTrace = new System.Diagnostics.StackTrace();
            _debugInfos[id].type = type;
#endif

            return handler;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Upsize(int newSize)
        {
            if (_stackPtrHandler.IsEmpty)
            {
                _stackByteLength = Math.Max(512, ArrayUtility.NextPow2(newSize));
                _stackPtrHandler = MemoryAllocator.Alloc<byte>(_stackByteLength);
                _stackPtr = _stackPtrHandler.As<byte>();
                _currentPtr = _stackPtr;
                _currentByteLength = 0;
            }
            else
            {
                var usedLength = _stackByteLength - _currentByteLength;
                _stackByteLength = ArrayUtility.NextPow2(newSize + usedLength);
                _stackPtrHandler = MemoryAllocator.Realloc<byte>(_stackPtrHandler, _stackByteLength);
                _stackPtr = _stackPtrHandler.As<byte>();
                _currentPtr = _stackPtr + usedLength;
                _currentByteLength = _stackByteLength - usedLength;
            }
        }
        #endregion

        #region Free
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(ref Handler target)
        {
            Free_Internal(target.GetHandledPtr());
            target = default;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(void* dataPtr)
        {
            Free_Internal(((Meta*)dataPtr) - 1);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Free_Internal(Meta* handledPtr)
        {
#if DEBUG
            if(handledPtr != _currentPtr) // Если значения одинаковые то это Handler из HybridAlloc, при условии что выделение памяти произошло в основном стеке, а не в StackAllocator
            {
                _increment--;
                if (_increment-- != handledPtr->ID)
                {
                    Throw.UndefinedException();
                }
                handledPtr->ID = default;
                handledPtr->ByteLength = default;
            }
#endif
            byte* ptr = (byte*)handledPtr;
            long byteDifference = _currentPtr - ptr;

            _currentPtr = ptr;
            _currentByteLength += (int)byteDifference;
        }
        #endregion

        #region Other
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
            public System.Diagnostics.StackTrace stackTrace;
            public Type type;
#endif
        }
        #endregion


        public readonly struct Handler : IDisposable
        {
            public static Handler Empty => new Handler();
            internal readonly Meta* Data; // Data[-1] is meta;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private Handler(Meta* dataPtr) { Data = dataPtr; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Handler FromHandledPtr(void* ptr) { return new Handler(((Meta*)ptr) + 1); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Handler FromDataPtr(void* ptr) { return new Handler((Meta*)ptr); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Meta* GetHandledPtr() { return Data - 1; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal int GetID_Debug()
            {
#if DEBUG
                return GetHandledPtr()->ID; 
#else
                return 0;
#endif
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal int GetByteLength_Debug()
            {
#if DEBUG
                return GetHandledPtr()->ByteLength; 
#else
                return 0;
#endif
            }

            public bool IsEmpty
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return Data == null; }
            }
            public IntPtr Ptr
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return (IntPtr)Data; }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T* As<T>() where T : unmanaged { return (T*)Ptr; }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IDisposable.Dispose() { Free((void*)Ptr); }
        }
    }

    internal static class StackAllocatorHandlerExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Dispose(this ref StackAllocator.Handler self)
        {
            StackAllocator.Free(ref self);
        }
    }
}