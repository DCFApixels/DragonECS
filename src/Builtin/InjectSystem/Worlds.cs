namespace DCFApixels.DragonECS
{
    public sealed class EcsDefaultWorld : EcsWorld
    {
        public EcsDefaultWorld(IEcsWorldConfig config = null, short worldID = -1) : base(config, worldID) { }
    }
    public sealed class EcsEventWorld : EcsWorld
    {
        public EcsEventWorld(IEcsWorldConfig config = null, short worldID = -1) : base(config, worldID) { }
    }
}
