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
    public readonly struct _OnNewEntityCreated : IEcsMessage
    {
        public readonly ent entity;

        public _OnNewEntityCreated(ent entity)
        {
            this.entity = entity;
        }
    }
    public readonly struct _OnNewEntityGenRecycled : IEcsMessage
    {
        public readonly ent entity;
        public _OnNewEntityGenRecycled(ent entity)
        {
            this.entity = entity;
        }
    }

    public interface IEcsDoMessege<TMessage> : IEcsProcessor
        where TMessage : IEcsMessage
    {
        public void Do(EcsSession session, in TMessage message);
    }
}
