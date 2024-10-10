﻿using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS.Internal
{
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal class EcsWhereToGroupExecutor : EcsQueryExecutor
    {
        private EcsMaskIterator _iterator;
        private EcsGroup _filteredAllGroup;
        private long _version;

        private EcsGroup _filteredGroup;

        private long _lastWorldVersion;
        private PoolVersionsChecker _versionsChecker;

        #region Properties
        public sealed override long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _version; }
        }
        #endregion

        #region OnInitialize/OnDestroy
        protected sealed override void OnInitialize()
        {
            _versionsChecker = new PoolVersionsChecker(Mask);
            _filteredAllGroup = EcsGroup.New(World);
            _iterator = Mask.GetIterator();
        }
        protected sealed override void OnDestroy()
        {
            _filteredAllGroup.Dispose();
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Execute_Iternal()
        {
            World.ReleaseDelEntityBufferAllAuto();
            if (_lastWorldVersion != World.Version || _versionsChecker.NextEquals() == false)
            {
                _version++;
                _iterator.IterateTo(World.Entities, _filteredAllGroup);
            }
            _lastWorldVersion = World.Version;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteFor_Iternal(EcsSpan span)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (span.IsNull) { Throw.ArgumentNull(nameof(span)); }
            if (span.WorldID != World.id) { Throw.Quiery_ArgumentDifferentWorldsException(); }
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
            ExecuteFor_Iternal(span);
            return _filteredGroup;
        }
        #endregion
    }
}