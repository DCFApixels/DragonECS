using System;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaDescriptionAttribute : EcsMetaAttribute
    {
        public readonly string description;
        public MetaDescriptionAttribute(string description)
        {
            this.description = description;
        }
    }
}
