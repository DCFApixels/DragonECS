using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public static class EcsDebugUtility
    {
        private const BindingFlags RFL_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        #region GetGenericTypeName
        public static string GetGenericTypeFullName<T>(int maxDepth = 2) => GetGenericTypeFullName(typeof(T), maxDepth);
        public static string GetGenericTypeFullName(Type type, int maxDepth = 2) => GetGenericTypeNameInternal(type, maxDepth, true);
        public static string GetGenericTypeName<T>(int maxDepth = 2) => GetGenericTypeName(typeof(T), maxDepth);
        public static string GetGenericTypeName(Type type, int maxDepth = 2) => GetGenericTypeNameInternal(type, maxDepth, false);
        private static string GetGenericTypeNameInternal(Type type, int maxDepth, bool isFull)
        {
#if (DEBUG && !DISABLE_DEBUG)
            string result = isFull ? type.FullName : type.Name;
            if (!type.IsGenericType || maxDepth == 0)
                return result;

            int iBacktick = result.IndexOf('`');
            if (iBacktick > 0)
                result = result.Remove(iBacktick);

            result += "<";
            Type[] typeParameters = type.GetGenericArguments();
            for (int i = 0; i < typeParameters.Length; ++i)
            {
                string typeParamName = GetGenericTypeNameInternal(typeParameters[i], maxDepth - 1, false);//чтобы строка не была слишком длинной, используются сокращенные имена для типов аргументов
                result += (i == 0 ? typeParamName : "," + typeParamName);
            }
            result += ">";
            return result;
#else //optimization for release build
            return type.Name;
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
                values[i] = (fields[i].GetValue(target) ?? "NULL").ToString();
            if (isWriteName)
                return $"{type.Name}({string.Join(", ", values)})";
            else
                return $"({string.Join(", ", values)})";
#else
            EcsDebug.PrintWarning($"Reflection is not available, the {nameof(AutoToString)} method does not work.");
            return string.Empty;
#endif
        }
        #endregion

        #region GetName
        public static string GetName(object obj, int maxGenericDepth = 2)
        {
            return obj is IEcsMetaProvider intr ?
                GetName(intr.MetaSource, maxGenericDepth) :
                GetName(type: obj.GetType(), maxGenericDepth);
        }
        public static string GetName<T>(int maxGenericDepth = 2) => GetName(typeof(T), maxGenericDepth);
        public static string GetName(Type type, int maxGenericDepth = 2) => type.TryGetCustomAttribute(out MetaNameAttribute atr) ? atr.name : GetGenericTypeName(type, maxGenericDepth);
        public static bool TryGetCustomName(object obj, out string name)
        {
            return obj is IEcsMetaProvider intr ?
                TryGetCustomName(intr.MetaSource, out name) :
                TryGetCustomName(type: obj.GetType(), out name);
        }
        public static bool TryGetCustomName<T>(out string name) => TryGetCustomName(type: typeof(T), out name);
        public static bool TryGetCustomName(Type type, out string name)
        {
            if (type.TryGetCustomAttribute(out MetaNameAttribute atr))
            {
                name = atr.name;
                return true;
            }
            name = string.Empty;
            return false;
        }
        #endregion

        #region GetGroup
        public static MetaGroup GetGroup(object obj)
        {
            return obj is IEcsMetaProvider intr ?
                GetGroup(intr.MetaSource) :
                GetGroup(type: obj.GetType());
        }
        public static MetaGroup GetGroup<T>() => GetGroup(typeof(T));
        public static MetaGroup GetGroup(Type type) => type.TryGetCustomAttribute(out MetaGroupAttribute atr) ? atr.GetData() : MetaGroup.Empty;
        public static bool TryGetGroup(object obj, out MetaGroup group)
        {
            return obj is IEcsMetaProvider intr ?
                TryGetGroup(intr.MetaSource, out group) :
                TryGetGroup(type: obj.GetType(), out group);
        }
        public static bool TryGetGroup<T>(out MetaGroup text) => TryGetGroup(typeof(T), out text);
        public static bool TryGetGroup(Type type, out MetaGroup group)
        {
            if (type.TryGetCustomAttribute(out MetaGroupAttribute atr))
            {
                group = atr.GetData();
                return true;
            }
            group = MetaGroup.Empty;
            return false;
        }
        #endregion

        #region GetDescription
        public static string GetDescription(object obj)
        {
            return obj is IEcsMetaProvider intr ?
                GetDescription(intr.MetaSource) :
                GetDescription(type: obj.GetType());
        }
        public static string GetDescription<T>() => GetDescription(typeof(T));
        public static string GetDescription(Type type) => type.TryGetCustomAttribute(out MetaDescriptionAttribute atr) ? atr.description : string.Empty;
        public static bool TryGetDescription(object obj, out string text)
        {
            return obj is IEcsMetaProvider intr ?
                TryGetDescription(intr.MetaSource, out text) :
                TryGetDescription(type: obj.GetType(), out text);
        }
        public static bool TryGetDescription<T>(out string text) => TryGetDescription(typeof(T), out text);
        public static bool TryGetDescription(Type type, out string text)
        {
            if (type.TryGetCustomAttribute(out MetaDescriptionAttribute atr))
            {
                text = atr.description;
                return true;
            }
            text = string.Empty;
            return false;
        }
        #endregion

        #region GetColor
        private static Random random = new Random(100100100);
        private static Dictionary<string, WordColor> _words = new Dictionary<string, WordColor>();
        private class WordColor
        {
            public int wordsCount;
            public MetaColor color;
        }
        private class NameColor
        {
            public List<WordColor> colors = new List<WordColor>();
            public NameColor(IEnumerable<string> nameWords)
            {
                foreach (var word in nameWords)
                {
                    if (!_words.TryGetValue(word, out WordColor color))
                    {
                        color = new WordColor();
                        _words.Add(word, color);
                        color.color = new MetaColor((byte)random.Next(), (byte)random.Next(), (byte)random.Next()).UpContrastColor() / 2;
                    }
                    color.wordsCount++;
                    colors.Add(color);
                }
            }
            private int CalcTotalWordsColor()
            {
                int result = 0;
                for (int i = 0, iMax = colors.Count; i < iMax; i++)
                {
                    result += colors[i].wordsCount;
                }
                return result;
            }
            public MetaColor CalcColor()
            {
                float r = 0, g = 0, b = 0;
                int totalWordsCount = CalcTotalWordsColor();
                for (int i = 0, iMax = colors.Count; i < iMax; i++)
                {
                    var color = colors[i];
                    float m = (float)color.wordsCount / totalWordsCount;
                    r += m * color.color.r;
                    g += m * color.color.g;
                    b += m * color.color.b;
                }
                return new MetaColor((byte)r, (byte)g, (byte)b);
            }
        }
        private static Dictionary<Type, NameColor> _names = new Dictionary<Type, NameColor>();
        private static MetaColor CalcNameColorFor(Type type)
        {
            Type targetType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            if (!_names.TryGetValue(targetType, out NameColor nameColor))
            {
                nameColor = new NameColor(SplitString(targetType.Name));
                _names.Add(targetType, nameColor);
            }
            return nameColor.CalcColor();
        }
        private static List<string> SplitString(string s)
        {
            string subs;
            List<string> words = new List<string>();
            int start = 0;
            for (int i = 1; i < s.Length; i++)
            {
                if (char.IsUpper(s[i]))
                {
                    subs = s.Substring(start, i - start);
                    if (subs.Length > 2 && subs.ToLower() != "system")
                        words.Add(subs);
                    start = i;
                }
            }
            subs = s.Substring(start);
            if (subs.Length > 2 && subs.ToLower() != "system")
                words.Add(subs);
            return words;
        }

        public static MetaColor GetColor(object obj)
        {
            return obj is IEcsMetaProvider intr ?
                GetColor(intr.MetaSource) :
                GetColor(type: obj.GetType());
        }
        public static MetaColor GetColor<T>() => GetColor(typeof(T));
        public static MetaColor GetColor(Type type)
        {
            var atr = type.GetCustomAttribute<MetaColorAttribute>();
            return atr != null ? atr.color
#if DEBUG //optimization for release build
                : CalcNameColorFor(type);
#else
                : MetaColor.BlackColor;
#endif
        }
        public static bool TryGetColor(object obj, out MetaColor color)
        {
            return obj is IEcsMetaProvider intr ?
                TryGetColor(intr.MetaSource, out color) :
                TryGetColor(type: obj.GetType(), out color);
        }
        public static bool TryGetColor<T>(out MetaColor color) => TryGetColor(typeof(T), out color);
        public static bool TryGetColor(Type type, out MetaColor color)
        {
            var atr = type.GetCustomAttribute<MetaColorAttribute>();
            if (atr != null)
            {
                color = atr.color;
                return true;
            }
            color = MetaColor.BlackColor;
            return false;
        }
        #endregion

        #region GetTags
        public static IReadOnlyCollection<string> GetTags(object obj)
        {
            return obj is IEcsMetaProvider intr ?
                GetTags(intr.MetaSource) :
                GetTags(type: obj.GetType());
        }
        public static IReadOnlyCollection<string> GetTags<T>() => GetTags(typeof(T));
        public static IReadOnlyCollection<string> GetTags(Type type)
        {
            var atr = type.GetCustomAttribute<MetaTagsAttribute>();
            return atr != null ? atr.Tags : Array.Empty<string>();
        }

        public static bool TryGetTags(object obj, out IReadOnlyCollection<string> tags)
        {
            return obj is IEcsMetaProvider intr ?
                TryGetTags(intr.MetaSource, out tags) :
                TryGetTags(type: obj.GetType(), out tags);
        }
        public static bool TryGetTags<T>(out IReadOnlyCollection<string> tags) => TryGetTags(typeof(T), out tags);
        public static bool TryGetTags(Type type, out IReadOnlyCollection<string> tags)
        {
            var atr = type.GetCustomAttribute<MetaTagsAttribute>();
            if (atr != null)
            {
                tags = atr.Tags;
                return true;
            }
            tags = Array.Empty<string>();
            return false;
        }
        #endregion

        #region IsHidden
        public static bool IsHidden(object obj)
        {
            return obj is IEcsMetaProvider intr ?
                IsHidden(intr.MetaSource) :
                IsHidden(type: obj.GetType());
        }
        public static bool IsHidden<T>() => IsHidden(typeof(T));
        public static bool IsHidden(Type type) => type.TryGetCustomAttribute(out MetaTagsAttribute atr) && atr.Tags.Contains(MetaTags.HIDDEN);
        #endregion

        #region MetaSource
        public static bool IsMetaSourceProvided(object obj)
        {
            return obj is IEcsMetaProvider;
        }
        public static object GetMetaSource(object obj)
        {
            return obj is IEcsMetaProvider intr ? intr.MetaSource : obj;
        }
        #endregion

        #region GenerateTypeDebugData
        public static TypeMetaData GenerateTypeDebugData(object obj)
        {
            return obj is IEcsMetaProvider intr ?
                GenerateTypeDebugData(intr.MetaSource) :
                GenerateTypeDebugData(type: obj.GetType());
        }
        public static TypeMetaData GenerateTypeDebugData<T>() => GenerateTypeDebugData(typeof(T));
        public static TypeMetaData GenerateTypeDebugData(Type type)
        {
            return new TypeMetaData(
                type,
                GetName(type),
                GetGroup(type),
                GetColor(type),
                GetDescription(type),
                GetTags(type).ToArray());
        }
        #endregion

        #region ReflectionExtensions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetCustomAttribute<T>(this Type self, out T attribute) where T : Attribute
        {
            attribute = self.GetCustomAttribute<T>();
            return attribute != null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetCustomAttribute<T>(this MemberInfo self, out T attribute) where T : Attribute
        {
            attribute = self.GetCustomAttribute<T>();
            return attribute != null;
        }
        #endregion
    }

    [Serializable]
    public sealed class TypeMetaData
    {
        public readonly Type type;
        public readonly string name;
        public readonly MetaGroup group;
        public readonly MetaColor color;
        public readonly string description;
        public readonly string[] tags;
        public TypeMetaData(Type type, string name, MetaGroup group, MetaColor color, string description, string[] tags)
        {
            this.type = type;
            this.name = name;
            this.group = group;
            this.color = color;
            this.description = description;
            this.tags = tags;
        }
    }
}
