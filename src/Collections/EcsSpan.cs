#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Core.Internal;
using DCFApixels.DragonECS.Core.Unchecked;
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
    /// <summary>
    /// Lightweight read-only span of entity ids belonging to a specific world.
    /// Use returned spans from queries and groups to iterate or convert to arrays.
    /// </summary>
    /// <remarks>
    /// The span always contains a set of unique entity identifiers — no duplicates are present.
    /// </remarks>
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public readonly ref struct EcsSpan
    {
        private readonly ReadOnlySpan<int> _values;
        private readonly short _worldID;

        #region Properties
        /// <summary>
        /// True when the span does not reference a valid world (null world id).
        /// </summary>
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _worldID == 0; }
        }

        /// <summary>
        /// Identifier of the world this span belongs to.
        /// </summary>
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _worldID; }
        }

        /// <summary>
        /// The EcsWorld instance owning the entities in this span.
        /// </summary>
        public EcsWorld World
        {
            get { return EcsWorld.GetWorld(_worldID); }
        }

        /// <summary>
        /// Number of entity ids in the span.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _values.Length; }
        }

        /// <summary>
        /// Returns a view that exposes the same entities as packed <see cref="entlong"/> handles.
        /// </summary>
        public EcsLongsSpan Longs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new EcsLongsSpan(this); }
        }

        /// <summary>
        /// True when the span represents the world's current live entities collection.
        /// </summary>
        public bool IsSourceEntities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _values == EcsWorld.GetWorld(_worldID).GetCurrentEntities_Internal()._values; }
        }

        /// <summary>
        /// Indexer to access the entity id at the given index in the span.
        /// </summary>
        /// <param name="index">Zero-based index in the span.</param>
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
        /// <summary>
        /// Returns a slice of this span starting at the specified index.
        /// </summary>
        /// <param name="start">Zero-based start index.</param>
        /// <returns>A span over the remaining entities.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start) { return new EcsSpan(_worldID, _values.Slice(start)); }

        /// <summary>
        /// Returns a slice of this span with the specified start and length.
        /// </summary>
        /// <param name="start">Zero-based start index.</param>
        /// <param name="length">Number of entities in the slice.</param>
        /// <returns>A span over the requested range.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start, int length) { return new EcsSpan(_worldID, _values.Slice(start, length)); }

        /// <summary>
        /// Converts the span to a managed array of entity ids.
        /// </summary>
        /// <returns>A new array containing the entity ids.</returns>
        public int[] ToArray() { return _values.ToArray(); }

        /// <summary>
        /// Copies entity ids into a reusable buffer, growing it when necessary.
        /// </summary>
        /// <param name="dynamicBuffer">Reusable destination buffer.</param>
        /// <returns>The number of elements written.</returns>
        public int ToArray(ref int[] dynamicBuffer)
        {
            if (dynamicBuffer.Length < _values.Length)
            {
                Array.Resize(ref dynamicBuffer, ArrayUtility.CeilPow2(_values.Length));
            }
            int i = 0;
            foreach (var e in this)
            {
                dynamicBuffer[i++] = e;
            }
            return i;
        }

        /// <summary>
        /// Adds all entity ids from the span to the provided collection.
        /// </summary>
        /// <param name="collection">Collection that receives the entity ids.</param>
        public void ToCollection(ICollection<int> collection)
        {
            foreach (var e in this)
            {
                collection.Add(e);
            }
        }
        #endregion

        #region operators
        public static bool operator ==(EcsSpan left, EcsSpan right) { return left._values == right._values && left._worldID == right._worldID; }
        public static bool operator !=(EcsSpan left, EcsSpan right) { return left._values != right._values || left._worldID != right._worldID; }
        #endregion

        #region Enumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<int>.Enumerator GetEnumerator() { return _values.GetEnumerator(); }
        #endregion

        #region Other
        public ReadOnlySpan<int> AsSystemSpan() { return _values; }

        /// <summary>
        /// Returns the first entity id in the span.
        /// </summary>
        /// <returns>The first entity id.</returns>
        /// <remarks>The span must not be empty.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int First() { return _values[0]; }

        /// <summary>
        /// Returns the last entity id in the span.
        /// </summary>
        /// <returns>The last entity id.</returns>
        /// <remarks>The span must not be empty.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Last() { return _values[_values.Length - 1]; }
        public override string ToString()
        {
            return CollectionUtility.EntitiesToString(_values.ToArray(), "span");
        }
#pragma warning disable CS0809 // Устаревший член переопределяет неустаревший член
        [Obsolete("Equals() on EcsSpan will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) { Throw.EntitySpan_EqualsNotSupported(); return false; }
        [Obsolete("GetHashCode() on EcsSpan will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() { Throw.EntitySpan_GetHashCodeNotSupported(); return 0; }
#pragma warning restore CS0809 // Устаревший член переопределяет неустаревший член

        internal class DebuggerProxy
        {
            private int[] _values;
            private short _worldID;
            public EcsWorld World { get { return EcsWorld.GetWorld(_worldID); } }
            public RawEntLong[] Entities
            {
                get
                {
                    RawEntLong[] result = new RawEntLong[_values.Length];
                    int i = 0;
                    foreach (var e in _values)
                    {
                        result[i++] = World.GetRawEntLong(e);
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
            public DebuggerProxy(EcsUnsafeSpan span) : this(span.ToSpan()) { }
        }
        #endregion
    }

    /// <summary>
    /// Read-only view that exposes the entities of an <see cref="EcsSpan"/> as packed <see cref="entlong"/> handles.
    /// </summary>
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [DebuggerTypeProxy(typeof(EcsSpan.DebuggerProxy))]
    public readonly ref struct EcsLongsSpan
    {
        private readonly EcsSpan _source;

        #region Properties
        /// <summary>
        /// True when the span does not reference a valid world (null world id).
        /// </summary>
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.IsNull; }
        }

        /// <summary>
        /// Identifier of the world this span belongs to.
        /// </summary>
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.WorldID; }
        }

        /// <summary>
        /// The EcsWorld instance owning the entities in this span.
        /// </summary>
        public EcsWorld World
        {
            get { return _source.World; }
        }

        /// <summary>
        /// Number of packed entity handles in the span.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.Count; }
        }

        /// <summary>
        /// True when the span represents the world's current live entities collection.
        /// </summary>
        public bool IsSourceEntities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.IsSourceEntities; }
        }

        /// <summary>
        /// Indexer to access the packed entity handle at the given index.
        /// </summary>
        /// <param name="index">Zero-based index in the span.</param>
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
        /// <summary>
        /// Returns a slice of this span starting at the specified index.
        /// </summary>
        /// <param name="start">Zero-based start index.</param>
        /// <returns>A packed-handle view over the remaining entities.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsLongsSpan Slice(int start) { return new EcsLongsSpan(_source.Slice(start)); }

        /// <summary>
        /// Returns a slice of this span with the specified start and length.
        /// </summary>
        /// <param name="start">Zero-based start index.</param>
        /// <param name="length">Number of entities in the slice.</param>
        /// <returns>A packed-handle view over the requested range.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsLongsSpan Slice(int start, int length) { return new EcsLongsSpan(_source.Slice(start, length)); }

        /// <summary>
        /// Converts this packed-handle view to the underlying entity ID span.
        /// </summary>
        /// <returns>The underlying entity ID span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ToSpan() { return _source; }

        /// <summary>
        /// Converts the span to a managed array of packed entity handles.
        /// </summary>
        /// <returns>A new array containing the packed entity handles.</returns>
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

        /// <summary>
        /// Copies packed entity handles into a reusable buffer, growing it when necessary.
        /// </summary>
        /// <param name="dynamicBuffer">Reusable destination buffer.</param>
        /// <returns>The number of elements written.</returns>
        public int ToArray(ref entlong[] dynamicBuffer)
        {
            if (dynamicBuffer.Length < _source.Count)
            {
                Array.Resize(ref dynamicBuffer, ArrayUtility.CeilPow2(_source.Count));
            }
            int i = 0;
            foreach (var e in this)
            {
                dynamicBuffer[i++] = e;
            }
            return i;
        }

        /// <summary>
        /// Adds all packed entity handles from the span to the provided collection.
        /// </summary>
        /// <param name="collection">Collection that receives the packed entity handles.</param>
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
        /// <summary>
        /// Returns the first packed entity handle in the span.
        /// </summary>
        /// <returns>The first packed entity handle.</returns>
        /// <remarks>The span must not be empty.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong First() { return _source.World.GetEntityLong(_source.First()); }

        /// <summary>
        /// Returns the last packed entity handle in the span.
        /// </summary>
        /// <returns>The last packed entity handle.</returns>
        /// <remarks>The span must not be empty.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong Last() { return _source.World.GetEntityLong(_source.Last()); }
        public override string ToString()
        {
            return CollectionUtility.EntitiesToString(_source.ToArray(), "longs_span");
        }
#pragma warning disable CS0809 // Устаревший член переопределяет неустаревший член
        [Obsolete("Equals() on EcsLongSpan will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) { Throw.EntitySpan_EqualsNotSupported(); return false; }
        [Obsolete("GetHashCode() on EcsLongSpan will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() { Throw.EntitySpan_GetHashCodeNotSupported(); return 0; }
#pragma warning restore CS0809 // Устаревший член переопределяет неустаревший член
        #endregion
    }
}

namespace DCFApixels.DragonECS.Core
{
#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;
#endif
    /// <summary>
    /// Pointer-backed read-only span of entity ids belonging to a specific world.
    /// </summary>
    /// <remarks>
    /// Suitable for use in Unity Jobs or other high‑performance contexts. The span does not own the referenced memory;
    /// the caller must ensure that memory remains valid for the entire lifetime of the span.
    /// </remarks>
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [DebuggerTypeProxy(typeof(EcsSpan.DebuggerProxy))]
    public unsafe readonly struct EcsUnsafeSpan
    {
#if UNITY_2020_3_OR_NEWER
        [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
#endif
        private readonly int* _values;
        private readonly int _length;
        private readonly short _worldID;

        #region Properties
        /// <summary>
        /// True when the span does not reference a valid world (null world id).
        /// </summary>
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _worldID == 0; }
        }

        /// <summary>
        /// Identifier of the world this span belongs to.
        /// </summary>
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _worldID; }
        }

        /// <summary>
        /// The EcsWorld instance owning the entities in this span.
        /// </summary>
        public EcsWorld World
        {
            get { return EcsWorld.GetWorld(_worldID); }
        }

        /// <summary>
        /// Number of entity ids in the span.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _length; }
        }

        /// <summary>
        /// Returns a view that exposes the same entities as packed <see cref="entlong"/> handles.
        /// </summary>
        public EcsLongsSpan Longs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ToSpan().Longs; }
        }

        /// <summary>
        /// True when the span represents the world's current live entities collection.
        /// </summary>
        public bool IsSourceEntities
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ToSpan().IsSourceEntities; }
        }

        /// <summary>
        /// Indexer to access the entity id at the given index in the span.
        /// </summary>
        /// <param name="index">Zero-based index in the span.</param>
#if ENABLE_IL2CPP
        [Il2CppSetOption(Option.ArrayBoundsChecks, true)]
#endif
        public int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if DEBUG
                if ((uint)index >= (uint)_length || (uint)index < 0) { Throw.EntitySpan_IndexOutOfRange(); }
#elif DRAGONECS_STABILITY_MODE
                if ((uint)index >= (uint)_length || (uint)index < 0) { return EcsConsts.NULL_ENTITY_ID; }
#endif
                return _values[index];
            }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsUnsafeSpan(short worldID, int* array, int length)
        {
            _worldID = worldID;
            _values = array;
            _length = length;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsUnsafeSpan(short worldID, int* array, int start, int length)
        {
            _worldID = worldID;
            _values = array + start;
            _length = length;
        }
        #endregion

        #region Slice/ToArray
        /// <summary>
        /// Returns a slice of this span starting at the specified index.
        /// </summary>
        /// <param name="start">Zero-based start index.</param>
        /// <returns>An unsafe span over the remaining entities.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsUnsafeSpan Slice(int start)
        {
            if ((uint)start > (uint)_length)
            {
                Throw.EntitySpan_SliceOutOfRange();
            }
            return new EcsUnsafeSpan(_worldID, _values, start, _length - start);
        }

        /// <summary>
        /// Returns a slice of this span with the specified start and length.
        /// </summary>
        /// <param name="start">Zero-based start index.</param>
        /// <param name="length">Number of entities in the slice.</param>
        /// <returns>An unsafe span over the requested range.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsUnsafeSpan Slice(int start, int length)
        {
            if ((uint)start > (uint)_length || (uint)length > (uint)(_length - start))
            {
                Throw.EntitySpan_SliceOutOfRange();
            }
            return new EcsUnsafeSpan(_worldID, _values, start, length);
        }

        /// <summary>
        /// Converts this pointer-backed view to a standard entity ID span.
        /// </summary>
        /// <returns>A standard read-only entity span over the same memory.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ToSpan() { return new EcsSpan(_worldID, new ReadOnlySpan<int>(_values, _length)); }

        /// <summary>
        /// Converts the span to a managed array of entity ids.
        /// </summary>
        /// <returns>A new array containing the entity ids.</returns>
        public int[] ToArray() { return new ReadOnlySpan<int>(_values, _length).ToArray(); }

        /// <summary>
        /// Copies entity ids into a reusable buffer, growing it when necessary.
        /// </summary>
        /// <param name="dynamicBuffer">Reusable destination buffer.</param>
        /// <returns>The number of elements written.</returns>
        public int ToArray(ref int[] dynamicBuffer)
        {
            if (dynamicBuffer.Length < _length)
            {
                Array.Resize(ref dynamicBuffer, ArrayUtility.CeilPow2(_length));
            }
            int i = 0;
            foreach (var e in this)
            {
                dynamicBuffer[i++] = e;
            }
            return i;
        }

        /// <summary>
        /// Adds all entity ids from the span to the provided collection.
        /// </summary>
        /// <param name="collection">Collection that receives the entity ids.</param>
        public void ToCollection(ICollection<int> collection)
        {
            foreach (var e in this)
            {
                collection.Add(e);
            }
        }
        #endregion

        #region operators
        public static bool operator ==(EcsUnsafeSpan left, EcsUnsafeSpan right) { return left._values == right._values && left._length == right._length && left._worldID == right._worldID; }
        public static bool operator !=(EcsUnsafeSpan left, EcsUnsafeSpan right) { return left._values != right._values || left._length != right._length || left._worldID != right._worldID; }
        public static implicit operator EcsSpan(EcsUnsafeSpan a) { return a.ToSpan(); }
        #endregion

        #region Enumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<int>.Enumerator GetEnumerator() { return new ReadOnlySpan<int>(_values, _length).GetEnumerator(); }
        #endregion

        #region Other
        /// <summary>
        /// Returns the first entity id in the span.
        /// </summary>
        /// <returns>The first entity id.</returns>
        /// <remarks>The span must not be empty.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int First() { return _values[0]; }

        /// <summary>
        /// Returns the last entity id in the span.
        /// </summary>
        /// <returns>The last entity id.</returns>
        /// <remarks>The span must not be empty.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Last() { return _values[_length - 1]; }
        public override string ToString()
        {
            return CollectionUtility.EntitiesToString(ToArray(), "span");
        }
        public override bool Equals(object obj)
        {
            return obj is EcsUnsafeSpan other && other == this;
        }
        public override int GetHashCode()
        {
            return *_values ^ _length ^ (_worldID << 16);
        }
        #endregion
    }
}
