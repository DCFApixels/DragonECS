namespace DCFApixels.DragonECS
{
    public sealed class EcsDefaultWorld : EcsWorld, IEntityStorage
    {
        public EcsDefaultWorld() : base(null) { }
        public EcsDefaultWorld(IEcsWorldConfig config) : base(config) { }
    }
    public sealed class EcsEventWorld : EcsWorld, IEntityStorage
    {
        public EcsEventWorld() : base(null) { }
        public EcsEventWorld(IEcsWorldConfig config) : base(config) { }
    }
}
