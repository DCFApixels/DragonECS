namespace DCFApixels.DragonECS
{

    public sealed class EmptyQuery : EcsQueryBase
    {
        private long _whereVersion;

        public override long WhereVersion => _whereVersion;

        public EmptyQuery(Builder b) { }

        public sealed override WhereResult Where()
        {
            groupFilter = source.Entities.GetGroupInternal();
            return new WhereResult(this, ++_whereVersion);
        }

        protected sealed override void OnBuild(Builder b) { }
    }
    public static partial class EcsWorldExtensions
    {
        public static WhereResult WhereAll(this EcsWorld self) => self.Select<EmptyQuery>().Where();
    }

    public sealed class HierarchyQuery : EcsJoinAttachQuery<Parent>
    {
        public HierarchyQuery(Builder b) { }
    }
}
