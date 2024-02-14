namespace DCFApixels.DragonECS
{
    public sealed class EcsDefaultWorld : EcsWorld, IEntityStorage
    {
        public EcsDefaultWorld(IEcsWorldConfig config = null, short worldID = -1) : base(config, worldID) { }
    }
    public sealed class EcsEventWorld : EcsWorld, IEntityStorage
    {
        public EcsEventWorld(IEcsWorldConfig config = null, short worldID = -1) : base(config, worldID) { }
    }
}
