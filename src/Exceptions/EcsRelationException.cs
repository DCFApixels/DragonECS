using System;

namespace DCFApixels.DragonECS
{
    [Serializable]
    public class EcsRelationException : EcsFrameworkException
    {
        public EcsRelationException() { }
        public EcsRelationException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public EcsRelationException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
        protected EcsRelationException(
            System.Runtime.Serialization.SerializationInfo info, 
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
