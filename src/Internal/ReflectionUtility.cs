#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Core.Internal
{
    internal static class ReflectionUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetPureType(this Type type)
        {
            if (type.IsGenericType)
            {
                return type.GetGenericTypeDefinition();
            }
            return type;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetAttributeInherited<T>(this Type self, out T attribute, out Type declareAtrType) where T : Attribute
        {
            if (self == null || self == typeof(object))
            {
                attribute = null;
                declareAtrType = null;
                return false;
            }

            attribute = self.GetCustomAttribute<T>();
            if (attribute == null)
            {
                return self.BaseType.TryGetAttributeInherited<T>(out attribute, out declareAtrType);
            }
            declareAtrType = self;
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetAttribute<T>(this MemberInfo self, out T attribute) where T : Attribute
        {
            attribute = self.GetCustomAttribute<T>();
            return attribute != null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetAttribute<T>(this MemberInfo self, bool inherit, out T attribute) where T : Attribute
        {
            attribute = self.GetCustomAttribute<T>(inherit);
            return attribute != null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttribute<T>(this MemberInfo self) where T : Attribute
        {
            return self.GetCustomAttribute<T>() != null;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAttribute<T>(this MemberInfo self, bool inherit) where T : Attribute
        {
            return self.GetCustomAttribute<T>(inherit) != null;
        }
        public static bool IsCanInstantiated(this Type type)
        {
            return !type.IsAbstract && !type.IsInterface;
        }
    }
}
