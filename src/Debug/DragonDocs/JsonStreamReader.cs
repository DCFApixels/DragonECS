using System.IO;

namespace DCFApixels.DragonECS.Internal
{
    internal struct JsonStreamReader
    {
        public StreamReader _stream;
        private static char[] _chars;
        public void Next()
        {
            _stream.Read(_chars, 0, 100);
        }
    }
}
