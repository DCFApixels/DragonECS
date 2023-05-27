using System;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class DebugNameAttribute : Attribute
    {
        public readonly string name;
        public DebugNameAttribute(string name) => this.name = name;
    }
}
