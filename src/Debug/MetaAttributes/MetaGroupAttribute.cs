using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaGroupAttribute : EcsMetaAttribute
    {
        public static readonly MetaGroupAttribute Empty = new MetaGroupAttribute("");
        public readonly string name;
        private string[] path = null;
        public IReadOnlyCollection<string> Splited
        {
            get
            {
                if (path == null)
                {
                    path = Regex.Split(name, @"[/|\\]");
                }
                return path;
            }
        }
        public MetaGroupAttribute(string name)
        {
            name = Regex.Replace(name, @"^[/|\\]+|[/|\\]+$", "");
            this.name = name;
        }
        public MetaGroup GetData()
        {
            return new MetaGroup(this);
        }
    }
    public readonly struct MetaGroup
    {
        public static readonly MetaGroup Empty = new MetaGroup(MetaGroupAttribute.Empty);
        private readonly MetaGroupAttribute _source;
        public string Name
        {
            get { return _source.name; }
        }
        public IReadOnlyCollection<string> Splited
        {
            get { return _source.Splited; }
        }
        public bool IsNull
        {
            get { return _source == null; }
        }
        public MetaGroup(MetaGroupAttribute source)
        {
            _source = source;
        }
    }
}
