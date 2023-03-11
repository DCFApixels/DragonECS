namespace DCFApixels.DragonECS
{
    public interface IEcsProcessor { }

    public struct _PreInit { }
    public struct _Init { }
    public struct _Run { }
    public struct _Destroy { }
    public struct _PostDestroy { }
    public interface IDo<TTag> : IEcsProcessor
    {
        public void Do(EcsSession session);
    }
    public interface IEcsSimpleCycleProcessor :
    IDo<_Init>,
    IDo<_Run>,
    IDo<_Destroy>
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

    public readonly struct _OnInject<T> : IEcsMessage
    {
        public readonly T data;
        public _OnInject(T data)
        {
            this.data = data;
        }
    }
    public interface IReceive<TMessage> : IEcsProcessor
        where TMessage : IEcsMessage
    {
        public void Do(EcsSession session, in TMessage m);
    }
}
