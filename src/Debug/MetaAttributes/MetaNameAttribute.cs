using System;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaNameAttribute : EcsMetaAttribute
    {
        public readonly string name;
        public MetaNameAttribute(string name) => this.name = name;
    }
}
