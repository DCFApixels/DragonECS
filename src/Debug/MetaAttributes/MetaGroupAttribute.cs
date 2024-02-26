using System;
using System.Text.RegularExpressions;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaGroupAttribute : Attribute
    {
        public static readonly MetaGroupAttribute Empty = new MetaGroupAttribute("");
        public readonly string name;
        public readonly string rootCategory;
        public MetaGroupAttribute(string name)
        {
            name = Regex.Replace(name, @"^[/|\\]+|[/|\\]+$", "");
            rootCategory = Regex.Match(name, @"^(.*?)[/\\]").Groups[1].Value;
            this.name = name;
        }
        public string[] SplitCategories()
        {
            return Regex.Split(name, @"[/|\\]");
        }
        public MetaGroup GetData() => new MetaGroup(this);
    }
    public readonly struct MetaGroup
    {
        public static readonly MetaGroup Empty = new MetaGroup(MetaGroupAttribute.Empty);
        private readonly MetaGroupAttribute _source;
        public string Name
        {
            get { return _source.name; }
        }
        public string RootCategory
        {
            get { return _source.rootCategory; }
        }
        public bool IsNull
        {
            get { return _source == null; }
        }
        public MetaGroup(MetaGroupAttribute source)
        {
            _source = source;
        }
        public string[] SplitCategories()
        {
            return _source.SplitCategories();
        }
    }
}
