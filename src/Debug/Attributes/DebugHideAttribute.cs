using System;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
    public sealed class DebugHideAttribute : Attribute { }
}
