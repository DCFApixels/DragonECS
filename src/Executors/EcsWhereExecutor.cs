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
    internal class EcsWhereExecutorCore : EcsQueryExecutorCore
    {
        private EcsMaskIterator _iterator;
        private int[] _filteredEntities = new int[32];
        private int _filteredEntitiesCount = 0;

        private long _lastWorldVersion = 0;
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
            _iterator = Mask.GetIterator();
        }
        protected sealed override void OnDestroy() { }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Filter(EcsSpan span)
        {
            _filteredEntitiesCount = _iterator.Iterate(span).CopyTo(ref _filteredEntities);
            _lastWorldVersion = World.Version;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan Execute()
        {
            return ExecuteFor(World.Entities);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsSpan ExecuteFor(EcsSpan span)
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
            return new EcsSpan(World.id, _filteredEntities, _filteredEntitiesCount);
        }

        //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //        public EcsSpan Execute(Comparison<int> comparison) 
        //        {
        //            return ExecuteFor(_aspect.World.Entities, comparison);
        //        }
        //        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //        public EcsSpan ExecuteFor(EcsSpan span, Comparison<int> comparison)
        //        {
        //#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
        //            if (span.IsNull) { Throw.ArgumentNull(nameof(span)); }
        //            if (span.WorldID != WorldID) { Throw.Quiery_ArgumentDifferentWorldsException(); }
        //#endif
        //            World.ReleaseDelEntityBufferAllAuto();
        //            if (_lastWorldVersion != World.Version)
        //            {
        //                Filter(span);
        //            }
        //            Array.Sort<int>(_filteredEntities, 0, _filteredEntitiesCount, comparison);
        //        }
        #endregion
    }
}

namespace DCFApixels.DragonECS
{
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsWhereExecutor<TAspect> : EcsQueryExecutor where TAspect : EcsAspect, new()
    {
        private TAspect _aspect;
        private EcsWhereExecutorCore _core;

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
            _core = Mediator.GetCore<EcsWhereExecutorCore>(_aspect.Mask);
        }
        protected sealed override void OnDestroy() { }
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
