namespace DCFApixels.DragonECS
{
    public interface ITemplateNode
    {
        void Apply(short worldID, int entityID);
    }
    public static class ITemplateNodeExtensions
    {
        public static int NewEntity(this EcsWorld world, ITemplateNode template)
        {
            int e = world.NewEntity();
            template.Apply(world.ID, e);
            return e;
        }
        public static entlong NewEntityLong(this EcsWorld world, ITemplateNode template)
        {
            entlong e = world.NewEntityLong();
            template.Apply(world.ID, e.ID);
            return e;
        }
    }
}