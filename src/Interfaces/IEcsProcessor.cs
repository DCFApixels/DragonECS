namespace DCFApixels.DragonECS
{
    public interface IEcsProcessor { }

    public interface IEcsDoTag { }
    public struct _PreInit : IEcsDoTag { }
    public struct _Init : IEcsDoTag { }
    public struct _Run : IEcsDoTag { }
    public struct _Destroy : IEcsDoTag { }
    public struct _PostDestroy : IEcsDoTag { }
    public interface IEcsDo<TTag> : IEcsProcessor
        where TTag : IEcsDoTag
    {
        public void Do(EcsSession session);
    }
    public interface IEcsSimpleCycleProcessor :
    IEcsDo<_Init>,
    IEcsDo<_Run>,
    IEcsDo<_Destroy>
    { }



    public interface IEcsMessage { }
    public interface IEcsDoMessege<TMessage> : IEcsProcessor
        where TMessage : IEcsMessage
    {
        public void Do(EcsSession session, in TMessage message);
    }
}
