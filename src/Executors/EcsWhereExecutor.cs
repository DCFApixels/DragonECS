#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Runtime.CompilerServices;
using static DCFApixels.DragonECS.Core.Internal.MemoryAllocator;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS.Core.Internal
{
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal sealed unsafe class EcsWhereExecutor : MaskQueryExecutor
    {
        private EcsMaskIterator _iterator;

        private HMem<int> _filteredAllEntities = Alloc<int>(32);
        private int _filteredAllEntitiesCount = 0;
        private HMem<int> _filteredEntities = default;
        private int _filteredEntitiesCount = 0;

        private long _version;
        private WorldStateVersionsChecker _versionsChecker;

        public bool _isDestroyed = false;

        #region Properties
        /// <summary>
        /// Version number representing the internal cached result state for this executor.
        /// </summary>
        public sealed override long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _version; }
        }
        /// <summary>
        /// Indicates whether the executor's cached results are still valid for the world state.
        /// </summary>
        public sealed override bool IsCached
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _versionsChecker.Check(); }
        }
        /// <summary>
        /// Number of entities in the last cached result set.
        /// </summary>
        public sealed override int LastCachedCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _filteredAllEntitiesCount; }
        }
        #endregion

        #region OnInitialize/OnDestroy
        protected sealed override void OnInitialize()
        {
            _versionsChecker = new WorldStateVersionsChecker(Mask);
            _iterator = Mask.GetIterator();
        }
        protected sealed override void OnDestroy()
        {
            if (_isDestroyed) { return; }
            if (_filteredAllEntities.IsCreated)
            {
                _filteredAllEntities.DisposeAndReset();
            }
            if (_filteredEntities.IsCreated)
            {
                _filteredEntities.DisposeAndReset();
            }
            _isDestroyed = true;
            _versionsChecker.Dispose();
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Execute_Iternal()
        {
            World.ReleaseDelEntityBufferAllAuto();
            if (_versionsChecker.CheckAndNext() == false)
            {
                _version++;
                _filteredAllEntitiesCount = _iterator.CacheTo(World.Entities, ref _filteredAllEntities);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteFor_Iternal(EcsSpan span)
        {
#if DEBUG || DRAGONECS_STABILITY_MODE
            if (span.IsNull) { Throw.ArgumentNull(nameof(span)); }
            if (span.WorldID != World.ID) { Throw.Quiery_ArgumentDifferentWorldsException(); }
#endif
            if (_filteredEntities.IsCreated == false)
            {
                _filteredEntities = Alloc<int>(32);
            }
            _filteredEntitiesCount = _iterator.CacheTo(span, ref _filteredEntities);
        }

        /// <summary>
        /// Executes the mask query against all entities currently alive in the world,
        /// and returns an unsafe span of matching entity IDs.
        /// The result is cached internally and reused if the world state hasn't changed.
        /// </summary>
        /// <returns>
        /// An <see cref="EcsUnsafeSpan"/> containing the entity IDs that satisfy the mask conditions.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsUnsafeSpan Execute()
        {
            Execute_Iternal();
#if DEBUG && DRAGONECS_DEEP_DEBUG
            var result = new EcsUnsafeSpan(World.ID, _filteredAllEntities.Ptr, _filteredAllEntitiesCount);
            using (EcsGroup group = EcsGroup.New(World))
            {
                foreach (var e in World.Entities)
                {
                    if (World.IsMatchesMask(Mask, e))
                    {
                        group.Add(e);
                    }
                }

                if (group.SetEquals(result.ToSpan()) == false)
                {
                    int[] array = new int[_filteredAllEntities.Length];
                    var count = _iterator.CacheTo(World.Entities, ref array);

                    EcsDebug.PrintError(result.ToString() + "\r\n" + group.ToSpan().ToString());
                    Throw.DeepDebugException();
                }
            }
#endif
            return new EcsUnsafeSpan(World.ID, _filteredAllEntities.Ptr, _filteredAllEntitiesCount); ;
        }

        /// <summary>
        /// Executes the mask query only on the subset of entities provided in the given span,
        /// returning an unsafe span of those that match the mask.
        /// </summary>
        /// <param name="span">
        /// The span of entity IDs to filter against the mask. Must belong to the same world.
        /// </param>
        /// <returns>
        /// An <see cref="EcsUnsafeSpan"/> containing the matching entity IDs from the supplied span.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsUnsafeSpan ExecuteFor(EcsSpan span)
        {
            if (span.IsSourceEntities)
            {
                return Execute();
            }
            ExecuteFor_Iternal(span);
#if DEBUG && DRAGONECS_DEEP_DEBUG
            var result = new EcsUnsafeSpan(World.ID, _filteredEntities.Ptr, _filteredEntitiesCount);
            foreach (var e in result)
            {
                if (World.IsMatchesMask(Mask, e) == false)
                {
                    Throw.DeepDebugException();
                }
            }
#endif
            return new EcsUnsafeSpan(World.ID, _filteredEntities.Ptr, _filteredEntitiesCount); ;
        }

        /// <summary>
        /// Executes the mask query against all world entities and sorts the resulting entity IDs
        /// using the specified comparison delegate.
        /// </summary>
        /// <param name="comparison">
        /// The comparison function used to order the entity IDs in the result.
        /// </param>
        /// <returns>
        /// An <see cref="EcsUnsafeSpan"/> containing the sorted list of matching entity IDs.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsUnsafeSpan Execute(Comparison<int> comparison)
        {
            Execute_Iternal();
            SortHalper.Sort(_filteredAllEntities.AsSpan(_filteredAllEntitiesCount), comparison);
            return new EcsUnsafeSpan(World.ID, _filteredAllEntities.Ptr, _filteredAllEntitiesCount);
        }

        /// <summary>
        /// Executes the mask query on the given span of entity IDs and sorts the matching results
        /// using the specified comparison delegate.
        /// </summary>
        /// <param name="source">
        /// The span of entity IDs to filter against the mask. Must belong to the same world.
        /// </param>
        /// <param name="comparison">
        /// The comparison function used to order the entity IDs in the result.
        /// </param>
        /// <returns>
        /// An <see cref="EcsUnsafeSpan"/> containing the sorted matching entity IDs from the source span.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsUnsafeSpan ExecuteFor(EcsSpan source, Comparison<int> comparison)
        {
            if (source.IsSourceEntities)
            {
                return Execute(comparison);
            }
            ExecuteFor_Iternal(source);
            SortHalper.Sort(_filteredEntities.AsSpan(_filteredEntitiesCount), comparison);
            return new EcsUnsafeSpan(World.ID, _filteredEntities.Ptr, _filteredEntitiesCount);
        }

        /// <summary>
        /// Returns a managed snapshot of the current query result as an <see cref="EcsSpan"/>.
        /// This method internally calls <see cref="Execute()"/> and converts the unsafe span to a safe span.
        /// </summary>
        /// <returns>
        /// An <see cref="EcsSpan"/> containing the entity IDs that match the mask.
        /// </returns>
        public override EcsSpan Snapshot() { return Execute(); }
        #endregion
    }
}