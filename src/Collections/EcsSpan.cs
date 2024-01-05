using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public readonly ref struct EcsSpan
    {
        private readonly int _worldID;
        private readonly ReadOnlySpan<int> _values;

        #region Properties
        public int WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _worldID;
        }
        public EcsWorld World
        {
            get => EcsWorld.GetWorld(_worldID);
        }
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values.Length;
        }
        public readonly int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values[index];
        }
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values.IsEmpty;
    }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsSpan(int worldID, ReadOnlySpan<int> span)
        {
            _worldID = worldID;
            _values = span;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsSpan(int worldID, int[] array)
        {
            _worldID = worldID;
            _values = array;
        }
        internal EcsSpan(int worldID, int[] array, int length)
        {
            _worldID = worldID;
            _values = new ReadOnlySpan<int>(array, 0, length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsSpan(int worldID, int[] array, int start, int length)
        {
            _worldID = worldID;
            _values = new ReadOnlySpan<int>(array, start, length);
        }
        #endregion

        #region Methdos
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] Bake()
        {
            int[] result = new int[_values.Length];
            _values.CopyTo(result);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Bake(ref int[] entities)
        {
            if (entities.Length < _values.Length)
                Array.Resize(ref entities, _values.Length);
            int[] result = new int[_values.Length];
            _values.CopyTo(result);
            return _values.Length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Bake(List<int> entities)
        {
            entities.Clear();
            foreach (var e in _values)
            {
                entities.Add(e);
            }
        }
        #endregion

        #region operators
        public static bool operator ==(EcsSpan left, EcsSpan right) => left._values == right._values;
        public static bool operator !=(EcsSpan left, EcsSpan right) => left._values != right._values;
        #endregion

        #region Enumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<int>.Enumerator GetEnumerator() => _values.GetEnumerator();
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start) => new EcsSpan(_worldID, _values.Slice(start));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start, int length) => new EcsSpan(_worldID, _values.Slice(start, length));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] ToArray() => _values.ToArray();

#pragma warning disable CS0809 // Устаревший член переопределяет неустаревший член
        [Obsolete("Equals() on EcsSpan will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new NotSupportedException();
        [Obsolete("GetHashCode() on EcsSpan will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new NotSupportedException();
#pragma warning restore CS0809 // Устаревший член переопределяет неустаревший член
        public override string ToString()
        {
            return $"span({_values.Length}) {{{string.Join(", ", _values.ToArray().OrderBy(o => o))}}}";
        }

        internal class DebuggerProxy
        {
            private int[] _values;
            private int _worldID;
            public EcsWorld World => EcsWorld.GetWorld(_worldID);
            public entlong[] Entities
            {
                get
                {
                    entlong[] result = new entlong[_values.Length];
                    int i = 0;
                    foreach (var e in _values)
                        result[i++] = World.GetEntityLong(e);
                    return result;
                }
            }
            public int Count => _values.Length;
            //public override string ToString() => $"span({_values.Length}) {{{string.Join(", ", _values.OrderBy(o => o))}}}";
            public DebuggerProxy(EcsSpan span)
            {
                _values = new int[span.Length];
                span._values.CopyTo(_values);
                _worldID = span._worldID;
            }
        }
        #endregion
    }
}
