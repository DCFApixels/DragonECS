using System;

namespace DCFApixels.DragonECS
{
    [Serializable]
    public class EcsFrameworkException : Exception
    {
        public EcsFrameworkException() { }
        public EcsFrameworkException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public EcsFrameworkException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
        protected EcsFrameworkException(
            System.Runtime.Serialization.SerializationInfo info, 
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
