using System;

namespace DCFApixels.DragonECS
{
    [Serializable]
    public class EcsRunnerImplementationException : Exception
    {
        public EcsRunnerImplementationException() { }
        public EcsRunnerImplementationException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public EcsRunnerImplementationException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
        protected EcsRunnerImplementationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
