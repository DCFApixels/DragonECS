#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class MetaProxyAttribute : DragonMetaAttribute
    {
        public Type Type;
        public MetaProxyAttribute(Type type)
        {
            Type = type;
        }
    }
    public class MetaProxy
    {
        public static readonly MetaProxy EmptyProxy = new MetaProxy(typeof(void));
        public static TypeMeta EmptyMeta => TypeMeta.NullTypeMeta;
        public readonly Type Type;
        public virtual string Name { get { return null; } }
        public virtual MetaColor? Color { get { return null; } }
        public virtual MetaDescription Description { get { return null; } }
        public virtual MetaGroup Group { get { return null; } }
        public virtual IEnumerable<string> Tags { get { return null; } }
        public MetaProxy(Type type)
        {
            Type = type;
        }
    }
}