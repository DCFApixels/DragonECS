using System;
using System.Collections.Generic;
using System.Reflection;

namespace DCFApixels.DragonECS
{
    public static class EcsDebugUtility
    {
        public static string GetGenericTypeFullName<T>(int maxDepth = 2) => GetGenericTypeFullName(typeof(T), maxDepth);
        public static string GetGenericTypeFullName(Type type, int maxDepth = 2) => GetGenericTypeNameInternal(type, maxDepth, true);
        public static string GetGenericTypeName<T>(int maxDepth = 2) => GetGenericTypeName(typeof(T), maxDepth);
        public static string GetGenericTypeName(Type type, int maxDepth = 2) => GetGenericTypeNameInternal(type, maxDepth, false);
        private static string GetGenericTypeNameInternal(Type type, int maxDepth, bool isFull)
        {
#if (DEBUG && !DISABLE_DEBUG)
            string friendlyName = isFull ? type.FullName : type.Name;
            if (!type.IsGenericType || maxDepth == 0)
                return friendlyName;

            int iBacktick = friendlyName.IndexOf('`');
            if (iBacktick > 0)
                friendlyName = friendlyName.Remove(iBacktick);

            friendlyName += "<";
            Type[] typeParameters = type.GetGenericArguments();
            for (int i = 0; i < typeParameters.Length; ++i)
            {
                string typeParamName = GetGenericTypeNameInternal(typeParameters[i], maxDepth - 1, false);//чтобы строка не была слишком длинной, используются сокращенные имена для типов аргументов
                friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
            }
            friendlyName += ">";
            return friendlyName;
#else //optimization for release build
            return type.Name;
#endif
        }

        public static string GetName<T>() => GetName(typeof(T));
        public static string GetName(Type type)
        {
            var atr = type.GetCustomAttribute<DebugNameAttribute>();
            return atr != null ? atr.name : GetGenericTypeName(type);
        }
        public static bool TryGetCustomName<T>(out string name) => TryGetCustomName(typeof(T), out name);
        public static bool TryGetCustomName(Type type, out string name)
        {
            var atr = type.GetCustomAttribute<DebugNameAttribute>();
            if (atr != null)
            {
                name = atr.name;
                return true;
            }
            name = string.Empty;
            return false;
        }

        public static string GetDescription<T>() => GetDescription(typeof(T));
        public static string GetDescription(Type type)
        {
            var atr = type.GetCustomAttribute<DebugDescriptionAttribute>();
            return atr != null ? atr.description : string.Empty;
        }
        public static bool TryGetDescription<T>(out string text) => TryGetDescription(typeof(T), out text);
        public static bool TryGetDescription(Type type, out string text)
        {
            var atr = type.GetCustomAttribute<DebugDescriptionAttribute>();
            if (atr != null)
            {
                text = atr.description;
                return true;
            }
            text = string.Empty;
            return false;
        }

        #region GetColor
        private static Random random = new Random();
        private static Dictionary<string, WordColor> _words = new Dictionary<string, WordColor>();
        private class WordColor
        {
            public int wordsCount;
            public DebugColorAttribute.Color color;
        }
        private class NameColor
        {
            public List<WordColor> colors = new List<WordColor>();
            public NameColor(IEnumerable<string> nameWords)
            {
                foreach (var word in nameWords)
                {
                    if(!_words.TryGetValue(word, out WordColor color))
                    {
                        color = new WordColor();
                        _words.Add(word, color);
                        color.color = new DebugColorAttribute.Color((byte)random.Next(), (byte)random.Next(), (byte)random.Next()).UpContrastColor() / 2;
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
            public DebugColorAttribute.Color CalcColor()
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
                return new DebugColorAttribute.Color((byte)r, (byte)g, (byte)b);
            }
        }
        private static Dictionary<Type, NameColor> _names = new Dictionary<Type, NameColor>();
        private static DebugColorAttribute.Color CalcNameColorFor(Type type)
        {
            Type targetType = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            if(!_names.TryGetValue(targetType, out NameColor nameColor))
            {
                nameColor = new NameColor(SplitString(targetType.Name));
                _names.Add(targetType, nameColor);
            }
            return nameColor.CalcColor();
        }
        public static List<string> SplitString(string s)
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

        public static (byte, byte, byte) GetColorRGB<T>() => GetColorRGB(typeof(T));
        public static (byte, byte, byte) GetColorRGB(Type type)
        {
            var atr = type.GetCustomAttribute<DebugColorAttribute>();
            return atr != null ? (atr.r, atr.g, atr.b)
#if DEBUG //optimization for release build
                : CalcNameColorFor(type).ToTuple();
#else 
                : ((byte)0, (byte)0, (byte)0);
#endif
        }
        public static bool TryGetColorRGB<T>(out (byte, byte, byte) color) => TryGetColorRGB(typeof(T), out color);
        public static bool TryGetColorRGB(Type type, out (byte, byte, byte) color)
        {
            var atr = type.GetCustomAttribute<DebugColorAttribute>();
            if(atr != null)
            {
                color = (atr.r, atr.g, atr.b);
                return true;
            }
            color = ((byte)0, (byte)0, (byte)0);
            return false;
        }
        #endregion

        public static bool IsHidden<T>() => IsHidden(typeof(T));
        public static bool IsHidden(Type type)
        {
            return type.GetCustomAttribute<DebugHideAttribute>() != null;
        }
    }
}
