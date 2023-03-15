using System;
using System.Runtime.Serialization;

namespace DCFApixels.DragonECS
{
    [Serializable]
    public class EcsFrameworkException : Exception
    {
        public EcsFrameworkException() { }
        public EcsFrameworkException(string message) : base(Exceptions.MESSAGE_SUFFIX + message) { }
        public EcsFrameworkException(string message, Exception inner) : base(Exceptions.MESSAGE_SUFFIX + message, inner) { }
        protected EcsFrameworkException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
