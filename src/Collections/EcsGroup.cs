#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

//_dense заполняется с индекса 1
//в операциях изменяющих состояние группы нельзя итерироваться по this, либо осторожно учитывать этот момент
namespace DCFApixels.DragonECS
{
#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 8)]
    [DebuggerTypeProxy(typeof(EcsGroup.DebuggerProxy))]
    public readonly ref struct EcsReadonlyGroup
    {
        private readonly EcsGroup _source;

        #region Properties
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source == null; }
        }
        public int WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.WorldID; }
        }
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.World; }
        }
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.Count; }
        }
        public int CapacityDense
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.CapacityDense; }
        }
        public EcsLongsSpan Longs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.Longs; }
        }
        public bool IsReleazed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.IsReleased; }
        }
        public int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source[index]; }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup(EcsGroup source)
        {
            _source = source;
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID) { return _source.Has(entityID); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int entityID) { return _source.IndexOf(entityID); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int[] array, int arrayIndex) { _source.CopyTo(array, arrayIndex); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup Clone() { return _source.Clone(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start) { return _source.Slice(start); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start, int length) { return _source.Slice(start, length); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ToSpan() { return _source.ToSpan(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] ToArray() { return _source.ToArray(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToArray(ref int[] dynamicBuffer) { return _source.ToArray(ref dynamicBuffer); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ToCollection(ICollection<int> collection) { _source.ToCollection(collection); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup.Enumerator GetEnumerator() { return _source.GetEnumerator(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int First() { return _source.First(); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Last() { return _source.Last(); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsGroup group) { return _source.SetEquals(group); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsReadonlyGroup group) { return _source.SetEquals(group._source); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsSpan span) { return _source.SetEquals(span); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(IEnumerable<int> other) { return _source.SetEquals(other); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsGroup group) { return _source.Overlaps(group); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsReadonlyGroup group) { return _source.Overlaps(group._source); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsSpan span) { return _source.Overlaps(span); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(IEnumerable<int> other) { return _source.Overlaps(other); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(EcsGroup group) { return _source.IsSubsetOf(group); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(EcsReadonlyGroup group) { return _source.IsSubsetOf(group._source); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(EcsSpan span) { return _source.IsSubsetOf(span); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(IEnumerable<int> other) { return _source.IsSubsetOf(other); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSubsetOf(EcsGroup group) { return _source.IsProperSubsetOf(group); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSubsetOf(EcsReadonlyGroup group) { return _source.IsProperSubsetOf(group._source); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSubsetOf(EcsSpan span) { return _source.IsProperSubsetOf(span); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSubsetOf(IEnumerable<int> other) { return _source.IsProperSubsetOf(other); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(EcsGroup group) { return _source.IsSupersetOf(group); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(EcsReadonlyGroup group) { return _source.IsSupersetOf(group._source); }
        public bool IsSupersetOf(EcsSpan span) { return _source.IsSupersetOf(span); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(IEnumerable<int> other) { return _source.IsSupersetOf(other); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSupersetOf(EcsGroup group) { return _source.IsProperSupersetOf(group); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSupersetOf(EcsReadonlyGroup group) { return _source.IsProperSupersetOf(group._source); }
        public bool IsProperSupersetOf(EcsSpan span) { return _source.IsProperSupersetOf(span); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSupersetOf(IEnumerable<int> other) { return _source.IsProperSupersetOf(other); }
        #endregion

        #region Internal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EcsGroup GetSource_Internal() { return _source; }
        #endregion

        #region Other
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

        private int*[] _groupSparsePagePool = new int*[64];
        private int _groupSparsePagePoolCount = 0;

        #region Pages
        internal int* TakePage()
        {
            if (_groupSparsePagePoolCount <= 0)
            {
                var x = UnmanagedArrayUtility.NewAndInit<int>(EcsGroup.PAGE_SIZE);
                return x;
            }
            return _groupSparsePagePool[--_groupSparsePagePoolCount];
        }
        internal void ReturnPage(int* page)
        {
            if (_groupSparsePagePoolCount >= _groupSparsePagePool.Length)
            {
                var old = _groupSparsePagePool;
                _groupSparsePagePool = new int*[_groupSparsePagePoolCount << 1];
                for (int j = 0; j < old.Length; j++)
                {
                    _groupSparsePagePool[j] = old[j];
                }
            }
            _groupSparsePagePool[_groupSparsePagePoolCount++] = page;
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
    //TODO переработать EcsGroup в структуру-обертку, чтобы когда вызывается Release то можно было занулить эту структуру, а может не перерабатывать, есть проблема с боксингом
    public unsafe class EcsGroup : IDisposable, IEnumerable<int>, ISet<int>, IEntityStorage
    {
        internal const int PAGE_SIZE = PageSlot.SIZE;
        private EcsWorld _source;
        private int[] _dense; // 0 индекс для нулевой записи
        private PageSlot* _sparsePages; //Старший бит занят временной маркировкой в операциях над множествами
        private int _sparsePagesCount;
        private int _totalCapacity;
        private int _count = 0;
        internal bool _isReleased = true;

        internal static readonly int* _nullPage = UnmanagedArrayUtility.NewAndInit<int>(PageSlot.SIZE);
        internal static readonly long _nullPagePtrFake = (long)_nullPage;

        #region Properties
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source.ID; }
        }
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _source; }
        }
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _count; }
        }
        public int CapacityDense
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _dense.Length; }
        }
        public EcsReadonlyGroup Readonly
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new EcsReadonlyGroup(this); }
        }
        public EcsLongsSpan Longs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new EcsLongsSpan(this); }
        }
        public bool IsReleased
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _isReleased; }
        }
        bool ICollection<int>.IsReadOnly { get { return false; } }

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
            //            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            //            set
            //            {
            //                // TODO добавить лок енумератора на изменение
            //#if DEBUG || DRAGONECS_STABILITY_MODE
            //                if (index < 0 || index >= Count) { Throw.ArgumentOutOfRange(); }
            //#endif
            //                var oldValue = _dense[index];
            //                _dense[index] = value;
            //                _sparse[oldValue] = 0;
            //            }
        }
        #endregion

        #region Constrcutors/Dispose
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
            _sparsePages = UnmanagedArrayUtility.New<PageSlot>(_sparsePagesCount);
            for (int i = 0; i < _sparsePagesCount; i++)
            {
                _sparsePages[i] = PageSlot.Empty;
            }
        }
        public void Dispose()
        {
            _source.ReleaseGroup(this);
        }
        #endregion

        #region Has/IndexOf
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int entityID)
        {
            ref PageSlot page = ref _sparsePages[entityID >> PageSlot.SHIFT];
            return page.Count == 1 ? _dense[page.IndexesXOR] == entityID : page.Indexes[entityID & PageSlot.MASK] != 0;
        }
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
                Array.Resize(ref _dense, ArrayUtility.NextPow2(_count << 1));
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
        public void Clear()
        {
            if (_count == 0) { return; }
            for (int i = 0; i < _sparsePagesCount; i++)
            {
                ref PageSlot page = ref _sparsePages[i];
                if (page.Indexes != _nullPage)
                {
                    //TODO тут надо оптимизировать отчисткой не всего а по dense списку
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
        #endregion

        #region Upsize
        public void Upsize(int minSize)
        {
            if (minSize >= _dense.Length)
            {
                Array.Resize(ref _dense, ArrayUtility.NextPow2_ClampOverflow(minSize));
            }
        }

        #endregion

        #region CopyFrom/Clone/Slice/ToSpan/ToArray
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyFrom(EcsReadonlyGroup group)
        {
            CopyFrom(group.GetSource_Internal());
        }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(int[] array, int arrayIndex)
        {
            foreach (var item in this)
            {
                array[arrayIndex++] = item;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsGroup Clone()
        {
            EcsGroup result = _source.GetFreeGroup();
            result.CopyFrom(this);
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Slice(int start)
        {
            return Slice(start, _count - start);
        }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ToSpan()
        {
            return new EcsSpan(WorldID, _dense, 1, _count);
        }
        public int[] ToArray()
        {
            int[] result = new int[_count];
            Array.Copy(_dense, 1, result, 0, _count);
            return result;
        }
        public int ToArray(ref int[] dynamicBuffer)
        {
            if (dynamicBuffer.Length < _count)
            {
                Array.Resize(ref dynamicBuffer, ArrayUtility.NextPow2(_count));
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

        #region Set operations

        #region UnionWith
        /// <summary>as Union sets</summary>
        public void UnionWith(EcsGroup group)
        {
#if DEBUG
            if (_source != group._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (_source != group._source) { return; }
#endif
            foreach (var entityID in group) { UnionWithStep(entityID); }
        }
        /// <summary>as Union sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnionWith(EcsReadonlyGroup group) { UnionWith(group.GetSource_Internal()); }
        /// <summary>as Union sets</summary>
        public void UnionWith(EcsSpan span)
        {
#if DEBUG
            if (WorldID != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (WorldID != span.WorldID) { return; }
#endif
            foreach (var entityID in span) { UnionWithStep(entityID); }
        }
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
        /// <summary>as Except sets</summary>
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
        /// <summary>as Except sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExceptWith(EcsReadonlyGroup group) { ExceptWith(group.GetSource_Internal()); }
        /// <summary>as Except sets</summary>
        public void ExceptWith(EcsSpan span)
        {
#if DEBUG
            if (WorldID != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (WorldID != span.WorldID) { return; }
#endif
            foreach (var entityID in span) { ExceptWithStep_Internal(entityID); }
        }
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
        /// <summary>as Intersect sets</summary>
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
        /// <summary>as Intersect sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IntersectWith(EcsReadonlyGroup group) { IntersectWith(group.GetSource_Internal()); }
        /// <summary>as Intersect sets</summary>
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
        /// <summary>as Symmetric Except sets</summary>
        public void SymmetricExceptWith(EcsGroup group)
        {
#if DEBUG
            if (_source != group._source) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (_source != group._source) { return; }
#endif
            foreach (var entityID in group) { SymmetricExceptWithStep_Internal(entityID); }
        }
        /// <summary>as Symmetric Except sets</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SymmetricExceptWith(EcsReadonlyGroup group) { SymmetricExceptWith(group.GetSource_Internal()); }

        /// <summary>as Symmetric Except sets</summary>
        public void SymmetricExceptWith(EcsSpan span)
        {
#if DEBUG
            if (WorldID != span.WorldID) { Throw.Group_ArgumentDifferentWorldsException(); }
#elif DRAGONECS_STABILITY_MODE
            if (WorldID != span.WorldID) { return; }
#endif
            foreach (var entityID in span) { SymmetricExceptWithStep_Internal(entityID); }
        }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetEquals(EcsReadonlyGroup group) { return SetEquals(group.GetSource_Internal()); }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(EcsReadonlyGroup group) { return Overlaps(group.GetSource_Internal()); }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSubsetOf(EcsReadonlyGroup group) { return IsSubsetOf(group.GetSource_Internal()); }
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
        public bool IsSubsetOf(IEnumerable<int> other)
        {
            if (Count == 0) { return true; }
            if (other is ICollection collection && collection.Count < Count) { return false; }
            return IsSubsetOf_Internal(other);
        }

        // ================================================================================

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSubsetOf(EcsReadonlyGroup group) { return IsProperSubsetOf(group.GetSource_Internal()); }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSupersetOf(EcsReadonlyGroup group) { return IsSupersetOf(group.GetSource_Internal()); }
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
        public bool IsSupersetOf(IEnumerable<int> other)
        {
            if (other is ICollection collection && collection.Count > Count) { return false; }
            return IsSupersetOf_Internal(other);
        }

        // ================================================================================

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsProperSupersetOf(EcsReadonlyGroup group) { return IsProperSupersetOf(group.GetSource_Internal()); }
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
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
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
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Union(EcsReadonlyGroup a, EcsReadonlyGroup b)
        {
            return Union(a.GetSource_Internal(), b.GetSource_Internal());
        }
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
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
        /// <summary>as Except sets</summary>
        /// <returns>new group from pool</returns>
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
        /// <summary>as Except sets</summary>
        /// <returns>new group from pool</returns>
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
        /// <summary>as Except sets</summary>
        /// <returns>new group from pool</returns>
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
        /// <summary>as Except sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Except(EcsReadonlyGroup a, EcsReadonlyGroup b)
        {
            return Except(a.GetSource_Internal(), b.GetSource_Internal());
        }
        #endregion

        #region Intersect
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
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
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
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
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Intersect(EcsGroup a, EcsSpan b)
        {
            //операция симметричная, можно просто переставить параметры
            return Intersect(b, a);
        }
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
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
        /// <summary>as Intersect sets</summary>
        /// <returns>new group from pool</returns>
        public static EcsGroup Intersect(EcsReadonlyGroup a, EcsReadonlyGroup b)
        {
            return Intersect(a.GetSource_Internal(), b.GetSource_Internal());
        }
        #endregion

        #region SymmetricExcept
        /// <summary>as Symmetric Except sets</summary>
        /// <returns>new group from pool</returns>
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
        /// <summary>as Symmetric Except sets</summary>
        /// <returns>new group from pool</returns>
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
        public static EcsGroup SymmetricExcept(EcsReadonlyGroup a, EcsReadonlyGroup b)
        {
            return SymmetricExcept(a.GetSource_Internal(), b.GetSource_Internal());
        }
        #endregion

        #region Inverse
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
        public static EcsGroup Inverse(EcsReadonlyGroup a)
        {
            return Inverse(a.GetSource_Internal());
        }
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int First() { return _dense[1]; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Last() { return _dense[_count]; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void OnWorldResize_Internal(int newSize)
        {
            //Array.Resize(ref _sparse, newSize);
            _totalCapacity = newSize;
            var oldPagesCount = _sparsePagesCount;
            _sparsePagesCount = CalcSparseSize(_totalCapacity);
            _sparsePages = UnmanagedArrayUtility.Resize<PageSlot>(_sparsePages, _sparsePagesCount);
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
        public override string ToString()
        {
            return CollectionUtility.EntitiesToString(_dense.Skip(1).Take(_count), "group");
        }
        void ICollection<int>.Add(int item) { Add(item); }
        bool ICollection<int>.Contains(int item) { return Has(item); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsReadonlyGroup(EcsGroup a) { return a.Readonly; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator EcsSpan(EcsGroup a) { return a.ToSpan(); }
        internal class DebuggerProxy
        {
            private EcsGroup _group;
            public EcsWorld World { get { return _group.World; } }
            public bool IsReleased { get { return _group.IsReleased; } }
            public EntitySlotInfo[] Entities
            {
                get
                {
                    EntitySlotInfo[] result = new EntitySlotInfo[_group.Count];
                    int i = 0;
                    foreach (var e in _group)
                    {
                        result[i++] = _group.World.GetEntitySlotInfoDebug(e);
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
                public bool IsNullPage
                {
                    get { return IndexesPtr == (IntPtr)_nullPagePtrFake; }
                }
                public int IndexesXOR;
                public sbyte Count;

                public DebuggerProxy(PageSlot page)
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
                }
            }
        }
        #endregion
    }
}