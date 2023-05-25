using System;
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

        public static string GetDescription<T>() => GetDescription(typeof(T));
        public static string GetDescription(Type type)
        {
            var atr = type.GetCustomAttribute<DebugDescriptionAttribute>();
            return atr != null ? atr.description : string.Empty;
        }

        public static (byte, byte, byte) GetColorRGB<T>() => GetColorRGB(typeof(T));
        public static (byte, byte, byte) GetColorRGB(Type type)
        {
            var atr = type.GetCustomAttribute<DebugColorAttribute>();
            return atr != null ? (atr.r, atr.g, atr.b) : ((byte)255, (byte)255, (byte)255);
        }
    }
}
