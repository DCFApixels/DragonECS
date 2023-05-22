using Unity.Profiling;

namespace DCFApixels.DragonECS
{
    public sealed class EcsWhereExecutor<TSubject> : EcsQueryExecutor where TSubject : EcsSubject
    {
        private readonly TSubject _subject;
        private readonly EcsGroup _filteredGroup;

        private long _executeVersion;

        private ProfilerMarker _executeWhere = new ProfilerMarker("JoinAttachQuery.Where");

        #region Properties
        public TSubject Subject => _subject;
        internal long ExecuteVersion => _executeVersion;
        #endregion

        #region Constructors
        public EcsWhereExecutor(TSubject subject)
        {
            _subject = subject;
            _filteredGroup = EcsGroup.New(subject.World);
        }
        #endregion

        #region Methods
        public EcsWhereResult<TSubject> Execute() => ExecuteFor(_subject.World.Entities);
        public EcsWhereResult<TSubject> ExecuteFor(EcsReadonlyGroup sourceGroup)
        {
            using (_executeWhere.Auto())
            {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (sourceGroup.IsNull) throw new System.ArgumentNullException();//TODO составить текст исключения. 
#endif
                _subject.GetIteratorFor(sourceGroup).CopyTo(_filteredGroup);
                return new EcsWhereResult<TSubject>(this, _filteredGroup.Readonly);
            }
        }
        internal sealed override void Destroy()
        {
            _filteredGroup.Release();
        }
        #endregion
    }

    #region WhereExecuter Results
    public readonly ref struct EcsWhereResult<TSubject> where TSubject : EcsSubject
    {
        public readonly TSubject s;
        private readonly EcsWhereExecutor<TSubject> _executer;
        public readonly EcsReadonlyGroup group;
        private readonly long _version;
        public bool IsRelevant => _version == _executer.ExecuteVersion;

        public EcsWhereResult(EcsWhereExecutor<TSubject> executer, EcsReadonlyGroup group)
        {
            _executer = executer;
            _version = executer.ExecuteVersion;
            s = executer.Subject;
            this.group = group;
        }
        public EcsGroup.Enumerator GetEnumerator()
        {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (!IsRelevant) throw new System.InvalidOperationException();//TODO составить текст исключения. 
#endif
            return group.GetEnumerator();
        }

        public override string ToString()
        {
            return group.ToString();
        }
    }
    #endregion
}
