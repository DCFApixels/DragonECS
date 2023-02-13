using System;

namespace DCFApixels.DragonECS
{
    [Serializable]
    public class EcsFrameworkException : Exception
    {
        public EcsFrameworkException() { }
        public EcsFrameworkException(string message) : base(Exceptions.MESSAGE_SUFFIX + message) { }
        public EcsFrameworkException(string message, Exception inner) : base(Exceptions.MESSAGE_SUFFIX + message, inner) { }
        protected EcsFrameworkException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
