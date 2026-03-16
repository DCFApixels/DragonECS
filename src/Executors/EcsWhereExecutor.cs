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
        public sealed override long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _version; }
        }
        public sealed override bool IsCached
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _versionsChecker.Check(); }
        }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsUnsafeSpan Execute()
        {
            Execute_Iternal();
            var result = new EcsUnsafeSpan(World.ID, _filteredAllEntities.Ptr, _filteredAllEntitiesCount);
#if DEBUG && DRAGONECS_DEEP_DEBUG
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
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsUnsafeSpan ExecuteFor(EcsSpan span)
        {
            if (span.IsSourceEntities)
            {
                return Execute();
            }
            ExecuteFor_Iternal(span);
            var result = new EcsUnsafeSpan(World.ID, _filteredEntities.Ptr, _filteredEntitiesCount);
#if DEBUG && DRAGONECS_DEEP_DEBUG
            foreach (var e in result)
            {
                if (World.IsMatchesMask(Mask, e) == false)
                {
                    Throw.DeepDebugException();
                }
            }
#endif
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsUnsafeSpan Execute(Comparison<int> comparison)
        {
            Execute_Iternal();
            ArraySortUtility.Sort(_filteredAllEntities.AsSpan(_filteredAllEntitiesCount), comparison);
            return new EcsUnsafeSpan(World.ID, _filteredAllEntities.Ptr, _filteredAllEntitiesCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsUnsafeSpan ExecuteFor(EcsSpan source, Comparison<int> comparison)
        {
            if (source.IsSourceEntities)
            {
                return Execute(comparison);
            }
            ExecuteFor_Iternal(source);
            ArraySortUtility.Sort(_filteredEntities.AsSpan(_filteredEntitiesCount), comparison);
            return new EcsUnsafeSpan(World.ID, _filteredEntities.Ptr, _filteredEntitiesCount);
        }
        #endregion
    }
}