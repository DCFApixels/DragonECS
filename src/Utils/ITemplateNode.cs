namespace DCFApixels.DragonECS
{
    public interface ITemplateNode
    {
        void Apply(int worldID, int entityID);
    }
    public static class ITemplateNodeExtensions
    {
        public static int NewEntity(this EcsWorld world, ITemplateNode template)
        {
            int e = world.NewEntity();
            template.Apply(world.id, e);
            return e;
        }
        public static entlong NewEntityLong(this EcsWorld world, ITemplateNode template)
        {
            entlong e = world.NewEntityLong();
            template.Apply(world.id, e.ID);
            return e;
        }
    }
}