namespace DCFApixels.DragonECS
{
    public sealed class EcsWhereExecutor<TSubject> : EcsQueryExecutor where TSubject : EcsSubject
    {
        private TSubject _subject;
        private EcsGroup _filteredGroup;

        private long _executeVersion;

#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
        private EcsProfilerMarker _executeWhere = new EcsProfilerMarker("Where");
#endif

        #region Properties
        public TSubject Subject => _subject;
        internal long ExecuteVersion => _executeVersion;
        #endregion

        #region OnInitialize/OnDestroy
        protected sealed override void OnInitialize()
        {
            _subject = World.GetSubject<TSubject>();
            _filteredGroup = EcsGroup.New(World);
        }
        protected sealed override void OnDestroy()
        {
            _filteredGroup.Dispose();
        }
        #endregion

        #region Methods
        public EcsReadonlyGroup Execute() => ExecuteFor(_subject.World.Entities);
        public EcsReadonlyGroup ExecuteFor(EcsReadonlyGroup sourceGroup)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
            using (_executeWhere.Auto())
#endif
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (sourceGroup.IsNull) throw new System.ArgumentNullException();//TODO составить текст исключения. 
#endif
                _subject.GetIteratorFor(sourceGroup).CopyTo(_filteredGroup);
                return _filteredGroup.Readonly;
            }
        }
#endregion
    }
}
