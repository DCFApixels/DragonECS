namespace DCFApixels.DragonECS
{
    public interface IEcsProcessor { }

    public interface IEcsDoTag { }
    public struct _PreInit : IEcsDoTag { }
    public struct _Init : IEcsDoTag { }
    public struct _Run : IEcsDoTag { }
    public struct _Destroy : IEcsDoTag { }
    public struct _PostDestroy : IEcsDoTag { }
    public interface IDo<TTag> : IEcsProcessor
        where TTag : IEcsDoTag
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


    public struct _OnComponentRemoved : IEcsMessage
    {
        public int entityID;
    }
    public struct _OnComponentAdded : IEcsMessage
    {
        public int entityID;
    }
    public interface IEcsGReceive<TMessage> : IEcsProcessor
        where TMessage : IEcsMessage
    {
        public void Do<T>(EcsSession session, in TMessage m, in T obj);
    }
}
