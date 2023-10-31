using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace DCFApixels.DragonECS
{
    public static class EcsDebugUtility
    {
        private const BindingFlags RFL_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private struct ProcessInterface
        {
            public Type interfaceType;
            public string processName;
            public ProcessInterface(Type interfaceType, string processName)
            {
                this.interfaceType = interfaceType;
                this.processName = processName;
            }
        }
        private static Dictionary<Type, ProcessInterface> _processes = new Dictionary<Type, ProcessInterface>();

        static EcsDebugUtility()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (!type.IsInterface)
                        continue;

                    if (type.GetInterface(nameof(IEcsProcess)) != null)
                    {
                        string name = type.Name;
                        if (name[0] == 'I' && name.Length > 1 && char.IsUpper(name[1]))
                            name = name.Substring(1);
                        name = Regex.Replace(name, @"\bEcs|Process\b", "");
                        _processes.Add(type, new ProcessInterface(type, name));
                    }
                }
            }
        }

        #region Process
        public static bool IsProcessInterface(Type type)
        {
            if (type.IsGenericType) type = type.GetGenericTypeDefinition();
            return _processes.ContainsKey(type);
        }
        public static string GetProcessInterfaceName(Type type)
        {
            if (type.IsGenericType) type = type.GetGenericTypeDefinition();
            return _processes[type].processName;
        }
        public static bool TryGetProcessInterfaceName(Type type, out string name)
        {
            if (type.IsGenericType) type = type.GetGenericTypeDefinition();
            bool result = _processes.TryGetValue(type, out ProcessInterface data);
            name = data.processName;
            return result;
        }
        #endregion

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
            var fields = type.GetFields(RFL_FLAGS);
            string[] values = new string[fields.Length];
            for (int i = 0; i < fields.Length; i++)
                values[i] = (fields[i].GetValue(target) ?? "NULL").ToString();
            if (isWriteName)
                return $"{type.Name}({string.Join(", ", values)})";
            else
                return $"({string.Join(", ", values)})";
        }
        #endregion

        #region GetName
        public static string GetName(object obj, int maxGenericDepth = 2)
        {
            return obj is IEcsDebugMetaProvider intr ?
                GetName(intr.DebugMetaSource, maxGenericDepth) :
                GetName(type: obj.GetType(), maxGenericDepth);
        }
        public static string GetName<T>(int maxGenericDepth = 2) => GetName(typeof(T), maxGenericDepth);
        public static string GetName(Type type, int maxGenericDepth = 2) => type.TryGetCustomAttribute(out DebugNameAttribute atr) ? atr.name : GetGenericTypeName(type, maxGenericDepth);
        public static bool TryGetCustomName(object obj, out string name)
        {
            return obj is IEcsDebugMetaProvider intr ? 
                TryGetCustomName(intr.DebugMetaSource, out name) : 
                TryGetCustomName(type: obj.GetType(), out name);
        }
        public static bool TryGetCustomName<T>(out string name) => TryGetCustomName(type: typeof(T), out name);
        public static bool TryGetCustomName(Type type, out string name)
        {
            if (type.TryGetCustomAttribute(out DebugNameAttribute atr))
            {
                name = atr.name;
                return true;
            }
            name = string.Empty;
            return false;
        }
        #endregion

        #region GetGroup
        public static DebugGroup GetGroup(object obj)
        {
            return obj is IEcsDebugMetaProvider intr ?
                GetGroup(intr.DebugMetaSource) :
                GetGroup(type: obj.GetType());
        }
        public static DebugGroup GetGroup<T>() => GetGroup(typeof(T));
        public static DebugGroup GetGroup(Type type) => type.TryGetCustomAttribute(out DebugGroupAttribute atr) ? atr.GetData() : DebugGroup.Empty;
        public static bool TryGetGroup(object obj, out DebugGroup group)
        {
            return obj is IEcsDebugMetaProvider intr ?
                TryGetGroup(intr.DebugMetaSource, out group) :
                TryGetGroup(type: obj.GetType(), out group);
        }
        public static bool TryGetGroup<T>(out DebugGroup text) => TryGetGroup(typeof(T), out text);
        public static bool TryGetGroup(Type type, out DebugGroup group)
        {
            if (type.TryGetCustomAttribute(out DebugGroupAttribute atr))
            {
                group = atr.GetData();
                return true;
            }
            group = DebugGroup.Empty;
            return false;
        }
        #endregion

        #region GetDescription
        public static string GetDescription(object obj)
        {
            return obj is IEcsDebugMetaProvider intr ?
                GetDescription(intr.DebugMetaSource) :
                GetDescription(type: obj.GetType());
        }
        public static string GetDescription<T>() => GetDescription(typeof(T));
        public static string GetDescription(Type type) => type.TryGetCustomAttribute(out DebugDescriptionAttribute atr) ? atr.description : string.Empty;
        public static bool TryGetDescription(object obj, out string text)
        {
            return obj is IEcsDebugMetaProvider intr ?
                TryGetDescription(intr.DebugMetaSource, out text) :
                TryGetDescription(type: obj.GetType(), out text);
        }
        public static bool TryGetDescription<T>(out string text) => TryGetDescription(typeof(T), out text);
        public static bool TryGetDescription(Type type, out string text)
        {
            if (type.TryGetCustomAttribute(out DebugDescriptionAttribute atr))
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
            public DebugColor color;
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
                        color.color = new DebugColor((byte)random.Next(), (byte)random.Next(), (byte)random.Next()).UpContrastColor() / 2;
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
            public DebugColor CalcColor()
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
                return new DebugColor((byte)r, (byte)g, (byte)b);
            }
        }
        private static Dictionary<Type, NameColor> _names = new Dictionary<Type, NameColor>();
        private static DebugColor CalcNameColorFor(Type type)
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

        public static DebugColor GetColor(object obj)
        {
            return obj is IEcsDebugMetaProvider intr ?
                GetColor(intr.DebugMetaSource) :
                GetColor(type: obj.GetType());
        }
        public static DebugColor GetColor<T>() => GetColor(typeof(T));
        public static DebugColor GetColor(Type type)
        {
            var atr = type.GetCustomAttribute<DebugColorAttribute>();
            return atr != null ? atr.color
#if DEBUG //optimization for release build
                : CalcNameColorFor(type);
#else
                : DebugColor.BlackColor;
#endif
        }
        public static bool TryGetColor(object obj, out DebugColor color)
        {
            return obj is IEcsDebugMetaProvider intr ?
                TryGetColor(intr.DebugMetaSource, out color) :
                TryGetColor(type: obj.GetType(), out color);
        }
        public static bool TryGetColor<T>(out DebugColor color) => TryGetColor(typeof(T), out color);
        public static bool TryGetColor(Type type, out DebugColor color)
        {
            var atr = type.GetCustomAttribute<DebugColorAttribute>();
            if (atr != null)
            {
                color = atr.color;
                return true;
            }
            color = DebugColor.BlackColor;
            return false;
        }
        #endregion

        #region IsHidden
        public static bool IsHidden(object obj)
        {
            return obj is IEcsDebugMetaProvider intr ?
                IsHidden(intr.DebugMetaSource) :
                IsHidden(type: obj.GetType());
        }
        public static bool IsHidden<T>() => IsHidden(typeof(T));
        public static bool IsHidden(Type type) => type.TryGetCustomAttribute(out DebugHideAttribute _);
        #endregion

        #region MetaSource
        public static bool IsMetaSourceProvided(object obj)
        {
            return obj is IEcsDebugMetaProvider;
        }
        public static object GetMetaSource(object obj)
        {
            return obj is IEcsDebugMetaProvider intr ? intr.DebugMetaSource : obj;
        }
        #endregion

        #region GenerateTypeDebugData
        public static TypeDebugData GenerateTypeDebugData(object obj)
        {
            return obj is IEcsDebugMetaProvider intr ?
                GenerateTypeDebugData(intr.DebugMetaSource) :
                GenerateTypeDebugData(type: obj.GetType());
        }
        public static TypeDebugData GenerateTypeDebugData<T>() => GenerateTypeDebugData(typeof(T));
        public static TypeDebugData GenerateTypeDebugData(Type type)
        {
            return new TypeDebugData(
                type,
                GetName(type),
                GetGroup(type),
                GetColor(type),
                GetDescription(type),
                IsHidden(type));
        }
        #endregion

        #region ReflectionExtensions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetCustomAttribute<T>(this Type self, out T attribute) where T : Attribute
        {
            attribute = self.GetCustomAttribute<T>();
            return attribute != null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetCustomAttribute<T>(this MemberInfo self, out T attribute) where T : Attribute
        {
            attribute = self.GetCustomAttribute<T>();
            return attribute != null;
        }
        #endregion
    }

    [Serializable]
    public sealed class TypeDebugData
    {
        public readonly Type type;
        public readonly string name;
        public readonly DebugGroup group;
        public readonly DebugColor color;
        public readonly string description;
        public readonly bool isHidden;
        public TypeDebugData(Type type, string name, DebugGroup group, DebugColor color, string description, bool isHidden)
        {
            this.type = type;
            this.name = name;
            this.group = group;
            this.color = color;
            this.description = description;
            this.isHidden = isHidden;
        }
    }
}
