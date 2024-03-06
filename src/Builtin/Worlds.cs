namespace DCFApixels.DragonECS
{
    public sealed class EcsDefaultWorld : EcsWorld
    {
        public EcsDefaultWorld(EcsWorldConfig config, short worldID = -1) : base(config, worldID) { }
        public EcsDefaultWorld(IConfigContainer configs = null, short worldID = -1) : base(configs, worldID) { }
    }
    public sealed class EcsEventWorld : EcsWorld
    {
        public EcsEventWorld(EcsWorldConfig config, short worldID = -1) : base(config, worldID) { }
        public EcsEventWorld(IConfigContainer configs = null, short worldID = -1) : base(configs, worldID) { }
    }
}
