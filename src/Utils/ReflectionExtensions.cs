using System;
using System.Reflection;

namespace DCFApixels.DragonECS.Internal
{
    internal static class ReflectionExtensions
    {
        public static bool TryGetAttribute<T>(this MemberInfo self, out T attribute) where T : Attribute
        {
            attribute = self.GetCustomAttribute<T>();
            return attribute != null;
        }
    }
}
