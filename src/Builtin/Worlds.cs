namespace DCFApixels.DragonECS
{
    internal sealed class EcsNullWorld : EcsWorld<EcsNullWorld>
    {
        public EcsNullWorld() : base(false) { }
    }
    public sealed class EcsDefaultWorld : EcsWorld<EcsDefaultWorld> { }
    public sealed class EcsEventWorld : EcsWorld<EcsEventWorld> { }
}
