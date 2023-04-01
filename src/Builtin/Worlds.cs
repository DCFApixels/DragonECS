namespace DCFApixels.DragonECS
{
    //TODO использовать этот мир для холостых вызовов. чтоб все нулевые ентити стучались в него, так можно попробовать сократить число проверок
    //internal sealed class EcsDeathWrold : EcsWorld<EcsDefaultWrold>
    //{
    //    public EcsDeathWrold(EcsPipeline pipeline) : base(pipeline) { }
    //}


    public sealed class EcsDefaultWrold : EcsWorld<EcsDefaultWrold>
    {
        public EcsDefaultWrold(EcsPipeline pipeline) : base(pipeline) { }
    }
    public sealed class EcsEventWrold : EcsWorld<EcsDefaultWrold>
    {
        public EcsEventWrold(EcsPipeline pipeline) : base(pipeline) { }
    }
}
