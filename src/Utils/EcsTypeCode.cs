﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Internal
{
    public static class EcsTypeCode
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
        public static int Get<T>() => EcsTypeCodeCache<T>.code;
        public static bool Has(Type type) => _codes.ContainsKey(type);
        public static bool Has<T>() => _codes.ContainsKey(typeof(T));
        public static IEnumerable<TypeCodeInfo> GetDeclared() => _codes.Select(o => new TypeCodeInfo(o.Key, o.Value));
    }
    public static class EcsTypeCodeCache<T>
    {
        public static readonly int code = EcsTypeCode.Get(typeof(T));
    }
    public struct TypeCodeInfo
    {
        public Type type;
        public int code;
        public TypeCodeInfo(Type type, int code)
        {
            this.type = type;
            this.code = code;
        }

        public override string ToString()
        {
            return this.AutoToString(false) + "\n\r";
        }
    }
}