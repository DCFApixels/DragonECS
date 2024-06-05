using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Internal
{
    internal unsafe ref struct JsonIterator
    {
        private char* _jsonPtr;
        private int _jsonLength;
        private int _depth;
        private JsonNode _currentNode;
        private char _currentChar;
        public int Depth
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _depth; }
        }
        public char Char
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _jsonPtr[0]; }
        }
        public JsonNode Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _currentNode; }
        }
        public JsonIterator(char* jsonPtr, int jsonLength)
        {
            _jsonPtr = jsonPtr - 1;
            _jsonLength = jsonLength + 1;
            _depth = 0;
            _currentChar = '\0';
            _currentNode = default;
        }
        public JsonNode Next()
        {
            _currentNode = NextInternal();
            return _currentNode;
        }
        public override string ToString() { return new string(_jsonPtr, 0, _jsonLength); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private JsonNode NextInternal()
        {
            if (NextSymbol() == false)
            {
                return new JsonNode(JsonNodeType.NA, null, 0, _depth); ;
            }
            if (IsEnterChar(_currentChar))
            {
                _depth++;
                return new JsonNode(JsonNodeType.Enter, _jsonPtr, 1, _depth);
            }
            if (IsExitChar(_currentChar))
            {
                _depth--;
                return new JsonNode(JsonNodeType.Exit, _jsonPtr, 1, _depth);
            }
            if (_currentChar == '"')
            {
                return ReadString();
            }
            if (char.IsLetter(_currentChar))
            {
                return ReadValue();
            }
            throw new FormatException("JSON format error.");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private JsonNode ReadString()
        {
            JsonSpan span = new JsonSpan(_jsonPtr);
            bool isEscape = false;
            while (Increment())
            {
                if (_currentChar == '"' && isEscape == false)
                {
                    JsonNodeType type = JsonNodeType.Value;
                    bool x = NextSymbol();
                    if (x && _currentChar == ':')
                    {
                        type = JsonNodeType.Key;
                    }
                    else
                    {
                        Deincrement();
                    }
                    return new JsonNode(type, span.ptr + 1, span.length, _depth);
                }
                isEscape = isEscape == false && _currentChar == '\\';
                span.length++;
            }
            throw new FormatException("JSON format error.");
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private JsonNode ReadValue()
        {
            JsonSpan span = new JsonSpan(_jsonPtr);
            while (Increment() && (char.IsWhiteSpace(_currentChar) || _currentChar == ',' || _currentChar == ':' || _currentChar == '}' || _currentChar == ']') == false)
            {
                span.length++;
            }
            JsonNodeType type = JsonNodeType.Value;
            Deincrement();
            if (NextSymbol() && _currentChar == ':')
            {
                type = JsonNodeType.Key;
            }
            else
            {
                Deincrement();
            }
            return new JsonNode(type, span.ptr, span.length + 1, _depth);
        }
        private ref struct JsonSpan
        {
            public char* ptr;
            public int length;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public JsonSpan(char* ptr)
            {
                this.ptr = ptr;
                length = 0;
            }
            public override string ToString() { return new string(ptr, 0, length); }
        }
        private bool NextSymbol()
        {
            bool isNotEnd;
            while ((isNotEnd = Increment()) && (char.IsWhiteSpace(_currentChar) || _currentChar == ',')) { }
            return isNotEnd;
        }
        private bool Increment()
        {
            if (_jsonLength > 1)
            {
                _jsonPtr++;
                _jsonLength--;
                _currentChar = _jsonPtr[0];
                return true;
            }
            return false;
        }
        private void Deincrement()
        {
            _jsonPtr--;
            _jsonLength++;
            _currentChar = _jsonPtr[0];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsEnterChar(char chr) { return chr == '{' || chr == '['; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsExitChar(char chr) { return chr == '}' || chr == ']'; }
    }
    internal readonly unsafe ref struct JsonNode
    {
        public readonly JsonNodeType Type;
        public readonly char* Ptr;
        public readonly int Length;
        public readonly int Depth;
        public bool IsEnter
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Type == JsonNodeType.Enter; }
        }
        public bool IsExit
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Type == JsonNodeType.Exit; }
        }
        public bool IsKey
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Type == JsonNodeType.Key; }
        }
        public bool IsValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Type == JsonNodeType.Value; }
        }
        public bool IsNotBreak
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Type != JsonNodeType.NA; }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JsonNode(JsonNodeType type, char* ptr, int length, int depth)
        {
            Type = type;
            Ptr = ptr;
            Length = length;
            Depth = depth;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() { return new string(Ptr, 0, Length); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EqualsString(string str)
        {
            if (str.Length != Length) { return false; }
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] != Ptr[i]) { return false; }
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(JsonNode node) { return node.IsNotBreak; }
    }
    internal enum JsonNodeType : byte
    {
        NA = 0,
        Key = 1, Value = 2,
        Enter = 3, Exit = 4,
    }
}