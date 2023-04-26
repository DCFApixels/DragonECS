namespace DCFApixels.DragonECS
{
    //TODO использовать этот мир для холостых вызовов. чтоб все нулевые ентити стучались в него, так можно попробовать сократить число проверок
    //internal sealed class EcsDeathWrold : EcsWorld<EcsDefaultWorld>
    //{
    //    public EcsDeathWrold(EcsPipeline pipeline) : base(pipeline) { }
    //}


    public sealed class EcsDefaultWorld : EcsWorld<EcsDefaultWorld>
    {
        public EcsDefaultWorld() : base(null) { }
        public EcsDefaultWorld(EcsPipeline pipeline) : base(pipeline) { }
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
