namespace DCFApixels.DragonECS
{
    public interface IEcsSystem { }

    public interface IEcsDoTag { }
    public struct _PreInit : IEcsDoTag { }
    public struct _Init : IEcsDoTag { }
    public struct _Run : IEcsDoTag { }
    public struct _Destroy : IEcsDoTag { }
    public struct _PostDestroy : IEcsDoTag { }
    public interface IEcsDo<TTag> : IEcsSystem
        where TTag : IEcsDoTag
    {
        public void Do(EcsSession engine);
    }

    public interface IEcsMessage { }
    public interface IEcsDoMessege<TMessage> : IEcsSystem
        where TMessage : IEcsMessage
    {
        public void Do(EcsSession engine, in TMessage message);
    }

    public interface IEcsSimpleCycleSystem :
        IEcsDo<_Init>,
        IEcsDo<_Run>,
        IEcsDo<_Destroy>
    { }
}
