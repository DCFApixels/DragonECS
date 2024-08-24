using DCFApixels.DragonECS.Internal;
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
    internal class EcsWhereToGroupExecutorCore : EcsQueryExecutorCore
    {
        private EcsMaskIterator _iterator;
        private EcsGroup _filteredGroup;

        private long _lastWorldVersion;
        private PoolVersionsChecker _versionsChecker;

        #region Properties
        public long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _lastWorldVersion; }
        }
        #endregion

        #region OnInitialize/OnDestroy
        protected sealed override void OnInitialize()
        {
            _versionsChecker = new PoolVersionsChecker(Mask);
            _filteredGroup = EcsGroup.New(World);
            _iterator = Mask.GetIterator();
        }
        protected sealed override void OnDestroy()
        {
            _filteredGroup.Dispose();
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Filter(EcsSpan span)
        {
            _iterator.Iterate(span).CopyTo(_filteredGroup);
            _lastWorldVersion = World.Version;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup Execute()
        {
            return ExecuteFor(World.Entities);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup ExecuteFor(EcsSpan span)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (span.IsNull) { Throw.ArgumentNull(nameof(span)); }
            if (span.WorldID != World.id) { Throw.Quiery_ArgumentDifferentWorldsException(); }
#endif
            World.ReleaseDelEntityBufferAllAuto();
            if (_lastWorldVersion != World.Version || _versionsChecker.NextEquals() == false)
            {
                Filter(span);
            }
            return _filteredGroup.Readonly;
        }
        #endregion
    }
}

namespace DCFApixels.DragonECS
{
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsWhereToGroupExecutor<TAspect> : EcsQueryExecutor where TAspect : EcsAspect, new()
    {
        private TAspect _aspect;
        private EcsWhereToGroupExecutorCore _core;

        #region Properties
        public TAspect Aspect
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _aspect; }
        }
        public sealed override long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _core.Version; }
        }
        #endregion

        #region OnInitialize/OnDestroy
        protected sealed override void OnInitialize()
        {
            _aspect = World.GetAspect<TAspect>();
            _core = Mediator.GetCore<EcsWhereToGroupExecutorCore>(_aspect.Mask);
        }
        protected sealed override void OnDestroy()
        {
            _core.Destroy();
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup Execute()
        {
            return _core.Execute();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup ExecuteFor(EcsSpan span)
        {
            return _core.ExecuteFor(span);
        }
        #endregion
    }
}
