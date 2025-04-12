#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
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
    internal sealed class EcsWhereToGroupExecutor : MaskQueryExecutor
    {
        private EcsMaskIterator _iterator;

        private EcsGroup _filteredAllGroup;
        private EcsGroup _filteredGroup;

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
            get { return _filteredAllGroup.Count; }
        }
        #endregion

        #region OnInitialize/OnDestroy
        protected sealed override void OnInitialize()
        {
            _versionsChecker = new WorldStateVersionsChecker(Mask);
            _filteredAllGroup = EcsGroup.New(World);
            _iterator = Mask.GetIterator();
        }
        protected sealed override void OnDestroy()
        {
            if (_isDestroyed) { return; }
            _isDestroyed = true;
            _filteredAllGroup.Dispose();
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
                _iterator.IterateTo(World.Entities, _filteredAllGroup);
#if DEBUG && DRAGONECS_DEEP_DEBUG
                if(_filteredGroup == null)
                {
                    _filteredGroup = EcsGroup.New(World);
                }
                _filteredGroup.Clear();
                foreach (var e in World.Entities)
                {
                    if(World.IsMatchesMask(Mask, e))
                    {
                        _filteredGroup.Add(e);
                    }
                }
                if(_filteredAllGroup.SetEquals(_filteredGroup) == false)
                {
                    throw new System.InvalidOperationException();
                }
#endif
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteFor_Iternal(EcsSpan span)
        {
#if DEBUG || DRAGONECS_STABILITY_MODE
            if (span.IsNull) { Throw.ArgumentNull(nameof(span)); }
            if (span.WorldID != World.ID) { Throw.Quiery_ArgumentDifferentWorldsException(); }
#endif
            if (_filteredGroup == null)
            {
                _filteredGroup = EcsGroup.New(World);
            }
            _iterator.IterateTo(span, _filteredGroup);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup Execute()
        {
            Execute_Iternal();
            return _filteredAllGroup;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup ExecuteFor(EcsSpan span)
        {
            if (span.IsSourceEntities)
            {
                return Execute();
            }
            ExecuteFor_Iternal(span);
            return _filteredGroup;
        }
        #endregion
    }
}