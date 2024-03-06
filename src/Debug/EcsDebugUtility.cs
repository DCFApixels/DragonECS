using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DCFApixels.DragonECS
{
    public static class EcsDebugUtility
    {
        private const BindingFlags RFL_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Dictionary<Type, TypeMeta> _metaCache = new Dictionary<Type, TypeMeta>();

        #region GetGenericTypeName
        public static string GetGenericTypeFullName<T>(int maxDepth = 2)
        {
            return GetGenericTypeFullName(typeof(T), maxDepth);
        }
        public static string GetGenericTypeFullName(Type type, int maxDepth = 2)
        {
            return GetGenericTypeNameInternal(type, maxDepth, true);
        }
        public static string GetGenericTypeName<T>(int maxDepth = 2)
        {
            return GetGenericTypeName(typeof(T), maxDepth);
        }
        public static string GetGenericTypeName(Type type, int maxDepth = 2)
        {
            return GetGenericTypeNameInternal(type, maxDepth, false);
        }
        private static string GetGenericTypeNameInternal(Type type, int maxDepth, bool isFull)
        {
#if (DEBUG && !DISABLE_DEBUG)
            string typeName = isFull ? type.FullName : type.Name;
            if (!type.IsGenericType || maxDepth == 0)
            {
                return typeName;
            }
            int genericInfoIndex = typeName.LastIndexOf('`');
            if (genericInfoIndex > 0)
            {
                typeName = typeName.Remove(genericInfoIndex);
            }

            string genericParams = "";
            Type[] typeParameters = type.GetGenericArguments();
            for (int i = 0; i < typeParameters.Length; ++i)
            {
                //чтобы строка не была слишком длинной, используются сокращенные имена для типов аргументов
                string paramTypeName = GetGenericTypeNameInternal(typeParameters[i], maxDepth - 1, false);
                genericParams += (i == 0 ? paramTypeName : $", {paramTypeName}");
            }
            return $"{typeName}<{genericParams}>";
#else //optimization for release build
            return isFull ? type.FullName : type.Name;
#endif
        }
        #endregion

        #region AutoToString
        /// <summary> slow but automatic conversion of ValueType to string in the format "name(field1, field2... fieldn)" </summary>
        public static string AutoToString<T>(this T self, bool isWriteName = true) where T : struct
        {
            return AutoToString(self, typeof(T), isWriteName);
        }
        private static string AutoToString(object target, Type type, bool isWriteName)
        {
#if !REFLECTION_DISABLED
            //TODO сделать специальный вывод в виде названий констант для Enum-ов
#pragma warning disable IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
            var fields = type.GetFields(RFL_FLAGS);
#pragma warning restore IL2070
            string[] values = new string[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                values[i] = (fields[i].GetValue(target) ?? "NULL").ToString();
            }
            if (isWriteName)
            {
                return $"{type.Name}({string.Join(", ", values)})";
            }
            else
            {
                return $"({string.Join(", ", values)})";
            }
#else
            EcsDebug.PrintWarning($"Reflection is not available, the {nameof(AutoToString)} method does not work.");
            return string.Empty;
#endif
        }
        #endregion

        #region GetName
        public static string GetMetaName(object obj)
        {
            return GetTypeMeta(obj).Name;
        }
        public static string GetMetaName<T>()
        {
            return GetTypeMeta<T>().Name;
        }
        public static string GetMetaName(Type type)
        {
            return GetTypeMeta(type).Name;
        }

        public static bool TryGetMetaName(object obj, out string name)
        {
            TypeMeta meta = GetTypeMeta(obj);
            name = meta.Name;
            return meta.IsCustomName;
        }
        public static bool TryGetMetaName<T>(out string name)
        {
            TypeMeta meta = GetTypeMeta<T>();
            name = meta.Name;
            return meta.IsCustomName;
        }
        public static bool TryGetMetaName(Type type, out string name)
        {
            TypeMeta meta = GetTypeMeta(type);
            name = meta.Name;
            return meta.IsCustomName;
        }
        #endregion

        #region GetColor
        public static MetaColor GetColor(object obj)
        {
            return GetTypeMeta(obj).Color;
        }
        public static MetaColor GetColor<T>()
        {
            return GetTypeMeta<T>().Color;
        }
        public static MetaColor GetColor(Type type)
        {
            return GetTypeMeta(type).Color;
        }

        public static bool TryGetColor(object obj, out MetaColor color)
        {
            TypeMeta meta = GetTypeMeta(obj);
            color = meta.Color;
            return meta.IsCustomColor;
        }
        public static bool TryGetColor<T>(out MetaColor color)
        {
            TypeMeta meta = GetTypeMeta<T>();
            color = meta.Color;
            return meta.IsCustomColor;
        }
        public static bool TryGetColor(Type type, out MetaColor color)
        {
            TypeMeta meta = GetTypeMeta(type);
            color = meta.Color;
            return meta.IsCustomColor;
        }
        #endregion

        #region GetDescription
        public static string GetDescription(object obj)
        {
            return GetTypeMeta(obj).Description;
        }
        public static string GetDescription<T>()
        {
            return GetTypeMeta<T>().Description;
        }
        public static string GetDescription(Type type)
        {
            return GetTypeMeta(type).Description;
        }

        public static bool TryGetDescription(object obj, out string description)
        {
            TypeMeta meta = GetTypeMeta(obj);
            description = meta.Description;
            return string.IsNullOrEmpty(description);
        }
        public static bool TryGetDescription<T>(out string description)
        {
            TypeMeta meta = GetTypeMeta<T>();
            description = meta.Description;
            return string.IsNullOrEmpty(description);
        }
        public static bool TryGetDescription(Type type, out string description)
        {
            TypeMeta meta = GetTypeMeta(type);
            description = meta.Description;
            return string.IsNullOrEmpty(description);
        }
        #endregion

        #region GetGroup
        public static MetaGroup GetGroup(object obj)
        {
            return GetTypeMeta(obj).Group;
        }
        public static MetaGroup GetGroup<T>()
        {
            return GetTypeMeta<T>().Group;
        }
        public static MetaGroup GetGroup(Type type)
        {
            return GetTypeMeta(type).Group;
        }

        public static bool TryGetGroup(object obj, out MetaGroup group)
        {
            TypeMeta meta = GetTypeMeta(obj);
            group = meta.Group;
            return group.IsNull;
        }
        public static bool TryGetGroup<T>(out MetaGroup group)
        {
            TypeMeta meta = GetTypeMeta<T>();
            group = meta.Group;
            return group.IsNull;
        }
        public static bool TryGetGroup(Type type, out MetaGroup group)
        {
            TypeMeta meta = GetTypeMeta(type);
            group = meta.Group;
            return group.IsNull;
        }
        #endregion

        #region GetTags
        public static IReadOnlyCollection<string> GetTags(object obj)
        {
            return GetTypeMeta(obj).Tags;
        }
        public static IReadOnlyCollection<string> GetTags<T>()
        {
            return GetTypeMeta<T>().Tags;
        }
        public static IReadOnlyCollection<string> GetTags(Type type)
        {
            return GetTypeMeta(type).Tags;
        }

        public static bool TryGetTags(object obj, out IReadOnlyCollection<string> tags)
        {
            TypeMeta meta = GetTypeMeta(obj);
            tags = meta.Tags;
            return tags.Count <= 0;
        }
        public static bool TryGetTags<T>(out IReadOnlyCollection<string> tags)
        {
            TypeMeta meta = GetTypeMeta<T>();
            tags = meta.Tags;
            return tags.Count <= 0;
        }
        public static bool TryGetTags(Type type, out IReadOnlyCollection<string> tags)
        {
            TypeMeta meta = GetTypeMeta(type);
            tags = meta.Tags;
            return tags.Count <= 0;
        }
        #endregion

        #region IsHidden
        public static bool IsHidden(object obj)
        {
            return GetTypeMeta(obj).IsHidden;
        }
        public static bool IsHidden<T>()
        {
            return GetTypeMeta<T>().IsHidden;
        }
        public static bool IsHidden(Type type)
        {
            return GetTypeMeta(type).IsHidden;
        }
        #endregion

        #region GetTypeMeta
        public static TypeMeta GetTypeMeta(object obj)
        {
            return GetTypeMeta(GetTypeMetaSource(obj).GetType());
        }
        public static TypeMeta GetTypeMeta<T>()
        {
            return GetTypeMeta(typeof(T));
        }
        public static TypeMeta GetTypeMeta(Type type)
        {
            if (_metaCache.TryGetValue(type, out TypeMeta result) == false)
            {
                result = new TypeMeta(type);
                _metaCache.Add(type, result);
            }
            return result;
        }
        #endregion

        #region TypeMetaSource
        public static bool IsTypeMetaProvided(object obj)
        {
            return obj is IEcsTypeMetaProvider;
        }
        public static object GetTypeMetaSource(object obj)
        {
            return obj is IEcsTypeMetaProvider intr ? intr.MetaSource : obj;
        }
        #endregion
    }

    public interface ITypeMeta
    {
        public string Name { get; }
        public MetaColor Color { get; }
        public string Description { get; }
        public MetaGroup Group { get; }
        public IReadOnlyCollection<string> Tags { get; }
    }
    public sealed class TypeMeta : ITypeMeta
    {
        internal readonly Type _type;

        private bool _isCustomName;
        private bool _isCustomColor;
        private bool _isHidden;

        private string _name;
        private MetaColor _color;
        private string _description;
        private MetaGroup _group;
        private IReadOnlyCollection<string> _tags;
        private int _typeCode;

        private InitFlag _initFlags = InitFlag.None;

        #region Constructors
        public TypeMeta(Type type)
        {
            _type = type;
        }
        #endregion

        #region Type
        public Type Type
        {
            get { return _type; }
        }
        #endregion

        #region Name
        private void InitName()
        {
            if (_initFlags.HasFlag(InitFlag.Name) == false)
            {
                (_name, _isCustomName) = MetaCacheGenerator.GetName(_type);
                _initFlags |= InitFlag.Name;
            }
        }
        public bool IsCustomName
        {
            get
            {
                InitName();
                return _isCustomName;
            }
        }
        public string Name
        {
            get
            {
                InitName();
                return _name;
            }
        }
        #endregion

        #region Color
        private void InitColor()
        {
            if (_initFlags.HasFlag(InitFlag.Color) == false)
            {
                (_color, _isCustomColor) = MetaCacheGenerator.GetColor(_type);
                _initFlags |= InitFlag.Color;
            }
        }
        public bool IsCustomColor
        {
            get
            {
                InitColor();
                return _isCustomColor;
            }
        }
        public MetaColor Color
        {
            get
            {
                InitColor();
                return _color;
            }
        }
        #endregion

        #region Description
        public string Description
        {
            get
            {
                if (_initFlags.HasFlag(InitFlag.Description) == false)
                {
                    _description = MetaCacheGenerator.GetDescription(_type);
                    _initFlags |= InitFlag.Description;
                }
                return _description;
            }
        }
        #endregion

        #region Group
        public MetaGroup Group
        {
            get
            {
                if (_initFlags.HasFlag(InitFlag.Group) == false)
                {
                    _group = MetaCacheGenerator.GetGroup(_type);
                    _initFlags |= InitFlag.Group;
                }
                return _group;
            }
        }
        #endregion

        #region Tags
        private void InitTags()
        {
            if (_initFlags.HasFlag(InitFlag.Tags) == false)
            {
                _tags = MetaCacheGenerator.GetTags(_type);
                _initFlags |= InitFlag.Tags;
                _isHidden = _tags.Contains(MetaTags.HIDDEN);
            }
        }
        public IReadOnlyCollection<string> Tags
        {
            get
            {
                InitTags();
                return _tags;
            }
        }
        public bool IsHidden
        {
            get
            {
                InitTags();
                return _isHidden;
            }
        }
        #endregion

        #region TypeCode
        public int TypeCode
        {
            get
            {
                if (_initFlags.HasFlag(InitFlag.TypeCode) == false)
                {
                    _typeCode = EcsTypeCode.Get(_type);
                    _initFlags |= InitFlag.TypeCode;
                }
                return _typeCode;
            }
        }
        #endregion

        #region InitializeAll
        public void InitializeAll()
        {
            if (_initFlags == InitFlag.All)
            {
                return;
            }
            _ = Name;
            _ = Group;
            _ = Color;
            _ = Description;
            _ = Tags;
            _ = TypeCode;
        }
        #endregion

        #region InitFlag
        [Flags]
        private enum InitFlag : byte
        {
            None = 0,
            Name = 1 << 0,
            Group = 1 << 1,
            Color = 1 << 2,
            Description = 1 << 3,
            Tags = 1 << 4,
            TypeCode = 1 << 5,

            All = Name | Group | Color | Description | Tags | TypeCode
        }
        #endregion
    }

    public static class TypeMetaDataCachedExtensions
    {
        public static TypeMeta GetMeta(this object self)
        {
            return EcsDebugUtility.GetTypeMeta(self);
        }
        public static TypeMeta ToMeta(this Type self)
        {
            return EcsDebugUtility.GetTypeMeta(self);
        }
    }
}

namespace DCFApixels.DragonECS.Internal
{
    internal static class MetaCacheGenerator
    {
        private const int GENERIC_NAME_DEPTH = 2;

        #region GetName
        public static (string, bool) GetName(Type type)
        {
            bool isCustom = type.TryGetCustomAttribute(out MetaNameAttribute atr) && string.IsNullOrEmpty(atr.name) == false;
            return (isCustom ? atr.name : EcsDebugUtility.GetGenericTypeName(type, GENERIC_NAME_DEPTH), isCustom);
        }
        #endregion

        #region GetColor
        private static MetaColor AutoColor(Type type)
        {
            return new MetaColor(type.Name).Desaturate(0.48f) / 1.18f;
        }
        public static (MetaColor, bool) GetColor(Type type)
        {
            bool isCustom = type.TryGetCustomAttribute(out MetaColorAttribute atr);
            return (isCustom ? atr.color : AutoColor(type), isCustom);
        }
        #endregion

        #region GetGroup
        public static MetaGroup GetGroup(Type type)
        {
            return type.TryGetCustomAttribute(out MetaGroupAttribute atr) ? atr.Data : MetaGroup.Empty;
        }
        #endregion

        #region GetDescription
        public static string GetDescription(Type type)
        {
            bool isCustom = type.TryGetCustomAttribute(out MetaDescriptionAttribute atr);
            return isCustom ? atr.description : string.Empty;
        }
        #endregion

        #region GetTags
        public static IReadOnlyCollection<string> GetTags(Type type)
        {
            var atr = type.GetCustomAttribute<MetaTagsAttribute>();
            return atr != null ? atr.Tags : Array.Empty<string>();
        }
        #endregion
    }
}