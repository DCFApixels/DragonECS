namespace DCFApixels.DragonECS
{
    public sealed class HierarchySubject : EcsSubject
    {
        public readonly EcsAttachPool<Parent> parents;
        public HierarchySubject(Builder b)
        {
            parents = b.Include<Parent>();
        }
    }
}
