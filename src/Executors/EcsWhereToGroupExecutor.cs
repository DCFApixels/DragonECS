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
    internal readonly struct EcsWhereToGroupExecutorCoreList : IEcsWorldComponent<EcsWhereToGroupExecutorCoreList>
    {
        internal readonly EcsWhereToGroupExecutorCore[] _cores;
        public EcsWhereToGroupExecutorCoreList(EcsWhereToGroupExecutorCore[] cores)
        {
            _cores = cores;
        }
        public void Init(ref EcsWhereToGroupExecutorCoreList component, EcsWorld world)
        {
            component = new EcsWhereToGroupExecutorCoreList(new EcsWhereToGroupExecutorCore[64]);
        }
        public void OnDestroy(ref EcsWhereToGroupExecutorCoreList component, EcsWorld world)
        {
            component = default;
        }
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal class EcsWhereToGroupExecutorCore
    {
        private EcsWorld _source;

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

        #region Constructors/Destroy
        public EcsWhereToGroupExecutorCore(EcsWorld source, EcsAspect aspect)
        {
            _source = source;
            _versionsChecker = new PoolVersionsChecker(aspect.Mask);
        }
        public void Destroy()
        {
            _filteredGroup.Dispose();
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Filter(EcsSpan span)
        {
            _iterator.Iterate(span).CopyTo(_filteredGroup);
            _lastWorldVersion = _source.Version;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Execute()
        {
            return ExecuteFor(_source.Entities);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ExecuteFor(EcsSpan span)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            if (span.IsNull) { Throw.ArgumentNull(nameof(span)); }
            if (span.WorldID != _source.id) { Throw.Quiery_ArgumentDifferentWorldsException(); }
#endif
            _source.ReleaseDelEntityBufferAllAuto();
            if (_lastWorldVersion != _source.Version || _versionsChecker.NextEquals() == false)
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
            _filteredGroup = EcsGroup.New(World);
            _versionsChecker = new PoolVersionsChecker(_aspect._mask);
        }
        protected sealed override void OnDestroy()
        {
            _core.Destroy();
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Execute()
        {
            return _core.Execute();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ExecuteFor(EcsSpan span)
        {
            return _core.ExecuteFor(span);
        }
        #endregion
    }
}
