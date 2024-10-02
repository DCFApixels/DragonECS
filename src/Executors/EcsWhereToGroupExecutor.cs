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
    internal class EcsWhereToGroupExecutor : EcsQueryExecutor
    {
        private EcsMaskIterator _iterator;
        private EcsGroup _filteredAllGroup;
        private long _version;

        private EcsGroup _filteredGroup;

        private long _lastWorldVersion;
        private PoolVersionsChecker _versionsChecker;

        #region Properties
        public long Version
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
                _iterator.Iterate(World.Entities).CopyTo(_filteredAllGroup);
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
            _iterator.Iterate(span).CopyTo(_filteredGroup);
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

namespace DCFApixels.DragonECS
{
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsWhereToGroupCache<TAspect> : EcsQueryCache where TAspect : EcsAspect, new()
    {
        private TAspect _aspect;
        private EcsWhereToGroupExecutor _executor;

        #region Properties
        public TAspect Aspect
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _aspect; }
        }
        public sealed override long Version
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _executor.Version; }
        }
        #endregion

        #region OnInitialize/OnDestroy
        protected sealed override void OnInitialize()
        {
            _aspect = World.GetAspect<TAspect>();
            _executor = Mediator.GetCore<EcsWhereToGroupExecutor>(_aspect.Mask);
        }
        protected sealed override void OnDestroy()
        {
            _executor.Destroy();
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup Execute()
        {
            return _executor.Execute();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsReadonlyGroup ExecuteFor(EcsSpan span)
        {
            return _executor.ExecuteFor(span);
        }
        #endregion
    }
}
