using System;

namespace DCFApixels.DragonECS
{
    [Serializable]
    public class EcsFrameworkException : Exception
    {
        private const string MESSAGE_SUFFIX = "[DragonECS] ";
        public EcsFrameworkException() { }
        public EcsFrameworkException(string message) : base(MESSAGE_SUFFIX + message) { }
        public EcsFrameworkException(string message, Exception inner) : base(MESSAGE_SUFFIX + message, inner) { }
        protected EcsFrameworkException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
