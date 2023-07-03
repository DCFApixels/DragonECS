using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    namespace Internal
    {
        internal static class EcsTypeCode
        {
            private static readonly Dictionary<Type, int> _codes = new Dictionary<Type, int>();
            private static int _incremetn = 1;
            public static int Count => _codes.Count;
            public static int Get(Type type)
            {
                if (!_codes.TryGetValue(type, out int code))
                {
                    code = _incremetn++;
                    _codes.Add(type, code);
                }
                return code;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Get<T>() => Cache<T>.code;
            private static class Cache<T>
            {
                public static readonly int code = Get(typeof(T));
            }
        }
    }
}
