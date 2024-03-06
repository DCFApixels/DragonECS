using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaGroupAttribute : EcsMetaAttribute
    {
        public readonly MetaGroupRef Data;
        public MetaGroupAttribute(string name)
        {
            Data = new MetaGroupRef(name);
        }
    }
    public class MetaGroupRef
    {
        public static readonly MetaGroupRef Empty = new MetaGroupRef("");

        public readonly string Name;
        private string[] path = null;
        public IReadOnlyCollection<string> Splited
        {
            get
            {
                if (path == null)
                {
                    path = Name.Split('/', StringSplitOptions.RemoveEmptyEntries);
                }
                return path;
            }
        }
        public MetaGroupRef(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Name = string.Empty;
                return;
            }
            name = name.Replace('\\', '/');
            if (name[name.Length - 1] != '/')
            {
                name += '/';
            }
            Name = name;
        }
    }

    public readonly struct MetaGroup
    {
        public static readonly MetaGroup Empty = new MetaGroup(MetaGroupRef.Empty);
        private readonly MetaGroupRef _source;
        public string Name
        {
            get { return _source.Name; }
        }
        public IReadOnlyCollection<string> Splited
        {
            get { return _source.Splited; }
        }
        public bool IsNull
        {
            get { return _source == null; }
        }
        public MetaGroup(MetaGroupRef source)
        {
            _source = source;
        }

        public static implicit operator MetaGroup(MetaGroupRef group)
        {
            return new MetaGroup(group);
        }
    }
}
