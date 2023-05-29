using System;
using System.Runtime.Serialization;

namespace DCFApixels.DragonECS
{
    [Serializable]
    public class EcsFrameworkException : Exception
    {
        public EcsFrameworkException() { }
        public EcsFrameworkException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public EcsFrameworkException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
        protected EcsFrameworkException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
    [Serializable]
    public class EcsRunnerImplementationException : EcsFrameworkException
    {
        public EcsRunnerImplementationException() { }
        public EcsRunnerImplementationException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public EcsRunnerImplementationException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
        protected EcsRunnerImplementationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
