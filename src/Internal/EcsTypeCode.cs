using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Internal
{
    //TODO разработать возможность ручного устанавливания ID типам.
    //это нужно для упрощения разработки сетевух
#if ENABLE_IL2CPP
    using Unity.IL2CPP.CompilerServices;
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal static class EcsTypeCode
    {
        private static readonly Dictionary<Type, int> _codes = new Dictionary<Type, int>();
        private static int _increment = 1;
        public static int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _codes.Count; }
        }
        public static int Get(Type type)
        {
            if (!_codes.TryGetValue(type, out int code))
            {
                code = _increment++;
                _codes.Add(type, code);
            }
            return code;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Get<T>() { return EcsTypeCodeCache<T>.code; }
        public static bool Has(Type type) { return _codes.ContainsKey(type); }
        public static bool Has<T>() { return _codes.ContainsKey(typeof(T)); }
        public static IEnumerable<TypeCodeInfo> GetDeclaredTypes() { return _codes.Select(o => new TypeCodeInfo(o.Key, o.Value)); }
    }
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal static class EcsTypeCodeCache<T>
    {
        public static readonly int code = EcsTypeCode.Get(typeof(T));
    }
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal struct TypeCodeInfo
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
            return this.AutoToString(false);
        }
    }
}