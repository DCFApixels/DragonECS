#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class MetaProxyAttribute : DragonMetaAttribute
    {
        public Type Type;
        public bool ForDeclaringType;
        public MetaProxyAttribute(Type type, bool forDeclaringType = false)
        {
            Type = type;
            ForDeclaringType = forDeclaringType;
        }
    }
    public class MetaProxyBase
    {
        public static readonly MetaProxyBase EmptyProxy = new MetaProxyBase(typeof(void));
        public virtual string Name { get { return null; } }
        public virtual MetaColor? Color { get { return null; } }
        public virtual MetaDescription Description { get { return null; } }
        public virtual MetaGroup Group { get { return null; } }
        public virtual IEnumerable<string> Tags { get { return null; } }
        public MetaProxyBase(Type type) { }
    }
}