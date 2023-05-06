namespace DCFApixels.DragonECS
{
    public sealed class EcsDefaultWorld : EcsWorld<EcsDefaultWorld>
    {
        public EcsDefaultWorld() : base(null) { }
        public EcsDefaultWorld(EcsPipeline pipeline) : base(pipeline) { }
    }
    public sealed class EcsPhysicsEventsWorld : EcsWorld<EcsPhysicsEventsWorld>
    {
        public EcsPhysicsEventsWorld() : base(null) { }
        public EcsPhysicsEventsWorld(EcsPipeline pipeline) : base(pipeline) { }
    }
    public sealed class EcsEventWorld : EcsWorld<EcsEventWorld>
    {
        public EcsEventWorld() : base(null) { }
        public EcsEventWorld(EcsPipeline pipeline) : base(pipeline) { }
    }
    public sealed class EcsUIWorld : EcsWorld<EcsUIWorld>
    {
        public EcsUIWorld() : base(null) { }
        public EcsUIWorld(EcsPipeline pipeline) : base(pipeline) { }
    }
}
