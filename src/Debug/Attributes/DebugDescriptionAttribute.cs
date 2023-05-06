using System;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class DebugDescriptionAttribute : Attribute
    {
        public readonly string description;
        public DebugDescriptionAttribute(string description) 
        {
            this.description = description;
        }
    }
}
