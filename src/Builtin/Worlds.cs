namespace DCFApixels.DragonECS
{
    public sealed class EcsDefaultWrold : EcsWorld<EcsDefaultWrold>
    {
        public EcsDefaultWrold(EcsPipeline pipeline) : base(pipeline) { }
    }
    public sealed class EcsEventWrold : EcsWorld<EcsDefaultWrold>
    {
        public EcsEventWrold(EcsPipeline pipeline) : base(pipeline) { }
    }
}
