#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public readonly ref struct EcsSpan
    {
        private readonly ReadOnlySpan<int> _values;
        private readonly short _worldID;

        #region Properties
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _worldID == 0; }
        }
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _worldID; }
        }
        public EcsWorld World
        {
            get { return EcsWorld.GetWorld(_worldID); }
        }
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _values.Length; }
        }
        public EcsLongsSpan Longs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new EcsLongsSpan(this); }
        }
        public bool IsSourceEntities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return this == EcsWorld.GetWorld(_worldID).GetCurrentEntities_Internal(); }
        }
#if ENABLE_IL2CPP
        [Il2CppSetOption(Option.ArrayBoundsChecks, true)]
#endif
        public int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _values[index]; }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsSpan(short worldID, ReadOnlySpan<int> span)
        {
            _worldID = worldID;
            _values = span;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsSpan(short worldID, int[] array)
        {
            _worldID = worldID;
            _values = new ReadOnlySpan<int>(array);
        }
        internal EcsSpan(short worldID, int[] array, int length)
        {
            _worldID = worldID;
            _values = new ReadOnlySpan<int>(array, 0, length);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsSpan(short worldID, int[] array, int start, int length)
        {
            _worldID = worldID;
            _values = new ReadOnlySpan<int>(array, start, length);
        }
        #endregion

        #region Slice/ToArray
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start) { return new EcsSpan(_worldID, _values.Slice(start)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start, int length) { return new EcsSpan(_worldID, _values.Slice(start, length)); }
        public int[] ToArray() { return _values.ToArray(); }
        public int ToArray(ref int[] dynamicBuffer)
        {
            if (dynamicBuffer.Length < _values.Length)
            {
                Array.Resize(ref dynamicBuffer, ArrayUtility.NextPow2(_values.Length));
            }
            int i = 0;
            foreach (var e in this)
            {
                dynamicBuffer[i++] = e;
            }
            return i;
        }
        public void ToCollection(ICollection<int> collection)
        {
            foreach (var e in this)
            {
                collection.Add(e);
            }
        }
        #endregion

        #region operators
        public static bool operator ==(EcsSpan left, EcsSpan right) { return left._values == right._values; }
        public static bool operator !=(EcsSpan left, EcsSpan right) { return left._values != right._values; }
        #endregion

        #region Enumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<int>.Enumerator GetEnumerator() { return _values.GetEnumerator(); }
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int First() { return _values[0]; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Last() { return _values[_values.Length - 1]; }
        public override string ToString()
        {
            return CollectionUtility.EntitiesToString(_values.ToArray(), "span");
        }
#pragma warning disable CS0809 // Устаревший член переопределяет неустаревший член
        [Obsolete("Equals() on EcsSpan will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw new NotSupportedException(); }
        [Obsolete("GetHashCode() on EcsSpan will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() { throw new NotSupportedException(); }
#pragma warning restore CS0809 // Устаревший член переопределяет неустаревший член

        internal class DebuggerProxy
        {
            private int[] _values;
            private short _worldID;
            public EcsWorld World { get { return EcsWorld.GetWorld(_worldID); } }
            public EntitySlotInfo[] Entities
            {
                get
                {
                    EntitySlotInfo[] result = new EntitySlotInfo[_values.Length];
                    int i = 0;
                    foreach (var e in _values)
                    {
                        result[i++] = World.GetEntitySlotInfoDebug(e);
                    }
                    return result;
                }
            }
            public int Count { get { return _values.Length; } }
            public DebuggerProxy(EcsSpan span)
            {
                _values = new int[span.Count];
                span._values.CopyTo(_values);
                _worldID = span._worldID;
            }
            public DebuggerProxy(EcsLongsSpan span) : this(span.ToSpan()) { }
        }
        #endregion
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [DebuggerTypeProxy(typeof(EcsSpan.DebuggerProxy))]
    public readonly ref struct EcsLongsSpan
    {
        private readonly EcsSpan _source;

        #region Properties
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.IsNull; }
        }
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.WorldID; }
        }
        public EcsWorld World
        {
            get { return _source.World; }
        }
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.Count; }
        }
        public bool IsSourceEntities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.IsSourceEntities; }
        }
#if ENABLE_IL2CPP
        [Il2CppSetOption(Option.ArrayBoundsChecks, true)]
#endif
        public entlong this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return World.GetEntityLong(_source[index]); }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsLongsSpan(EcsSpan span)
        {
            _source = span;
        }
        #endregion

        #region Slice/ToSpan/ToArry
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsLongsSpan Slice(int start) { return new EcsLongsSpan(_source.Slice(start)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsLongsSpan Slice(int start, int length) { return new EcsLongsSpan(_source.Slice(start, length)); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ToSpan() { return _source; }
        public entlong[] ToArray()
        {
            entlong[] result = new entlong[_source.Count];
            int i = 0;
            foreach (var e in this)
            {
                result[i++] = e;
            }
            return result;
        }
        public int ToArray(ref entlong[] dynamicBuffer)
        {
            if (dynamicBuffer.Length < _source.Count)
            {
                Array.Resize(ref dynamicBuffer, ArrayUtility.NextPow2(_source.Count));
            }
            int i = 0;
            foreach (var e in this)
            {
                dynamicBuffer[i++] = e;
            }
            return i;
        }
        public void ToCollection(ICollection<entlong> collection)
        {
            foreach (var e in this)
            {
                collection.Add(e);
            }
        }
        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(EcsLongsSpan left, EcsLongsSpan right) { return left._source == right._source; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(EcsLongsSpan left, EcsLongsSpan right) { return left._source != right._source; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsSpan(EcsLongsSpan a) { return a.ToSpan(); }
        #endregion

        #region Enumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() { return new Enumerator(_source.World, _source.GetEnumerator()); }
        public ref struct Enumerator
        {
            private readonly EcsWorld _world;
            private ReadOnlySpan<int>.Enumerator _enumerator;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(EcsWorld world, ReadOnlySpan<int>.Enumerator enumerator)
            {
                _world = world;
                _enumerator = enumerator;
            }
            public entlong Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return _world.GetEntityLong(_enumerator.Current); }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() { return _enumerator.MoveNext(); }
        }
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong First() { return _source.World.GetEntityLong(_source.First()); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong Last() { return _source.World.GetEntityLong(_source.Last()); }
        public override string ToString()
        {
            return CollectionUtility.EntitiesToString(_source.ToArray(), "longs_span");
        }
#pragma warning disable CS0809 // Устаревший член переопределяет неустаревший член
        [Obsolete("Equals() on EcsLongSpan will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw new NotSupportedException(); }
        [Obsolete("GetHashCode() on EcsLongSpan will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() { throw new NotSupportedException(); }
#pragma warning restore CS0809 // Устаревший член переопределяет неустаревший член
        #endregion
    }
}