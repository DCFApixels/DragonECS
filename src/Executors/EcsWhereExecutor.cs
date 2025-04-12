#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using System;
using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS.Internal
{
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal sealed class EcsWhereExecutor : MaskQueryExecutor
    {
        private EcsMaskIterator _iterator;

        private int[] _filteredAllEntities = new int[32];
        private int _filteredAllEntitiesCount = 0;
        private int[] _filteredEntities = null;
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
                _filteredAllEntitiesCount = _iterator.IterateTo(World.Entities, ref _filteredAllEntities);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteFor_Iternal(EcsSpan span)
        {
#if DEBUG || DRAGONECS_STABILITY_MODE
            if (span.IsNull) { Throw.ArgumentNull(nameof(span)); }
            if (span.WorldID != World.ID) { Throw.Quiery_ArgumentDifferentWorldsException(); }
#endif
            if (_filteredEntities == null)
            {
                _filteredEntities = new int[32];
            }
            _filteredEntitiesCount = _iterator.IterateTo(span, ref _filteredEntities);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Execute()
        {
            Execute_Iternal();
#if DEBUG && DRAGONECS_DEEP_DEBUG
            var newSpan = new EcsSpan(World.ID, _filteredAllEntities, _filteredAllEntitiesCount);
            using (EcsGroup group = EcsGroup.New(World))
            {
                foreach (var e in World.Entities)
                {
                    if (World.IsMatchesMask(Mask, e))
                    {
                        group.Add(e);
                    }
                }

                if (group.SetEquals(newSpan) == false)
                {
                    int[] array = new int[_filteredAllEntities.Length];
                    var count = _iterator.IterateTo(World.Entities, ref array);

                    EcsDebug.PrintError(newSpan.ToString() + "\r\n" + group.ToSpan().ToString());
                    Throw.DeepDebugException();
                }
            }
#endif
            return new EcsSpan(World.ID, _filteredAllEntities, _filteredAllEntitiesCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ExecuteFor(EcsSpan span)
        {
            if (span.IsSourceEntities)
            {
                return Execute();
            }
            ExecuteFor_Iternal(span);
#if DEBUG && DRAGONECS_DEEP_DEBUG
            var newSpan = new EcsSpan(World.ID, _filteredEntities, _filteredEntitiesCount);
            foreach (var e in newSpan)
            {
                if (World.IsMatchesMask(Mask, e) == false)
                {
                    Throw.DeepDebugException();
                }
            }
#endif
            return new EcsSpan(World.ID, _filteredEntities, _filteredEntitiesCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Execute(Comparison<int> comparison)
        {
            Execute_Iternal();
            ArraySortHalperX<int>.Sort(_filteredAllEntities, comparison, _filteredAllEntitiesCount);
            return new EcsSpan(World.ID, _filteredAllEntities, _filteredAllEntitiesCount);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ExecuteFor(EcsSpan span, Comparison<int> comparison)
        {
            if (span.IsSourceEntities)
            {
                return Execute(comparison);
            }
            ExecuteFor_Iternal(span);
            ArraySortHalperX<int>.Sort(_filteredEntities, comparison, _filteredEntitiesCount);
            return new EcsSpan(World.ID, _filteredEntities, _filteredEntitiesCount);
        }
        #endregion
    }
}