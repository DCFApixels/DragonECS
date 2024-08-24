using DCFApixels.DragonECS.Internal;
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
    internal readonly struct EcsWhereExecutorCoreList : IEcsWorldComponent<EcsWhereExecutorCoreList>
    {
        internal readonly EcsWhereExecutorCore[] _cores;
        public EcsWhereExecutorCoreList(EcsWhereExecutorCore[] cores)
        {
            _cores = cores;
        }
        public void Init(ref EcsWhereExecutorCoreList component, EcsWorld world)
        {
            component = new EcsWhereExecutorCoreList(new EcsWhereExecutorCore[64]);
        }
        public void OnDestroy(ref EcsWhereExecutorCoreList component, EcsWorld world)
        {
            component = default;
        }
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal class EcsWhereExecutorCore
    {
        private EcsWorld _source;

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

        #region Constructors
        public EcsWhereExecutorCore(EcsWorld source, EcsAspect aspect)
        {
            _source = source;
            _versionsChecker = new PoolVersionsChecker(aspect.Mask);
        }
        #endregion

        #region Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Filter(EcsSpan span)
        {
            _filteredEntitiesCount = _iterator.Iterate(span).CopyTo(ref _filteredEntities);
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
            return new EcsSpan(_source.id, _filteredEntities, _filteredEntitiesCount);
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
            int maskID = _aspect.Mask.ID;
            ref var list = ref World.Get<EcsWhereExecutorCoreList>();
            var cores = list._cores;
            if (maskID >= list._cores.Length)
            {
                Array.Resize(ref cores, cores.Length << 1);
                list = new EcsWhereExecutorCoreList(cores);
            }
            ref var coreRef = ref cores[maskID];
            if (coreRef == null)
            {
                coreRef = new EcsWhereExecutorCore(World, _aspect);
            }
            _core = coreRef;
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
