namespace DCFApixels.DragonECS
{
    public sealed class EcsWhereExecutor<TAspect> : EcsQueryExecutor where TAspect : EcsAspect
    {
        private TAspect _aspect;
        private EcsGroup _filteredGroup;

        private long _executeVersion;

#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
        private EcsProfilerMarker _executeWhere = new EcsProfilerMarker("Where");
#endif

        #region Properties
        public TAspect Aspect => _aspect;
        internal long ExecuteVersion => _executeVersion;
        #endregion

        #region OnInitialize/OnDestroy
        protected sealed override void OnInitialize()
        {
            _aspect = World.GetAspect<TAspect>();
            _filteredGroup = EcsGroup.New(World);
        }
        protected sealed override void OnDestroy()
        {
            _filteredGroup.Dispose();
        }
        #endregion

        #region Methods
        public EcsReadonlyGroup Execute() => ExecuteFor(_aspect.World.Entities);
        public EcsReadonlyGroup ExecuteFor(EcsReadonlyGroup sourceGroup)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            _executeWhere.Begin();
            if (sourceGroup.IsNull) throw new System.ArgumentNullException();//TODO составить текст исключения. 
#endif
            _aspect.GetIteratorFor(sourceGroup).CopyTo(_filteredGroup);
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            _executeWhere.End();
#endif
            return _filteredGroup.Readonly;
        }
        #endregion
    }
}
