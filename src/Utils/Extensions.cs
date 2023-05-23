namespace DCFApixels.DragonECS
{
    public static class Extensions
    {
        public static entlong ToEntityLong(this int self, EcsWorld world)
        {
            return world.GetEntityLong(self);
        }
    }
}
