using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    namespace Internal
    {
        internal static class EcsTypeCode
        {
            private static readonly Dictionary<Type, int> _codes = new Dictionary<Type, int>();
            private static int _incremetn = 1;
            public static int GetCode(Type type)
            {
                if (!_codes.TryGetValue(type, out int code))
                {
                    code = _incremetn++;
                    _codes.Add(type, code);
                }
                return code;
            }
            public static int Count => _codes.Count;
            internal static class Cache<T>
            {
                public static readonly int code = GetCode(typeof(T));
            }
        }
    }
}
