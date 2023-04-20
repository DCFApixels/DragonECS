namespace DCFApixels.DragonECS
{
    //TODO использовать этот мир для холостых вызовов. чтоб все нулевые ентити стучались в него, так можно попробовать сократить число проверок
    //internal sealed class EcsDeathWrold : EcsWorld<EcsDefaultWrold>
    //{
    //    public EcsDeathWrold(EcsPipeline pipeline) : base(pipeline) { }
    //}


    public sealed class EcsDefaultWrold : EcsWorld<EcsDefaultWrold>
    {
        public EcsDefaultWrold(EcsPipeline pipeline = null) : base(pipeline) { }
    }
    public sealed class EcsEventWrold : EcsEdgeWorld<EcsEventWrold>
    {
        public EcsEventWrold(IEcsWorld firstTarget, IEcsWorld secondTarget, EcsPipeline pipeline = null) : base(firstTarget, secondTarget, pipeline) { }
    }
}
