using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Internal
{
#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [DebuggerTypeProxy(typeof(UnsafeArray<>.DebuggerProxy))]
    internal unsafe struct UnsafeArray<T> : IDisposable, IEnumerable<T>
        where T : unmanaged
    {
        internal T* ptr;
        internal int Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray(int length)
        {
            UnmanagedArrayUtility.New(out ptr, length);
            Length = length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray(int length, bool isInit)
        {
            UnmanagedArrayUtility.NewAndInit(out ptr, length);
            Length = length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UnsafeArray(T* ptr, int length)
        {
            this.ptr = ptr;
            Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeArray<T> Clone()
        {
            return new UnsafeArray<T>(UnmanagedArrayUtility.Clone(ptr, Length), Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            UnmanagedArrayUtility.Free(ref ptr, ref Length);
        }
        public override string ToString()
        {
            T* ptr = this.ptr;
            return CollectionUtility.AutoToString(EnumerableInt.Range(0, Length).Select(i => ptr[i]), "ua");
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() { return new Enumerator(ptr, Length); }
        public struct Enumerator : IEnumerator<T>
        {
            private readonly T* _ptr;
            private readonly int _length;
            private int _index;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(T* ptr, int length)
            {
                _ptr = ptr;
                _length = length;
                _index = -1;
            }
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return _ptr[_index]; }
            }
            object IEnumerator.Current { get { return Current; } }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() { return ++_index < _length; }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IDisposable.Dispose() { }
            void IEnumerator.Reset() { throw new NotSupportedException(); }
        }

        internal class DebuggerProxy
        {
            public T[] elements;
            public int length;
            public DebuggerProxy(UnsafeArray<T> instance)
            {
                elements = EnumerableInt.Range(0, instance.Length).Select(i => instance.ptr[i]).ToArray();
                length = instance.Length;
            }
        }
    }
}
