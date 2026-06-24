#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core.Internal;
using DCFApixels.DragonECS.Core.Unchecked;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

//_dense заполняется с индекса 1
//в операциях изменяющих состояние группы нельзя итерироваться по this, либо осторожно учитывать этот момент
namespace DCFApixels.DragonECS
{
#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    /// <summary>
    /// Read-only lightweight view over an <see cref="EcsGroup"/> instance.
    /// Provides safe accessors and non-mutating convenience methods for consumers.
    /// </summary>
    [DebuggerTypeProxy(typeof(EcsGroup.DebuggerProxy))]
    public readonly ref struct EcsReadonlyGroup
    {
        private readonly EcsGroup _source;

        #region Properties
        /// <summary>
        /// Returns true when the underlying group reference is null.
        /// </summary>
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source == null; }
        }

        /// <summary>
        /// Identifier of the world that owns the group.
        /// </summary>
        public int WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.WorldID; }
        }

        /// <summary>
        /// The <see cref="EcsWorld"/> instance that owns the group.
        /// </summary>
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.World; }
        }

        /// <summary>
        /// Number of entities currently contained in the group.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.Count; }
        }

        /// <summary>
        /// Current dense-array capacity used by the group.
        /// </summary>
        public int CapacityDense
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.CapacityDense; }
        }

        /// <summary>
        /// Returns a span of packed entity identifiers (<see cref="entlong"/>) – equivalent to a regular span of entity IDs
        /// </summary>
        public EcsLongsSpan Longs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.Longs; }
        }

        /// <summary>
        /// True when the group has been released back to the world's pool and should not be used.
        /// </summary>
        public bool IsReleazed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.IsReleased; }
        }

        /// <summary>
        /// Indexer returning the entity id at the specified dense index.
        /// </summary>
        public int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source[index]; }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a read-only view over the provided <see cref="EcsGroup"/>.
        /// </summary>
        /// <param name="source">Source group to wrap.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup(EcsGroup source)
        {
            _source = source;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Check whether the specified entity id is present in the group.
        /// </summary>
        /// <param name="entityID">Entity identifier to check.</param>
        /// <returns>True when the entity is contained; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) { return _source.Has(entityID); }

        /// <summary>
        /// Get the dense index of the specified entity in the group.
        /// </summary>
        /// <param name="entityID">Entity identifier to locate.</param>
        /// <returns>Dense index of the entity or -1 when not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int entityID) { return _source.IndexOf(entityID); }

        /// <summary>
        /// Copy the group's entity ids into the destination array starting at the specified index.
        /// </summary>
        /// <param name="array">Destination array to copy entity ids into.</param>
        /// <param name="arrayIndex">Starting index in the destination array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int[] array, int arrayIndex) { _source.CopyTo(array, arrayIndex); }

        /// <summary>
        /// Create a pooled mutable clone of this group. The clone is obtained from the world's group pool.
        /// </summary>
        /// <returns>A new <see cref="EcsGroup"/> containing the same entities.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup Clone() { return _source.Clone(); }

        /// <summary>
        /// Return a span representing a slice of the group's dense array starting at start.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start) { return _source.Slice(start); }

        /// <summary>
        /// Return a span representing a slice of the group's dense array with specified length.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start, int length) { return _source.Slice(start, length); }

        /// <summary>
        /// Return a span containing all entity ids in the group.
        /// </summary>
        /// <returns>Span of all entity ids.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ToSpan() { return _source.ToSpan(); }

        /// <summary>
        /// Convert the group contents to a managed int[] array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] ToArray() { return _source.ToArray(); }

        /// <summary>
        /// Copy group contents into a reusable buffer and return the written length.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToArray(ref int[] dynamicBuffer) { return _source.ToArray(ref dynamicBuffer); }

        /// <summary>
        /// Add all entity ids of the group into the provided collection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToCollection(ICollection<int> collection) { _source.ToCollection(collection); }

        /// <summary>
        /// Get a value-type enumerator for iterating over entity ids in the group.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup.Enumerator GetEnumerator() { return _source.GetEnumerator(); }

        /// <summary>
        /// Return the first entity id in the group.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int First() { return _source.First(); }

        /// <summary>
        /// Returns a lightweight span view of the group's entity IDs as an <see cref="EcsSpan"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Last() { return _source.Last(); }

        /// <summary>
        /// Determines whether this group contains exactly the same entity IDs as the specified collection.
        /// </summary>
        /// <param name="group">The collection to compare.</param>
        /// <returns>True if both sets are equal; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsGroup group) { return _source.SetEquals(group); }

        /// <summary>
        /// Determines whether this group contains exactly the same entity IDs as the specified read-only group.
        /// </summary>
        /// <param name="group">The collection to compare.</param>
        /// <returns>True if both sets are equal; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsReadonlyGroup group) { return _source.SetEquals(group._source); }

        /// <summary>
        /// Determines whether this group contains exactly the same entity IDs as the specified span.
        /// </summary>
        /// <param name="span">The span to compare.</param>
        /// <returns>True if both sets are equal; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsSpan span) { return _source.SetEquals(span); }

        /// <summary>
        /// Determines whether this group contains exactly the same entity IDs as the specified enumerable collection.
        /// </summary>
        /// <param name="other">The collection to compare.</param>
        /// <returns>True if both sets are equal; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(IEnumerable<int> other) { return _source.SetEquals(other); }

        /// <summary>Determines whether this group and the specified collection share at least one common entity ID.</summary>
        /// <param name="group">The collection to check for intersection.</param>
        /// <returns>True if there is any overlapping element; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsGroup group) { return _source.Overlaps(group); }

        /// <summary>
        /// Determines whether this group and the specified read-only group share at least one common entity ID.
        /// </summary>
        /// <param name="group">The collection to check for intersection.</param>
        /// <returns>True if there is any overlapping element; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsReadonlyGroup group) { return _source.Overlaps(group._source); }

        /// <summary>
        /// Determines whether this group and the specified span share at least one common entity ID.
        /// </summary>
        /// <param name="span">The span to check for intersection.</param>
        /// <returns>True if there is any overlapping element; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsSpan span) { return _source.Overlaps(span); }

        /// <summary>
        /// Determines whether this group and the specified enumerable collection share at least one common entity ID.
        /// </summary>
        /// <param name="other">The collection to check for intersection.</param>
        /// <returns>True if there is any overlapping element; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(IEnumerable<int> other) { return _source.Overlaps(other); }

        /// <summary>
        /// Determines whether all entity IDs from this group are also present in the specified collection.
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a subset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(EcsGroup group) { return _source.IsSubsetOf(group); }

        /// <summary>
        /// Determines whether all entity IDs from this group are also present in the specified read-only group.
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a subset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(EcsReadonlyGroup group) { return _source.IsSubsetOf(group._source); }

        /// <summary>
        /// Determines whether all entity IDs from this group are also present in the specified span.
        /// </summary>
        /// <param name="span">The span to compare against.</param>
        /// <returns>True if this group is a subset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(EcsSpan span) { return _source.IsSubsetOf(span); }

        /// <summary>
        /// Determines whether all entity IDs from this group are also present in the specified enumerable collection.
        /// </summary>
        /// <param name="other">The collection to compare against.</param>
        /// <returns>True if this group is a subset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(IEnumerable<int> other) { return _source.IsSubsetOf(other); }

        /// <summary>
        /// Determines whether this group is a proper subset of the specified collection (i.e., all elements are present and the sets are not equal).
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a proper subset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSubsetOf(EcsGroup group) { return _source.IsProperSubsetOf(group); }

        /// <summary>
        /// Determines whether this group is a proper subset of the specified read-only group.
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a proper subset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSubsetOf(EcsReadonlyGroup group) { return _source.IsProperSubsetOf(group._source); }

        /// <summary>
        /// Determines whether this group is a proper subset of the specified span.
        /// </summary>
        /// <param name="span">The span to compare against.</param>
        /// <returns>True if this group is a proper subset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSubsetOf(EcsSpan span) { return _source.IsProperSubsetOf(span); }

        /// <summary>
        /// Determines whether this group is a proper subset of the specified enumerable collection.
        /// </summary>
        /// <param name="other">The collection to compare against.</param>
        /// <returns>True if this group is a proper subset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSubsetOf(IEnumerable<int> other) { return _source.IsProperSubsetOf(other); }

        /// <summary>
        /// Determines whether this group contains all entity IDs from the specified collection.
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a superset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(EcsGroup group) { return _source.IsSupersetOf(group); }

        /// <summary>
        /// Determines whether this group contains all entity IDs from the specified read-only group.
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a superset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(EcsReadonlyGroup group) { return _source.IsSupersetOf(group._source); }

        /// <summary>
        /// Determines whether this group contains all entity IDs from the specified span.
        /// </summary>
        /// <param name="span">The span to compare against.</param>
        /// <returns>True if this group is a superset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(EcsSpan span) { return _source.IsSupersetOf(span); }

        /// <summary>
        /// Determines whether this group contains all entity IDs from the specified enumerable collection.
        /// </summary>
        /// <param name="other">The collection to compare against.</param>
        /// <returns>True if this group is a superset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(IEnumerable<int> other) { return _source.IsSupersetOf(other); }

        /// <summary>
        /// Determines whether this group is a proper superset of the specified collection (i.e., contains all elements and the sets are not equal).
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a proper superset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSupersetOf(EcsGroup group) { return _source.IsProperSupersetOf(group); }

        /// <summary>
        /// Determines whether this group is a proper superset of the specified read-only group.
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a proper superset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSupersetOf(EcsReadonlyGroup group) { return _source.IsProperSupersetOf(group._source); }

        /// <summary>
        /// Determines whether this group is a proper superset of the specified span.
        /// </summary>
        /// <param name="span">The span to compare against.</param>
        /// <returns>True if this group is a proper superset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSupersetOf(EcsSpan span) { return _source.IsProperSupersetOf(span); }

        /// <summary>
        /// Determines whether this group is a proper superset of the specified enumerable collection.
        /// </summary>
        /// <param name="other">The collection to compare against.</param>
        /// <returns>True if this group is a proper superset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSupersetOf(IEnumerable<int> other) { return _source.IsProperSupersetOf(other); }
        #endregion

        #region Internal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsGroup GetSource_Internal() { return _source; }
        #endregion

        #region Other
        /// <summary>
        /// Convert the group's contents to a diagnostic string.
        /// </summary>
        /// <returns>Human-readable representation of the group's entities.</returns>
        public override string ToString()
        {
            return _source != null ? _source.ToString() : "NULL";
        }
#pragma warning disable CS0809 // Устаревший член переопределяет неустаревший член
        [Obsolete("Equals() on EcsGroup will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw new NotSupportedException(); }
        [Obsolete("GetHashCode() on EcsGroup will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() { throw new NotSupportedException(); }
#pragma warning restore CS0809 // Устаревший член переопределяет неустаревший член
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsSpan(EcsReadonlyGroup a) { return a.ToSpan(); }
        #endregion
    }

    public unsafe partial class EcsWorld
    {
        private List<WeakReference<EcsGroup>> _groups = new List<WeakReference<EcsGroup>>();
        private Stack<EcsGroup> _groupsPool = new Stack<EcsGroup>(64);

        private MemoryAllocator.Handler[] _groupSparsePagePool = new MemoryAllocator.Handler[64];
        private int _groupSparsePagePoolCount = 0;

        #region Pages
        internal int* TakePage()
        {
            if (_groupSparsePagePoolCount <= 0)
            {
                return MemoryAllocator.AllocAndInit<int>(EcsGroup.PAGE_SIZE).Ptr;
            }
            var takedPage = _groupSparsePagePool[--_groupSparsePagePoolCount];
            _groupSparsePagePool[_groupSparsePagePoolCount] = MemoryAllocator.Handler.Empty;
            return takedPage.As<int>();
        }
        internal void ReturnPage(int* page)
        {
#if DEBUG && DRAGONECS_DEEP_DEBUG
            var h = MemoryAllocator.Handler.FromDataPtr(page);
            if (h.GetID_Debug() == 0 || page == null)
            {
                Throw.DeepDebugException();
            }
#endif

            if (_groupSparsePagePoolCount >= _groupSparsePagePool.Length)
            {
                var old = _groupSparsePagePool;
                _groupSparsePagePool = new MemoryAllocator.Handler[_groupSparsePagePoolCount << 1];
                for (int j = 0; j < old.Length; j++)
                {
                    _groupSparsePagePool[j] = old[j];
                }
            }
            _groupSparsePagePool[_groupSparsePagePoolCount++] = MemoryAllocator.Handler.FromDataPtr(page);
        }
        private void DisposeGroups()
        {
            for (int i = 0; i < _groupSparsePagePoolCount; i++)
            {
                ref var page = ref _groupSparsePagePool[i];
                if (page.IsCreated)
                {
                    MemoryAllocator.FreeAndClear(ref page);
                }
            }
            _groupSparsePagePoolCount = 0;
        }
        #endregion

        #region Groups Pool
        private void RemoveGroupAt(int index)
        {
            int last = _groups.Count - 1;
            _groups[index] = _groups[last];
            _groups.RemoveAt(last);
        }
        internal void RegisterGroup(EcsGroup group)
        {
            _groups.Add(new WeakReference<EcsGroup>(group));
        }
        internal EcsGroup GetFreeGroup()
        {
            EcsGroup result = _groupsPool.Count <= 0 ? new EcsGroup(this, _configs.GetWorldConfigOrDefault().GroupCapacity) : _groupsPool.Pop();
            result._isReleased = false;
            return result;
        }
        internal void ReleaseGroup(EcsGroup group)
        {
#if DEBUG
            if (group.World != this) { Throw.World_GroupDoesNotBelongWorld(); }
#elif DRAGONECS_STABILITY_MODE
            if (group.World != this)
            {
                if (TryGetWorld(group.WorldID, out EcsWorld sourceWorld))
                {
                    group.World.ReleaseGroup(group);
                }
            }
#endif
            group._isReleased = true;
            group.Clear();
            _groupsPool.Push(group);
        }
        #endregion
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public unsafe class EcsGroup : IDisposable, IEnumerable<int>, ISet<int>, IEntityStorage
    {
        internal const int PAGE_SIZE = PageSlot.SIZE;
        private EcsWorld _source;
        private int[] _dense; // 0 индекс для нулевой записи
        private PageSlot* _sparsePages; //Старший бит занят временной маркировкой в операциях над множествами
        private MemoryAllocator.Handler _sparsePagesHandler; //Старший бит занят временной маркировкой в операциях над множествами
        private int _sparsePagesCount;
        private int _totalCapacity;
        private int _count = 0;
        internal bool _isReleased = true;

        internal static readonly int* _nullPage = MemoryAllocator.AllocAndInit<int>(PageSlot.SIZE).Ptr;
        internal static readonly long _nullPagePtrFake = (long)_nullPage;

        #region Properties
        /// <summary>
        /// Identifier of the world that owns the group.
        /// </summary>
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.ID; }
        }

        /// <summary>
        /// The <see cref="EcsWorld"/> instance that owns the group.
        /// </summary>
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source; }
        }

        /// <summary>
        /// Number of entities currently contained in the group.
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count; }
        }

        /// <summary>
        /// Current dense-array capacity used by the group.
        /// </summary>
        public int CapacityDense
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _dense.Length; }
        }

        /// <summary>
        /// Gets a read-only view of this <see cref="EcsGroup"/> instance.
        /// Provides safe, non‑mutating access to the group's contents.
        /// </summary>
        public EcsReadonlyGroup Readonly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new EcsReadonlyGroup(this); }
        }

        /// <summary>
        /// Returns a span of packed entity identifiers (<see cref="entlong"/>) – equivalent to a regular span of entity IDs
        /// </summary>
        public EcsLongsSpan Longs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new EcsLongsSpan(this); }
        }

        /// <summary>
        /// True when the group has been released back to the world's pool and should not be used.
        /// </summary>
        public bool IsReleased
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isReleased; }
        }
        bool ICollection<int>.IsReadOnly { get { return false; } }

        /// <summary>
        /// Indexer returning the entity id at the specified dense index.
        /// </summary>
        public int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if DEBUG
                if (index < 0 || index >= Count) { Throw.ArgumentOutOfRange(); }
#elif DRAGONECS_STABILITY_MODE
                if (index < 0 || index >= Count) { return EcsConsts.NULL_ENTITY_ID; }
#endif
                return _dense[++index];
            }
        }
        #endregion

        #region Constrcutors/Dispose
        /// <summary>
        /// Rent or create a new group associated with the specified world.
        /// The returned group is taken from the world's internal pool and must be released via Dispose().
        /// </summary>
        /// <param name="world">World that will own the created group.</param>
        /// <returns>New or pooled <see cref="EcsGroup"/> instance bound to the given world.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsGroup New(EcsWorld world)
        {
            return world.GetFreeGroup();
        }
        internal EcsGroup(EcsWorld world, int denseCapacity)
        {
            _source = world;
            _source.RegisterGroup(this);
            _dense = new int[denseCapacity];
            _totalCapacity = world.Capacity;
            _sparsePagesCount = CalcSparseSize(_totalCapacity);
            _sparsePagesHandler = MemoryAllocator.Alloc<PageSlot>(_sparsePagesCount);
            _sparsePages = _sparsePagesHandler.As<PageSlot>();
            for (int i = 0; i < _sparsePagesCount; i++)
            {
                _sparsePages[i] = PageSlot.Empty;
            }
        }
        ~EcsGroup()
        {
            lock (this)
            {
                for (int i = 0; i < _sparsePagesCount; i++)
                {
                    ref PageSlot page = ref _sparsePages[i];
                    if (page.Indexes != _nullPage)
                    {
                        MemoryAllocator.Free(page.Indexes);
                        page = default;
                        page.Indexes = _nullPage;
                    }
                    page.IndexesXOR = 0;
                    page.Count = 0;
                }
                _sparsePagesHandler.DisposeAndReset();
            }
        }
        public void Dispose()
        {
            // anchor: no-op placeholder for upcoming automated edits
            _source.ReleaseGroup(this);
        }
        #endregion

        #region Has/IndexOf
        /// <summary>
        /// Check whether the specified entity id is present in the group.
        /// </summary>
        /// <param name="entityID">Entity identifier to check.</param>
        /// <returns>True when the entity is contained; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            ref PageSlot page = ref _sparsePages[entityID >> PageSlot.SHIFT];
            return page.Count == 1 ? _dense[page.IndexesXOR] == entityID : page.Indexes[entityID & PageSlot.MASK] != 0;
        }

        /// <summary>
        /// Get the dense index of the specified entity in the group.
        /// </summary>
        /// <param name="entityID">Entity identifier to locate.</param>
        /// <returns>Dense index of the entity or -1 when not found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int entityID)
        {
            ref PageSlot page = ref _sparsePages[entityID >> PageSlot.SHIFT];
            return page.Count == 1 ? page.IndexesXOR : page.Indexes[entityID & PageSlot.MASK];
        }
        #endregion

        #region Add/Remove
        public void AddUnchecked(int entityID)
        {
#if DEBUG
            if (Has(entityID)) { Throw.Group_AlreadyContains(entityID); }
#elif DRAGONECS_STABILITY_MODE
            if (Has(entityID)) { return; }
#endif
            Add_Internal(entityID);
        }
        public bool Add(int entityID)
        {
            if (Has(entityID))
            {
                return false;
            }
            Add_Internal(entityID);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Add_Internal(int entityID)
        {
            if (++_count >= _dense.Length)
            {
                Array.Resize(ref _dense, ArrayUtility.NextPow2(_count));
            }
            _dense[_count] = entityID;

            ref PageSlot page = ref _sparsePages[entityID >> PageSlot.SHIFT];
            page.Count++;
            // page.Count != 0
            if (page.Count == 1)
            {
                page.IndexesXOR = _count;
            }
            else // page.Count > 1
            {
                if (page.Count == 2)
                {
                    page.Indexes = _source.TakePage();
                    page.Indexes[_dense[page.IndexesXOR] & PageSlot.MASK] = page.IndexesXOR;
                }
                page.IndexesXOR ^= _count;
                page.Indexes[entityID & PageSlot.MASK] = _count;
            }
        }

        public void RemoveUnchecked(int entityID)
        {
#if DEBUG
            if (Has(entityID) == false) { Throw.Group_DoesNotContain(entityID); }
#elif DRAGONECS_STABILITY_MODE
            if (Has(entityID) == false) { return; }
#endif
            Remove_Internal(entityID);
        }
        public bool Remove(int entityID)
        {
            if (Has(entityID) == false)
            {
                return false;
            }
            Remove_Internal(entityID);
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ChangeIndexInSparse(int entityID, int index)
        {
            ref PageSlot page = ref _sparsePages[entityID >> PageSlot.SHIFT];
#if DEBUG && DRAGONECS_DEEP_DEBUG
            if (page.Count == 0) { Throw.DeepDebugException(); }
#endif
            if (page.Count == 1)
            {
                page.IndexesXOR = index;
            }
            else
            {
                int localEntityID = entityID & PageSlot.MASK;
#if DEBUG && DRAGONECS_DEEP_DEBUG
                if (page.Indexes[localEntityID] == 0) { Throw.DeepDebugException(); }
#endif
                page.Indexes[localEntityID] = index;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Remove_Internal(int entityID)
        {
            ref PageSlot page = ref _sparsePages[entityID >> PageSlot.SHIFT];
            int localEntityID = entityID & PageSlot.MASK;

            if (--page.Count == 0)
            {
                _dense[page.IndexesXOR] = _dense[_count]; //_dense[_sparse[entityID]] = _dense[_count];
                ChangeIndexInSparse(_dense[_count], page.IndexesXOR); //_sparse[_dense[_count--]] = _sparse[entityID];
                page.IndexesXOR = 0; //_sparse[entityID] = 0;
            }
            else
            {
                _dense[page.Indexes[localEntityID]] = _dense[_count]; //_dense[_sparse[entityID]] = _dense[_count];
                ChangeIndexInSparse(_dense[_count], page.Indexes[localEntityID]); //_sparse[_dense[_count--]] = _sparse[entityID];
                page.IndexesXOR ^= page.Indexes[localEntityID];
                page.Indexes[localEntityID] = 0; //_sparse[entityID] = 0; 
                if (page.Count == 1)
                {
                    _source.ReturnPage(page.Indexes);
                    page.Indexes = _nullPage;
                }
            }
            _count--;
        }

        /// <summary>
        /// Remove entity ids from the group that are no longer marked as used in the owning world.
        /// Iterates the group's contents and removes any entity that the world reports as unused.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveUnusedEntityIDs()
        {
            foreach (var entityID in this)
            {
                if (_source.IsUsed(entityID) == false)
                {
                    Remove_Internal(entityID);
                }
            }
        }
        #endregion

        #region Clear
        /// <summary>
        /// Remove all entities from the group.
        /// If the owning world is not destroyed this will also return sparse pages to the world's page pool.
        /// </summary>
        public void Clear()
        {
            if (_count == 0) { return; }

            if (_source.IsDestroyed == false)
            {
                for (int i = 0; i < _sparsePagesCount; i++)
                {
                    ref PageSlot page = ref _sparsePages[i];
                    if (page.Indexes != _nullPage)
                    {
                        for (int j = 0; j < PageSlot.SIZE; j++)
                        {
                            page.Indexes[j] = 0;
                        }
                        _source.ReturnPage(page.Indexes);
                        page.Indexes = _nullPage;
                    }
                    page.IndexesXOR = 0;
                    page.Count = 0;
                }
                _count = 0;
            }
        }
        #endregion

        #region Upsize
        /// <summary>
        /// Ensure the internal dense array has capacity for at least minSize elements.
        /// </summary>
        /// <param name="minSize">Minimum required capacity.</param>
        public void Upsize(int minSize)
        {
            if (minSize >= _dense.Length)
            {
                Array.Resize(ref _dense, ArrayUtility.CeilPow2_ClampOverflow(minSize));
            }
        }

        #endregion

        #region CopyFrom/Clone/Slice/ToSpan/ToArray
        /// <summary>
        /// Replace this group's contents with the contents of the provided group.
        /// Existing items will be cleared and the source group's entities copied in order.
        /// </summary>
        /// <param name="group">Source group to copy from.</param>
        public void CopyFrom(EcsGroup group)
        {
#if DEBUG
            if (group.World != _source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (group.World != _source)
            {
                Clear();
                return;
            }
#endif
            Clear();
            foreach (var entityID in group)
            {
                Add_Internal(entityID);
            }
        }

        /// <summary>
        /// Replace this group's contents with the contents of a read-only group view.
        /// </summary>
        /// <param name="group">Read-only group to copy from.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(EcsReadonlyGroup group)
        {
            CopyFrom(group.GetSource_Internal());
        }

        /// <summary>
        /// Replace this group's contents with the entity ids from the provided span.
        /// </summary>
        /// <param name="span">Source span containing entity ids.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(EcsSpan span)
        {
            if (_count > 0)
            {
                Clear();
            }
            for (int i = 0; i < span.Count; i++)
            {
                Add_Internal(span[i]);
            }
        }

        /// <summary>
        /// Copy the group's entity ids into the destination array starting at the specified index.
        /// </summary>
        /// <param name="array">Destination array to copy entity ids into.</param>
        /// <param name="arrayIndex">Starting index in the destination array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int[] array, int arrayIndex)
        {
            foreach (var item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        /// <summary>
        /// Create a pooled mutable clone of this group. The clone is obtained from the world's group pool.
        /// </summary>
        /// <returns>A new <see cref="EcsGroup"/> containing the same entities.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup Clone()
        {
            EcsGroup result = _source.GetFreeGroup();
            result.CopyFrom(this);
            return result;
        }

        /// <summary>
        /// Return a span view starting at the given index to the end of the group.
        /// </summary>
        /// <param name="start">Start index in dense ordering (0-based).</param>
        /// <returns>Slice span of entity ids.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start)
        {
            return Slice(start, _count - start);
        }

        /// <summary>
        /// Return a span view of the group's entities starting at start with specified length.
        /// </summary>
        /// <param name="start">Start index in dense ordering (0-based).</param>
        /// <param name="length">Number of elements in the slice.</param>
        /// <returns>Span of entity ids.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start, int length)
        {
#if DEBUG
            if (start < 0 || start + length > _count) { Throw.ArgumentOutOfRange(); }
#elif DRAGONECS_STABILITY_MODE
            if (start < 0) { start = 0; }
            if (start + length > _count) { length = _count - start; }
#endif
            return new EcsSpan(WorldID, _dense, start + 1, length);
        }

        /// <summary>
        /// Return a span containing all entity ids in this group.
        /// </summary>
        /// <returns>Span of all entity ids.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ToSpan()
        {
            return new EcsSpan(WorldID, _dense, 1, _count);
        }

        /// <summary>
        /// Convert the group contents to a managed int array.
        /// </summary>
        /// <returns>Array containing the group's entity ids in dense order.</returns>
        public int[] ToArray()
        {
            int[] result = new int[_count];
            Array.Copy(_dense, 1, result, 0, _count);
            return result;
        }

        /// <summary>
        /// Copy the group's entity ids into a reusable buffer and return the number of elements written.
        /// </summary>
        /// <param name="dynamicBuffer">Buffer to receive the entity ids. Will be resized if too small.</param>
        /// <returns>Number of entity ids written into the buffer.</returns>
        public int ToArray(ref int[] dynamicBuffer)
        {
            if (dynamicBuffer.Length < _count)
            {
                Array.Resize(ref dynamicBuffer, ArrayUtility.CeilPow2(_count));
            }
            int i = 0;
            foreach (var e in this)
            {
                dynamicBuffer[i++] = e;
            }
            return i;
        }

        /// <summary>
        /// Add all entity ids of the group into the provided collection.
        /// </summary>
        /// <param name="collection">Collection to populate with entity ids.</param>
        public void ToCollection(ICollection<int> collection)
        {
            foreach (var e in this)
            {
                collection.Add(e);
            }
        }
        #endregion

        #region Set operations

        #region UnionWith
        /// <summary>
        /// Add all elements from the specified group into this group (set union).
        /// </summary>
        /// <param name="group">Group whose elements to add.</param>
        public void UnionWith(EcsGroup group)
        {
#if DEBUG
            if (_source != group._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (_source != group._source) { return; }
#endif
            foreach (var entityID in group) { UnionWithStep(entityID); }
        }

        /// <summary>
        /// Add all elements from the specified read-only group into this group (set union).
        /// </summary>
        /// <param name="group">Read-only group whose elements to add.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnionWith(EcsReadonlyGroup group) { UnionWith(group.GetSource_Internal()); }

        /// <summary>
        /// Add all entity ids from the provided span into this group (set union).
        /// </summary>
        /// <param name="span">Span containing entity ids to add.</param>
        public void UnionWith(EcsSpan span)
        {
#if DEBUG
            if (WorldID != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (WorldID != span.WorldID) { return; }
#endif
            foreach (var entityID in span) { UnionWithStep(entityID); }
        }

        /// <summary>
        /// Add all elements from the enumerable into this group (set union).
        /// </summary>
        /// <param name="other">Enumerable containing entity ids to add.</param>
        public void UnionWith(IEnumerable<int> other)
        {
            foreach (var entityID in other) { UnionWithStep(entityID); }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UnionWithStep(int entityID)
        {
            if (Has(entityID) == false)
            {
                Add_Internal(entityID);
            }
        }
        #endregion

        #region ExceptWith
        /// <summary>
        /// Remove all elements contained in the specified group from this group (set difference).
        /// </summary>
        /// <param name="group">Group whose elements should be removed from this group.</param>
        public void ExceptWith(EcsGroup group)
        {
#if DEBUG
            if (_source != group._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (_source != group._source) { return; }
#endif
            if (group.Count > Count) //мини оптимизация, итеррируемся по короткому списку
            {
                for (int i = _count; i > 0; i--)//итерация в обратном порядке исключает ошибки при удалении элементов
                {
                    int entityID = _dense[i];
                    if (group.Has(entityID))
                    {
                        Remove_Internal(entityID);
                    }
                }
            }
            else
            {
                foreach (var entityID in group) { ExceptWithStep_Internal(entityID); }
            }
        }

        /// <summary>
        /// Remove all elements contained in the specified read-only group from this group (set difference).
        /// </summary>
        /// <param name="group">Read-only group whose elements should be removed from this group.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExceptWith(EcsReadonlyGroup group) { ExceptWith(group.GetSource_Internal()); }

        /// <summary>
        /// Remove all entity ids from the provided span from this group (set difference).
        /// </summary>
        /// <param name="span">Span containing entity ids to remove.</param>
        public void ExceptWith(EcsSpan span)
        {
#if DEBUG
            if (WorldID != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (WorldID != span.WorldID) { return; }
#endif
            foreach (var entityID in span) { ExceptWithStep_Internal(entityID); }
        }

        /// <summary>
        /// Remove all elements found in the enumerable from this group (set difference).
        /// </summary>
        /// <param name="other">Enumerable containing entity ids to remove.</param>
        public void ExceptWith(IEnumerable<int> other)
        {
            foreach (var entityID in other) { ExceptWithStep_Internal(entityID); }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExceptWithStep_Internal(int entityID)
        {
            if (Has(entityID))
            {
                Remove_Internal(entityID);
            }
        }
        #endregion

        #region IntersectWith
        /// <summary>
        /// Keep only elements that are also present in the specified group (set intersection).
        /// </summary>
        /// <param name="group">Group to intersect with.</param>
        public void IntersectWith(EcsGroup group)
        {
#if DEBUG
            if (_source != group._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (_source != group._source) { return; }
#endif
            for (int i = _count; i > 0; i--)//итерация в обратном порядке исключает ошибки при удалении элементов
            {
                int entityID = _dense[i];
                if (group.Has(entityID) == false)
                {
                    Remove_Internal(entityID);
                }
            }
        }

        /// <summary>
        /// Keep only elements that are also present in the specified read-only group (set intersection).
        /// </summary>
        /// <param name="group">Read-only group to intersect with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IntersectWith(EcsReadonlyGroup group) { IntersectWith(group.GetSource_Internal()); }
       
        /// <summary>
        /// Keep only entity ids that are also present in the provided span (set intersection).
        /// </summary>
        /// <param name="span">Span to intersect with.</param>
        public void IntersectWith(EcsSpan span)
        {
#if DEBUG
            if (WorldID != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (WorldID != span.WorldID) { return; }
#endif
            foreach (var entityID in span)
            {
                if (Has(entityID))
                {
                    MarkEntity_Internal(entityID);
                }
            }
            ClearUnmarked_Internal();
        }

        /// <summary>
        /// Keep only elements that are also present in the provided enumerable (set intersection).
        /// </summary>
        /// <param name="other">Enumerable to intersect with.</param>
        public void IntersectWith(IEnumerable<int> other)
        {
            if (other is ISet<int> set)
            {
                for (int i = _count; i > 0; i--)//итерация в обратном порядке исключает ошибки при удалении элементов
                {
                    int entityID = _dense[i];
                    if (set.Contains(entityID) == false)
                    {
                        Remove_Internal(entityID);
                    }
                }
            }
            else
            {
                foreach (var entityID in other)
                {
                    if (Has(entityID))
                    {
                        MarkEntity_Internal(entityID);
                    }
                }
                ClearUnmarked_Internal();
            }
        }
        #endregion

        #region SymmetricExceptWith
        /// <summary>
        /// Perform symmetric difference with the specified group: elements present in either set but not in both. (set symmetric difference)
        /// </summary>
        /// <param name="group">Group to symmetric-except with.</param>
        public void SymmetricExceptWith(EcsGroup group)
        {
#if DEBUG
            if (_source != group._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (_source != group._source) { return; }
#endif
            foreach (var entityID in group) { SymmetricExceptWithStep_Internal(entityID); }
        }

        /// <summary>
        /// Perform symmetric difference with the specified read-only group: elements present in either set but not in both. (set symmetric difference)
        /// </summary>
        /// <param name="group">Read-only group to symmetric-except with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SymmetricExceptWith(EcsReadonlyGroup group) { SymmetricExceptWith(group.GetSource_Internal()); }

        /// <summary>
        /// Perform symmetric difference with the provided span: elements present in either set but not in both. (set symmetric difference)
        /// </summary>
        /// <param name="span">Span to symmetric-except with.</param>
        public void SymmetricExceptWith(EcsSpan span)
        {
#if DEBUG
            if (WorldID != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (WorldID != span.WorldID) { return; }
#endif
            foreach (var entityID in span) { SymmetricExceptWithStep_Internal(entityID); }
        }

        /// <summary>
        /// Perform symmetric difference with the provided enumerable: elements present in either set but not in both. (set symmetric difference)
        /// </summary>
        /// <param name="other">Enumerable to symmetric-except with.</param>
        public void SymmetricExceptWith(IEnumerable<int> other)
        {
            foreach (var entityID in other) { SymmetricExceptWithStep_Internal(entityID); }
        }
        private void SymmetricExceptWithStep_Internal(int entityID)
        {
            if (Has(entityID))
            {
                Remove_Internal(entityID);
            }
            else
            {
                Add_Internal(entityID);
            }
        }
        #endregion

        #region Inverse
        /// <summary>
        /// Replace this group with the inverse set relative to the world's entities: entities present in the world but not in this group.
        /// </summary>
        public void Inverse()
        {
            if (_count == 0)
            {
                foreach (var entityID in _source.Entities)
                {
                    Add_Internal(entityID);
                }
                return;
            }
            foreach (var entityID in _source.Entities)
            {
                if (Has(entityID))
                {
                    Remove_Internal(entityID);
                }
                else
                {
                    Add_Internal(entityID);
                }
            }
        }
        #endregion

        #region SetEquals
        /// <summary>
        /// Determines whether this group contains exactly the same entity ids as the specified group.
        /// </summary>
        /// <param name="group">Group to compare with.</param>
        /// <returns>True if both sets contain the same entity ids; otherwise false.</returns>
        public bool SetEquals(EcsGroup group)
        {
#if DEBUG
            if (_source != group._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (_source != group._source) { return false; }
#endif
            if (group.Count != Count) { return false; }
            foreach (var entityID in group)
            {
                if (Has(entityID) == false)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether this group contains exactly the same entity ids as the specified read-only group.
        /// </summary>
        /// <param name="group">Read-only group to compare with.</param>
        /// <returns>True if both sets contain the same entity ids; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsReadonlyGroup group) { return SetEquals(group.GetSource_Internal()); }
        
        /// <summary>
        /// Determines whether this group contains exactly the same entity ids as the specified span.
        /// </summary>
        /// <param name="span">Span to compare with.</param>
        /// <returns>True if both sets contain the same entity ids; otherwise false.</returns>
        public bool SetEquals(EcsSpan span)
        {
#if DEBUG
            if (WorldID != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (WorldID != span.WorldID) { return false; }
#endif
            if (span.Count != Count) { return false; }
            foreach (var entityID in span)
            {
                if (Has(entityID) == false)
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Determines whether this group contains exactly the same entity ids as the specified enumerable.
        /// </summary>
        /// <param name="other">Enumerable to compare with.</param>
        /// <returns>True if both sets contain the same entity ids; otherwise false.</returns>
        public bool SetEquals(IEnumerable<int> other)
        {
            if (other is ICollection collection && collection.Count != Count) { return false; }
            foreach (var entityID in other)
            {
                if (Has(entityID) == false)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region Overlaps
        /// <summary>
        /// Determines whether this group and the specified group share at least one common entity id.
        /// </summary>
        /// <param name="group">Group to check for intersection.</param>
        /// <returns>True if there is any overlapping element; otherwise false.</returns>
        public bool Overlaps(EcsGroup group)
        {
#if DEBUG
            if (_source != group._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (_source != group._source) { return false; }
#endif
            if (group.Count > Count)
            {
                foreach (var entityID in this)
                {
                    if (group.Has(entityID))
                    {
                        return true;
                    }
                }
            }
            else
            {
                foreach (var entityID in group)
                {
                    if (Has(entityID))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether this group and the specified read-only group share at least one common entity id.
        /// </summary>
        /// <param name="group">Read-only group to check for intersection.</param>
        /// <returns>True if there is any overlapping element; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsReadonlyGroup group) { return Overlaps(group.GetSource_Internal()); }
        
        /// <summary>
        /// Determines whether this group and the specified span share at least one common entity id.
        /// </summary>
        /// <param name="span">Span to check for intersection.</param>
        /// <returns>True if there is any overlapping element; otherwise false.</returns>
        public bool Overlaps(EcsSpan span)
        {
#if DEBUG
            if (WorldID != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (WorldID != span.WorldID) { return false; }
#endif
            foreach (var entityID in span)
            {
                if (Has(entityID))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Determines whether this group and the specified enumerable share at least one common entity id.
        /// </summary>
        /// <param name="other">Enumerable to check for intersection.</param>
        /// <returns>True if there is any overlapping element; otherwise false.</returns>
        public bool Overlaps(IEnumerable<int> other)
        {
            foreach (var entityID in other)
            {
                if (Has(entityID))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region IsSubsetOf/IsProperSubsetOf
        /// <summary>
        /// Determines whether all entity ids from this group are also present in the specified collection.
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a subset; otherwise false.</returns>
        public bool IsSubsetOf(EcsGroup group)
        {
#if DEBUG
            if (_source != group._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (_source != group._source) { return false; }
#endif
            if (Count == 0) { return true; }
            if (group.Count < Count) { return false; }
            return IsSubsetOf_Internal(group);
        }
        
        /// <summary>
        /// Determines whether all entity ids from this group are also present in the specified read-only group.
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a subset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(EcsReadonlyGroup group) { return IsSubsetOf(group.GetSource_Internal()); }
        
        /// <summary>
        /// Determines whether all entity ids from this group are also present in the specified span.
        /// </summary>
        /// <param name="span">The span to compare against.</param>
        /// <returns>True if this group is a subset; otherwise false.</returns>
        public bool IsSubsetOf(EcsSpan span)
        {
#if DEBUG
            if (WorldID != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (WorldID != span.WorldID) { return false; }
#endif
            if (Count == 0) { return true; }
            if (span.Count < Count) { return false; }
            return IsSubsetOf_Internal(span);
        }
        
        /// <summary>
        /// Determines whether all entity ids from this group are also present in the specified enumerable collection.
        /// </summary>
        /// <param name="other">The collection to compare against.</param>
        /// <returns>True if this group is a subset; otherwise false.</returns>
        public bool IsSubsetOf(IEnumerable<int> other)
        {
            if (Count == 0) { return true; }
            if (other is ICollection collection && collection.Count < Count) { return false; }
            return IsSubsetOf_Internal(other);
        }

        // ================================================================================

        /// <summary>
        /// Determines whether this group is a proper subset of the specified collection (i.e., all elements are present and the sets are not equal).
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a proper subset; otherwise false.</returns>
        public bool IsProperSubsetOf(EcsGroup group)
        {
#if DEBUG
            if (_source != group._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (_source != group._source) { return false; }
#endif
            if (Count == 0) { return true; }
            if (group.Count <= Count) { return false; }
            return IsSubsetOf_Internal(group);
        }
        
        /// <summary>
        /// Determines whether this group is a proper subset of the specified read-only group.
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a proper subset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSubsetOf(EcsReadonlyGroup group) { return IsProperSubsetOf(group.GetSource_Internal()); }
        
        /// <summary>
        /// Determines whether this group is a proper subset of the specified span.
        /// </summary>
        /// <param name="span">The span to compare against.</param>
        /// <returns>True if this group is a proper subset; otherwise false.</returns>
        public bool IsProperSubsetOf(EcsSpan span)
        {
#if DEBUG
            if (WorldID != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (WorldID != span.WorldID) { return false; }
#endif
            if (Count == 0) { return true; }
            if (span.Count <= Count) { return false; }
            return IsSubsetOf_Internal(span);
        }
        
        /// <summary>
        /// Determines whether this group is a proper subset of the specified enumerable collection.
        /// </summary>
        /// <param name="other">The collection to compare against.</param>
        /// <returns>True if this group is a proper subset; otherwise false.</returns>
        public bool IsProperSubsetOf(IEnumerable<int> other)
        {
            if (Count == 0) { return true; }
            if (other is ICollection collection && collection.Count <= Count) { return false; }
            return IsSubsetOf_Internal(other);
        }

        // ================================================================================

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsSubsetOf_Internal(EcsGroup group)
        {
            foreach (var entityID in this)
            {
                if (group.Has(entityID) == false)
                {
                    return false;
                }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsSubsetOf_Internal(EcsSpan span)
        {
            int uniqueCount = 0;
            foreach (var entityID in span)
            {
#if DEBUG && DRAGONECS_DEEP_DEBUG
                HashSet<int> thisHS = new HashSet<int>();
                ToCollection(thisHS);
                if (thisHS.Contains(entityID) && Has(entityID) == false) { Throw.DeepDebugException(); }
#endif
                if (Has(entityID))
                {
                    uniqueCount++;
                }
            }
            return uniqueCount == this.Count;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsSubsetOf_Internal(IEnumerable<int> other)
        {
            int uniqueCount = 0;
            foreach (var entityID in other)
            {
                if (Has(entityID))
                {
                    uniqueCount++;
                }
            }
            return uniqueCount == Count;
        }
        #endregion

        #region IsSupersetOf/IsProperSupersetOf
        /// <summary>
        /// Determines whether this group contains all entity ids from the specified collection.
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a superset; otherwise false.</returns>
        public bool IsSupersetOf(EcsGroup group)
        {
#if DEBUG
            if (_source != group._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (_source != group._source) { return false; }
#endif
            if (group.Count > Count) { return false; }
            return IsSupersetOf_Internal(group);
        }
        
        /// <summary>
        /// Determines whether this group contains all entity ids from the specified read-only group.
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a superset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(EcsReadonlyGroup group) { return IsSupersetOf(group.GetSource_Internal()); }
        
        /// <summary>
        /// Determines whether this group contains all entity ids from the specified span.
        /// </summary>
        /// <param name="span">The span to compare against.</param>
        /// <returns>True if this group is a superset; otherwise false.</returns>
        public bool IsSupersetOf(EcsSpan span)
        {
#if DEBUG
            if (WorldID != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (WorldID != span.WorldID) { return false; }
#endif
            if (span.Count > Count) { return false; }
            return IsSupersetOf_Internal(span);
        }
        
        /// <summary>
        /// Determines whether this group contains all entity ids from the specified enumerable collection.
        /// </summary>
        /// <param name="other">The collection to compare against.</param>
        /// <returns>True if this group is a superset; otherwise false.</returns>
        public bool IsSupersetOf(IEnumerable<int> other)
        {
            if (other is ICollection collection && collection.Count > Count) { return false; }
            return IsSupersetOf_Internal(other);
        }

        // ================================================================================

        /// <summary>
        /// Determines whether this group is a proper superset of the specified collection (i.e., contains all elements and the sets are not equal).
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a proper superset; otherwise false.</returns>
        public bool IsProperSupersetOf(EcsGroup group)
        {
#if DEBUG
            if (_source != group._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (_source != group._source) { return false; }
#endif
            if (group.Count >= Count) { return false; }
            return IsSupersetOf_Internal(group);
        }
        
        /// <summary>
        /// Determines whether this group is a proper superset of the specified read-only group.
        /// </summary>
        /// <param name="group">The collection to compare against.</param>
        /// <returns>True if this group is a proper superset; otherwise false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSupersetOf(EcsReadonlyGroup group) { return IsProperSupersetOf(group.GetSource_Internal()); }
        
        /// <summary>
        /// Determines whether this group is a proper superset of the specified span.
        /// </summary>
        /// <param name="span">The span to compare against.</param>
        /// <returns>True if this group is a proper superset; otherwise false.</returns>
        public bool IsProperSupersetOf(EcsSpan span)
        {
#if DEBUG
            if (WorldID != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (WorldID != span.WorldID) { return false; }
#endif
            if (span.Count >= Count) { return false; }
            return IsSupersetOf_Internal(span);
        }
        
        /// <summary>
        /// Determines whether this group is a proper superset of the specified enumerable collection.
        /// </summary>
        /// <param name="other">The collection to compare against.</param>
        /// <returns>True if this group is a proper superset; otherwise false.</returns>
        public bool IsProperSupersetOf(IEnumerable<int> other)
        {
            if (other is ICollection collection && collection.Count >= Count) { return false; }
            return IsSupersetOf_Internal(other);
        }

        // ================================================================================

        private bool IsSupersetOf_Internal(EcsGroup group)
        {
            foreach (var entityID in group)
            {
                if (Has(entityID) == false)
                {
                    return false;
                }
            }
            return true;
        }
        private bool IsSupersetOf_Internal(EcsSpan span)
        {
            foreach (var entityID in span)
            {
                if (Has(entityID) == false)
                {
                    return false;
                }
            }
            return true;
        }
        private bool IsSupersetOf_Internal(IEnumerable<int> other)
        {
            foreach (var entityID in other)
            {
                if (Has(entityID) == false)
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #endregion

        #region Static Set operations

        #region Union
        /// <summary>
        /// Create a new group that contains the union of two groups. The result is obtained from the world's group pool.
        /// </summary>
        /// <param name="a">First input group.</param>
        /// <param name="b">Second input group.</param>
        /// <returns>New pooled group containing elements present in either input group.</returns>
        public static EcsGroup Union(EcsGroup a, EcsGroup b)
        {
#if DEBUG
            if (a._source != b._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (a._source != b._source) { return a.World.GetFreeGroup(); }
#endif
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var entityID in a)
            {
                result.Add_Internal(entityID);
            }
            foreach (var entityID in b)
            {
                result.Add(entityID);
            }
            return result;
        }
        
        /// <summary>
        /// Create a new group that contains the union of two read-only groups. The result is obtained from the world's group pool.
        /// </summary>
        /// <param name="a">First input read-only group.</param>
        /// <param name="b">Second input read-only group.</param>
        /// <returns>New pooled group containing elements present in either input group.</returns>
        public static EcsGroup Union(EcsReadonlyGroup a, EcsReadonlyGroup b)
        {
            return Union(a.GetSource_Internal(), b.GetSource_Internal());
        }
        
        /// <summary>
        /// Create a new group that contains the union of two spans. The result is obtained from the world's group pool.
        /// </summary>
        /// <param name="a">First input span.</param>
        /// <param name="b">Second input span.</param>
        /// <returns>New pooled group containing elements present in either input span.</returns>
        public static EcsGroup Union(EcsSpan a, EcsSpan b)
        {
#if DEBUG
            if (a.WorldID != b.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (a.WorldID != b.WorldID) { return a.World.GetFreeGroup(); }
#endif
            EcsGroup result = a.World.GetFreeGroup();
            foreach (var entityID in a)
            {
                result.Add_Internal(entityID);
            }
            foreach (var entityID in b)
            {
                result.Add(entityID);
            }
            return result;
        }
        #endregion

        #region Except
        /// <summary>
        /// Create a new group that contains elements present in the first group but not in the second (set difference).
        /// </summary>
        /// <param name="a">Source group.</param>
        /// <param name="b">Group with elements to exclude.</param>
        /// <returns>New pooled group containing the difference a \ b.</returns>
        public static EcsGroup Except(EcsGroup a, EcsGroup b)
        {
#if DEBUG
            if (a._source != b._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (a._source != b._source) { return a.World.GetFreeGroup(); }
#endif
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var entityID in a)
            {
                if (b.Has(entityID) == false)
                {
                    result.Add_Internal(entityID);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Create a new group that contains elements present in the span but not in the group.
        /// </summary>
        /// <param name="a">Source span.</param>
        /// <param name="b">Group with elements to exclude.</param>
        /// <returns>New pooled group containing the difference a \ b.</returns>
        public static EcsGroup Except(EcsSpan a, EcsGroup b)
        {
#if DEBUG
            if (a.WorldID != b.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (a.WorldID != b.WorldID) { return a.World.GetFreeGroup(); }
#endif
            EcsGroup result = b._source.GetFreeGroup();
            foreach (var entityID in a)
            {
                if (b.Has(entityID) == false)
                {
                    result.Add_Internal(entityID);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Create a new group that contains elements present in the first span but not in the second.
        /// </summary>
        /// <param name="a">First input span.</param>
        /// <param name="b">Second input span.</param>
        /// <returns>New pooled group containing the difference a \ b.</returns>
        public static EcsGroup Except(EcsSpan a, EcsSpan b)
        {
#if DEBUG
            if (a.WorldID != b.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (a.WorldID != b.WorldID) { return a.World.GetFreeGroup(); }
#endif
            EcsGroup result = a.World.GetFreeGroup();
            result.CopyFrom(a);
            result.ExceptWith(b);
            return result;
        }
        
        /// <summary>
        /// Create a new group that contains elements present in the first read-only group but not in the second.
        /// </summary>
        /// <param name="a">First input read-only group.</param>
        /// <param name="b">Second input read-only group.</param>
        /// <returns>New pooled group containing the difference a \ b.</returns>
        public static EcsGroup Except(EcsReadonlyGroup a, EcsReadonlyGroup b)
        {
            return Except(a.GetSource_Internal(), b.GetSource_Internal());
        }
        #endregion

        #region Intersect
        /// <summary>
        /// Create a new group that contains the intersection of two groups. The result is obtained from the world's group pool.
        /// </summary>
        /// <param name="a">First input group.</param>
        /// <param name="b">Second input group.</param>
        /// <returns>New pooled group containing elements present in both input groups.</returns>
        public static EcsGroup Intersect(EcsGroup a, EcsGroup b)
        {
#if DEBUG
            if (a._source != b._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (a._source != b._source) { return a.World.GetFreeGroup(); }
#endif
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var entityID in a)
            {
                if (b.Has(entityID))
                {
                    result.Add_Internal(entityID);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Create a new group that contains elements present both in the span and in the group.
        /// </summary>
        /// <param name="a">Input span.</param>
        /// <param name="b">Input group.</param>
        /// <returns>New pooled group containing elements present in both inputs.</returns>
        public static EcsGroup Intersect(EcsSpan a, EcsGroup b)
        {
#if DEBUG
            if (a.WorldID != b.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (a.WorldID != b.WorldID) { return a.World.GetFreeGroup(); }
#endif
            EcsGroup result = b._source.GetFreeGroup();
            foreach (var entityID in a)
            {
                if (b.Has(entityID))
                {
                    result.Add_Internal(entityID);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Create a new group that contains elements present both in the group and in the span.
        /// </summary>
        /// <param name="a">Input group.</param>
        /// <param name="b">Input span.</param>
        /// <returns>New pooled group containing elements present in both inputs.</returns>
        public static EcsGroup Intersect(EcsGroup a, EcsSpan b)
        {
            // Operation is symmetric; forward to the span/group overload.
            return Intersect(b, a);
        }
        
        /// <summary>
        /// Create a new group that contains the intersection of two spans.
        /// </summary>
        /// <param name="a">First input span.</param>
        /// <param name="b">Second input span.</param>
        /// <returns>New pooled group containing elements present in both spans.</returns>
        public static EcsGroup Intersect(EcsSpan a, EcsSpan b)
        {
#if DEBUG
            if (a.WorldID != b.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (a.WorldID != b.WorldID) { return a.World.GetFreeGroup(); }
#endif
            EcsGroup result = b.World.GetFreeGroup();
            result.CopyFrom(a);
            result.IntersectWith(b);
            return result;
        }
        
        /// <summary>
        /// Create a new group that contains the intersection of two read-only groups.
        /// </summary>
        /// <param name="a">First input read-only group.</param>
        /// <param name="b">Second input read-only group.</param>
        /// <returns>New pooled group containing elements present in both inputs.</returns>
        public static EcsGroup Intersect(EcsReadonlyGroup a, EcsReadonlyGroup b)
        {
            return Intersect(a.GetSource_Internal(), b.GetSource_Internal());
        }
        #endregion

        #region SymmetricExcept
        /// <summary>
        /// Create a new group that contains the symmetric difference between two groups.
        /// The result contains elements present in either group but not in both.
        /// </summary>
        /// <param name="a">First input group.</param>
        /// <param name="b">Second input group.</param>
        /// <returns>New pooled group containing the symmetric difference.</returns>
        public static EcsGroup SymmetricExcept(EcsGroup a, EcsGroup b)
        {
#if DEBUG
            if (a._source != b._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (a._source != b._source) { return a.World.GetFreeGroup(); }
#endif
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var entityID in a)
            {
                if (b.Has(entityID) == false)
                {
                    result.Add_Internal(entityID);
                }
            }
            foreach (var entityID in b)
            {
                if (a.Has(entityID) == false)
                {
                    result.Add_Internal(entityID);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Create a new group that contains the symmetric difference between two spans.
        /// </summary>
        /// <param name="a">First input span.</param>
        /// <param name="b">Second input span.</param>
        /// <returns>New pooled group containing the symmetric difference.</returns>
        public static EcsGroup SymmetricExcept(EcsSpan a, EcsSpan b)
        {
#if DEBUG
            if (a.WorldID != b.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (a.WorldID != b.WorldID) { return a.World.GetFreeGroup(); }
#endif
            EcsGroup result = a.World.GetFreeGroup();
            result.CopyFrom(a);
            foreach (var entityID in b)
            {
                if (result.Has(entityID))
                {
                    result.MarkEntity_Internal(entityID);
                }
                else
                {
                    result.Add_Internal(entityID);
                }
            }
            result.ClearMarked_Internal();
            return result;
        }
        
        /// <summary>
        /// Create a new group that contains the symmetric difference between two read-only groups.
        /// </summary>
        /// <param name="a">First input read-only group.</param>
        /// <param name="b">Second input read-only group.</param>
        /// <returns>New pooled group containing the symmetric difference.</returns>
        public static EcsGroup SymmetricExcept(EcsReadonlyGroup a, EcsReadonlyGroup b)
        {
            return SymmetricExcept(a.GetSource_Internal(), b.GetSource_Internal());
        }
        #endregion

        #region Inverse
        /// <summary>
        /// Create a new group that contains the inverse of the provided group relative to the world's entities.
        /// The result is obtained from the world's group pool.
        /// </summary>
        /// <param name="a">Input group to invert.</param>
        /// <returns>New pooled group containing entities present in the world but not in the input group.</returns>
        public static EcsGroup Inverse(EcsGroup a)
        {
            EcsGroup result = a._source.GetFreeGroup();
            foreach (var entityID in a._source.Entities)
            {
                if (a.Has(entityID) == false)
                {
                    result.Add_Internal(entityID);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Create a new group that contains the inverse of the provided read-only group relative to the world's entities.
        /// </summary>
        /// <param name="a">Read-only group to invert.</param>
        /// <returns>New pooled group containing entities present in the world but not in the input group.</returns>
        public static EcsGroup Inverse(EcsReadonlyGroup a)
        {
            return Inverse(a.GetSource_Internal());
        }
        
        /// <summary>
        /// Create a new group that contains the inverse of the provided span relative to the world's entities.
        /// </summary>
        /// <param name="a">Span to invert.</param>
        /// <returns>New pooled group containing entities present in the world but not in the input span.</returns>
        public static EcsGroup Inverse(EcsSpan a)
        {
            EcsGroup result = a.World.GetFreeGroup();
            result.CopyFrom(a.World.Entities);
            result.ExceptWith(a);
            return result;
        }
        #endregion

        #endregion

        #region Enumerator
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator() { return new Enumerator(this); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        IEnumerator<int> IEnumerable<int>.GetEnumerator() { return GetEnumerator(); }
        public struct Enumerator : IEnumerator<int>
        {
            private readonly int[] _dense;
            private uint _index;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(EcsGroup group)
            {
                _dense = group._dense;
                //для оптимизации компилятором
                _index = (uint)(group._count > _dense.Length ? _dense.Length : group._count) + 1;
            }
            public int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return _dense[_index]; }
            }
            object IEnumerator.Current { get { return Current; } }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() { return --_index > 0; }  // проверка с учтом что отсчет начинается с индекса 1 
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IDisposable.Dispose() { }
            void IEnumerator.Reset() { throw new NotSupportedException(); }
        }
        #endregion

        #region HiBitMarking
        // Hi-bit marking: use highest bit of int to mark entities during set operations
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void MarkEntity_Internal(int entityID)
        {
            _dense[IndexOf(entityID)] |= int.MinValue;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsMarkIndex_Internal(int index)
        {
            return (_dense[index] & int.MinValue) == int.MinValue;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UnmarkIndex_Internal(int index)
        {
            _dense[index] &= int.MaxValue;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearUnmarked_Internal()
        {
            for (int i = _count; i > 0; i--)//итерация в обратном порядке исключает ошибки при удалении элементов
            {
                int entityID = _dense[i];
                if (IsMarkIndex_Internal(i))
                {
                    UnmarkIndex_Internal(i);
                }
                else
                {
                    Remove_Internal(entityID);
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearMarked_Internal()
        {
            for (int i = _count; i > 0; i--)//итерация в обратном порядке исключает ошибки при удалении элементов
            {
                if (IsMarkIndex_Internal(i))
                {
                    UnmarkIndex_Internal(i); // Unmark_Internal должен быть до Remove_Internal
                    int entityID = _dense[i];
                    Remove_Internal(entityID);
                }
            }
        }
        #endregion

        #region Other
        /// <summary>
        /// Return the first entity id in the group.
        /// </summary>
        /// <returns>First entity id in dense ordering.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int First() { return _dense[1]; }
        
        /// <summary>
        /// Return the last entity id in the group.
        /// </summary>
        /// <returns>Last entity id in dense ordering.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Last() { return _dense[_count]; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnWorldResize_Internal(int newSize)
        {
            //Array.Resize(ref _sparse, newSize);
            _totalCapacity = newSize;
            var oldPagesCount = _sparsePagesCount;
            _sparsePagesCount = CalcSparseSize(_totalCapacity);
            _sparsePagesHandler = MemoryAllocator.Realloc<PageSlot>(_sparsePagesHandler, _sparsePagesCount);
            _sparsePages = _sparsePagesHandler.As<PageSlot>();
            //_sparsePages = UnmanagedArrayUtility.Resize<PageSlot>(_sparsePages, _sparsePagesCount);
            for (int i = oldPagesCount; i < _sparsePagesCount; i++)
            {
                _sparsePages[i] = PageSlot.Empty;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnReleaseDelEntityBuffer_Internal(ReadOnlySpan<int> buffer)
        {
            if (_count <= 0) { return; }
            foreach (var entityID in buffer)
            {
                if (Has(entityID))
                {
                    Remove_Internal(entityID);
                }
            }
        }
        
        /// <summary>
        /// Convert the group's contents to a diagnostic string.
        /// </summary>
        /// <returns>Human-readable representation of the group's entities.</returns>
        public override string ToString()
        {
            return CollectionUtility.EntitiesToString(_dense.Skip(1).Take(_count), "group");
        }
        void ICollection<int>.Add(int item) { Add(item); }
        bool ICollection<int>.Contains(int item) { return Has(item); }

        /// <summary>
        /// Implicitly obtain a read-only view over this group.
        /// </summary>
        /// <param name="a">Source group to convert.</param>
        /// <returns>Read-only view of the group.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsReadonlyGroup(EcsGroup a) { return a.Readonly; }
        
        /// <summary>
        /// Implicitly convert this group into an <see cref="EcsSpan"/> representing its entities.
        /// </summary>
        /// <param name="a">Source group to convert.</param>
        /// <returns>Span view over the group's entities.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsSpan(EcsGroup a) { return a.ToSpan(); }
        internal class DebuggerProxy
        {
            private EcsGroup _group;
            public EcsWorld World { get { return _group.World; } }
            public bool IsReleased { get { return _group.IsReleased; } }
            public RawEntLong[] Entities
            {
                get
                {
                    RawEntLong[] result = new RawEntLong[_group.Count];
                    int i = 0;
                    foreach (var e in _group)
                    {
                        result[i++] = _group.World.GetRawEntLong(e);
                    }
                    return result;
                }
            }
            public int Count { get { return _group.Count; } }
            public int CapacityDense { get { return _group.CapacityDense; } }
            public override string ToString() { return _group.ToString(); }
            public DebuggerProxy(EcsGroup group) { _group = group; }
            public DebuggerProxy(EcsReadonlyGroup group) : this(group.GetSource_Internal()) { }
        }
        #endregion

        #region PageSlot
        private static int CalcSparseSize(int capacity)
        {
            return (capacity >> PageSlot.SHIFT) + ((capacity & PageSlot.MASK) == 0 ? 0 : 1);
        }
        [DebuggerTypeProxy(typeof(DebuggerProxy))]
        [DebuggerDisplay("Page: {Count}")]
        private struct PageSlot
        {
            public const int SHIFT = 6; // 64
            public const int SIZE = 1 << SHIFT;
            public const int MASK = SIZE - 1;

            public static readonly PageSlot Empty = new PageSlot(_nullPage);

            public int* Indexes;
            public int IndexesXOR;
            public sbyte Count;
            public PageSlot(int* indexes)
            {
                Indexes = indexes;
                IndexesXOR = 0;
                Count = 0;
            }
            private class DebuggerProxy
            {
                private PageSlot _page;
                public int[] Indexes;
                public IntPtr IndexesPtr;
                public bool IsNullPage;
                public int IndexesXOR;
                public sbyte Count;

                public DebuggerProxy(PageSlot page)
                {
                    //if (page.Indexes == null) { return; }
                    //try
                    {
                        _page = page;
                        Indexes = new int[SIZE];
                        for (int i = 0; i < SIZE; i++)
                        {
                            Indexes[i] = page.Indexes[i];
                        }
                        IndexesPtr = (IntPtr)page.Indexes;
                        IndexesXOR = page.IndexesXOR;
                        Count = page.Count;
                        IsNullPage = IndexesPtr == (IntPtr)_nullPagePtrFake;
                    }
                    //catch (Exception)
                    //{
                    //    _page = default;
                    //    Indexes = null;
                    //    IndexesPtr = default;
                    //    IndexesXOR = default;
                    //    Count = default;
                    //    IsNullPage = default;
                    //}
                }
            }
        }
        #endregion
    }
}