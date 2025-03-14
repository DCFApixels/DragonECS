#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Internal
{
    internal static class ReflectionUtility
    {
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
    }
}
