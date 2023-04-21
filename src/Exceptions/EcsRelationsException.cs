using System;

namespace DCFApixels.DragonECS
{
    [Serializable]
    public class EcsRelationsException : Exception
    {
        public EcsRelationsException() { }
        public EcsRelationsException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public EcsRelationsException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
        protected EcsRelationsException(
            System.Runtime.Serialization.SerializationInfo info, 
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
