using System;

namespace DCFApixels.DragonECS
{
    [Serializable]
    public class EcsReactException : Exception
    {
        public EcsReactException() { }
        public EcsReactException(string message) : base(Exceptions.MESSAGE_SUFFIX + message) { }
        public EcsReactException(string message, Exception inner) : base(Exceptions.MESSAGE_SUFFIX + message, inner) { }
        protected EcsReactException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}