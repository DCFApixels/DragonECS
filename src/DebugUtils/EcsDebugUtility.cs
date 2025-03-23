#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
#if DEBUG || !REFLECTION_DISABLED
using System.Reflection;
#endif

namespace DCFApixels.DragonECS
{
    public static class EcsDebugUtility
    {
#if DEBUG || !REFLECTION_DISABLED
        private const BindingFlags RFL_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
#endif

        #region GetGenericTypeName
        public static string GetGenericTypeFullName<T>(int maxDepth = 2)
        {
            return GetGenericTypeFullName(typeof(T), maxDepth);
        }
        public static string GetGenericTypeFullName(Type type, int maxDepth = 2)
        {
            return GetGenericTypeName_Internal(type, maxDepth, true);
        }
        public static string GetGenericTypeName<T>(int maxDepth = 2)
        {
            return GetGenericTypeName(typeof(T), maxDepth);
        }
        public static string GetGenericTypeName(Type type, int maxDepth = 2)
        {
            return GetGenericTypeName_Internal(type, maxDepth, false);
        }
        private static string GetGenericTypeName_Internal(Type type, int maxDepth, bool isFull)
        {
#if DEBUG || !REFLECTION_DISABLED //в дебажных утилитах REFLECTION_DISABLED только в релизном билде работает
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
                string paramTypeName = GetGenericTypeName_Internal(typeParameters[i], maxDepth - 1, false);
                genericParams += (i == 0 ? paramTypeName : $", {paramTypeName}");
            }
            return $"{typeName}<{genericParams}>";
#else
            EcsDebug.PrintWarning($"Reflection is not available, the {nameof(GetGenericTypeName_Internal)} method does not work.");
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

        internal static string AutoToString(object target, Type type, bool isWriteName)
        {
#if DEBUG || !REFLECTION_DISABLED //в дебажных утилитах REFLECTION_DISABLED только в релизном билде работает
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
        public static MetaDescription GetDescription(object obj)
        {
            return GetTypeMeta(obj).Description;
        }
        public static MetaDescription GetDescription<T>()
        {
            return GetTypeMeta<T>().Description;
        }
        public static MetaDescription GetDescription(Type type)
        {
            return GetTypeMeta(type).Description;
        }

        public static bool TryGetDescription(object obj, out MetaDescription description)
        {
            TypeMeta meta = GetTypeMeta(obj);
            description = meta.Description;
            return description != MetaDescription.Empty;
        }
        public static bool TryGetDescription<T>(out MetaDescription description)
        {
            TypeMeta meta = GetTypeMeta<T>();
            description = meta.Description;
            return description != MetaDescription.Empty;
        }
        public static bool TryGetDescription(Type type, out MetaDescription description)
        {
            TypeMeta meta = GetTypeMeta(type);
            description = meta.Description;
            return description != MetaDescription.Empty;
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
            return group != MetaGroup.Empty;
        }
        public static bool TryGetGroup<T>(out MetaGroup group)
        {
            TypeMeta meta = GetTypeMeta<T>();
            group = meta.Group;
            return group != MetaGroup.Empty;
        }
        public static bool TryGetGroup(Type type, out MetaGroup group)
        {
            TypeMeta meta = GetTypeMeta(type);
            group = meta.Group;
            return group != MetaGroup.Empty;
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
            if (obj == null) { return TypeMeta.NullTypeMeta; }
            return TypeMeta.Get(GetTypeMetaSource(obj).GetType());
        }
        public static TypeMeta GetTypeMeta<T>()
        {
            return TypeMeta.Get(typeof(T));
        }
        public static TypeMeta GetTypeMeta(Type type)
        {
            return TypeMeta.Get(type);
        }
        #endregion

        #region TypeMetaProvider
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

    public static class TypeMetaDataCachedExtensions
    {
        public static TypeMeta GetMeta(this object self)
        {
#if DEBUG && DRAGONECS_DEEP_DEBUG
            if (self is Type type) { Throw.DeepDebugException(); }
#endif
            return EcsDebugUtility.GetTypeMeta(self);
        }
        public static TypeMeta ToMeta(this Type self)
        {
            return EcsDebugUtility.GetTypeMeta(self);
        }
    }
}