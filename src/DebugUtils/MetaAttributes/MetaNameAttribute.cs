#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using System;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaNameAttribute : EcsMetaAttribute
    {
        public readonly string name;
        public readonly bool isHideGeneric;
        public MetaNameAttribute(string name, bool isHideGeneric = false)
        {
            this.name = name;
            this.isHideGeneric = isHideGeneric;
        }
    }
}
