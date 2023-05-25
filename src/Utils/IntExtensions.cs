namespace DCFApixels.DragonECS
{
    public static class IntExtensions
    {
        public static entlong ToEntityLong(this int self, EcsWorld world)
        {
            return world.GetEntityLong(self);
        }
    }
}
