#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Internal;
using System;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public sealed class AllowedInWorldsAttribute : Attribute
    {
        public object[] AllowedWorlds;
        public AllowedInWorldsAttribute(params object[] allowedWorlds)
        {
            AllowedWorlds = allowedWorlds;
        }


        public static void CheckAllows<T>(EcsWorld world)
        {
            Type componentType = typeof(T);
            Type worldType = world.GetType();
            if (componentType.TryGetAttribute(out AllowedInWorldsAttribute attribute))
            {
                foreach (var worldTag in attribute.AllowedWorlds)
                {
                    bool result = false;
                    if (worldTag is Type worldTypeTag)
                    {
                        result = worldTypeTag == worldType;
                    }
                    else
                    {
                        string worldStringTag = worldTag.ToString();
                        result = world.Name == worldStringTag;
                    }
                    if (result)
                    {
                        return;
                    }
                }
                throw new InvalidOperationException($"Using component {componentType.ToMeta().TypeName} is not allowed in the {worldType.ToMeta().TypeName} world.");
            }
        }
    }
}
