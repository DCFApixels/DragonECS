using System;
using System.Text.RegularExpressions;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class DebugGroupAttribute : Attribute
    {
        public static readonly DebugGroupAttribute Empty = new DebugGroupAttribute("");
        public readonly string name;
        public readonly string rootCategory;
        public DebugGroupAttribute(string name)
        {
            name = Regex.Replace(name, @"^[/|\\]+|[/|\\]+$", "");
            rootCategory = Regex.Match(name, @"^(.*?)[/\\]").Groups[1].Value;
            this.name = name;
        }
        public string[] SplitCategories()
        {
            return Regex.Split(name, @"[/|\\]");
        }
        public DebugGroup GetData() => new DebugGroup(this);
    }
    public readonly struct DebugGroup
    {
        public static readonly DebugGroup Empty = new DebugGroup(DebugGroupAttribute.Empty);
        private readonly DebugGroupAttribute _source;
        public string Name => _source.name;
        public string RootCategory => _source.rootCategory;
        public DebugGroup(DebugGroupAttribute source) => _source = source;
        public string[] SplitCategories() => _source.SplitCategories();
    }
}
