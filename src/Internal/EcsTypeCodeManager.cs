using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS.Internal
{
    //TODO разработать возможность ручного устанавливания ID типам.
    //это нужно для упрощения разработки сетевух
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal static class EcsTypeCodeManager
    {
        private static readonly Dictionary<Type, EcsTypeCode> _codes = new Dictionary<Type, EcsTypeCode>();
        private static int _increment = 1;
        private static object _lock = new object();
        public static int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _codes.Count; }
        }
        public static EcsTypeCode Get(Type type)
        {
            lock (_lock)
            {
                if (!_codes.TryGetValue(type, out EcsTypeCode code))
                {
                    code = (EcsTypeCode)_increment++;
                    _codes.Add(type, code);
                }
                return code;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsTypeCode Get<T>() { return EcsTypeCodeCache<T>.code; }
        public static bool Has(Type type) { return _codes.ContainsKey(type); }
        public static bool Has<T>() { return _codes.ContainsKey(typeof(T)); }
        public static Type FindTypeOfCode(EcsTypeCode typeCode)
        {
            foreach (var item in _codes)
            {
                if (item.Value == typeCode)
                {
                    return item.Key;
                }
            }
            return null;
        }
        public static IEnumerable<TypeCodeInfo> GetDeclaredTypes() { return _codes.Select(o => new TypeCodeInfo(o.Key, o.Value)); }
    }
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal static class EcsTypeCodeCache<T>
    {
        public static readonly EcsTypeCode code = EcsTypeCodeManager.Get(typeof(T));
    }
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal struct TypeCodeInfo
    {
        public Type type;
        public EcsTypeCode code;
        public TypeCodeInfo(Type type, EcsTypeCode code)
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