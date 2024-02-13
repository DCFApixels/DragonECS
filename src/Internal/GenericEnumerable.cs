using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Internal
{
    internal readonly struct GenericEnumerable<T, TEnumerator> : IEnumerable<T> where TEnumerator : IEnumerator<T>
    {
        public readonly TEnumerator _enumerator;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GenericEnumerable(TEnumerator enumerator) => _enumerator = enumerator;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TEnumerator GetEnumerator() => _enumerator;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _enumerator;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator() => _enumerator;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator GenericEnumerable<T, TEnumerator>(TEnumerator enumerator) => new GenericEnumerable<T, TEnumerator>(enumerator);
    }
}