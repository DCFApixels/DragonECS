#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;

namespace DCFApixels.DragonECS.Core
{
    public abstract class EcsMetaAttribute : Attribute { }


    internal unsafe static class EcsMetaAttributeHalper
    {
        internal const string EMPTY_NO_SENSE_MESSAGE = "With empty parameters, this attribute makes no sense.";
        [ThreadStatic]
        private static string[] _splitBuffer;
        public static string[] Split(char separator, string value)
        {
            if (_splitBuffer == null)
            {
                _splitBuffer = new string[128];
            }
            int length = value.Length;
            int bufferIndex = 0;
            fixed (char* ptr = value)
            {
                var reader = new SplitStream(ptr, value.Length, separator);
                while (reader.Next())
                {
                    if (reader.current != null)
                    {
                        if (_splitBuffer.Length == bufferIndex)
                        {
                            Array.Resize(ref _splitBuffer, _splitBuffer.Length << 1);
                        }
                        _splitBuffer[bufferIndex++] = reader.current;
                    }
                }
            }

            string[] result = new string[bufferIndex];
            for (int i = 0; i < bufferIndex; i++)
            {
                result[i] = _splitBuffer[i];
            }
            return result;
        }

        #region SplitStream
        private ref struct SplitStream
        {
            public string current;
            public char* ptr;
            public int length;
            public readonly char separator;
            public SplitStream(char* ptr, int length, char separator)
            {
                this.ptr = ptr;
                this.length = length;
                this.separator = separator;
                current = null;
            }
            public bool Next()
            {
                if (length <= 0) { return false; }
                char chr;

                char* spanPtr;
                while (char.IsWhiteSpace(chr = *ptr) && length > 0) { ptr++; length--; }
                spanPtr = ptr;

                char* spanEndPtr = spanPtr;
                while ((chr = *ptr) != separator && length > 0)
                {
                    ptr++; length--;
                    if (char.IsWhiteSpace(chr) == false)
                    {
                        spanEndPtr = ptr;
                    }
                }
                ptr++; length--;

                current = spanPtr < spanEndPtr ? new string(spanPtr, 0, (int)(spanEndPtr - spanPtr)) : null;
                return true;
            }
        }
        #endregion
    }
}
