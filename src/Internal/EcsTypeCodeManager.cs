#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS.Internal
{
    //TODO разработать возможность ручного устанавливания ID типам.
    //это может быть полезно как детерминированность для сети
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal static class EcsTypeCodeManager
    {
        private static readonly Dictionary<EcsTypeCodeKey, EcsTypeCode> _codes = new Dictionary<EcsTypeCodeKey, EcsTypeCode>();
        private static int _increment = 1;
        private static readonly object _lock = new object();
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
        public static EcsTypeCodeKey FindTypeOfCode(EcsTypeCode typeCode)
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
        public EcsTypeCodeKey type;
        public EcsTypeCode code;
        public TypeCodeInfo(EcsTypeCodeKey type, EcsTypeCode code)
        {
            this.type = type;
            this.code = code;
        }
        public override string ToString()
        {
            return this.AutoToString(false);
        }
    }
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    internal readonly struct EcsTypeCodeKey : IEquatable<EcsTypeCodeKey>
    {
        public readonly Type Type;
        public readonly string NameKey;
        public EcsTypeCodeKey(Type type, string nameKey)
        {
            Type = type;
            NameKey = nameKey;
        }
        public bool Equals(EcsTypeCodeKey other)
        {
            return Type == other.Type && NameKey == other.NameKey;
        }
        public override bool Equals(object obj)
        {
            return obj is EcsTypeCodeKey other && Equals(other);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Type, NameKey);
        }
        public override string ToString()
        {
            if (string.IsNullOrEmpty(NameKey))
            {
                return Type.ToString();
            }
            return $"{Type} {NameKey}";
        }
        public static implicit operator EcsTypeCodeKey(Type type) { return new EcsTypeCodeKey(type, string.Empty); }
    }
}