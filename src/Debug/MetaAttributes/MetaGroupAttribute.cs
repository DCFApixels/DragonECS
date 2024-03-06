using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaGroupAttribute : EcsMetaAttribute
    {
        public readonly MetaGroup Data;
        public MetaGroupAttribute(string name)
        {
            Data = new MetaGroup(name);
        }
    }
    public class MetaGroup
    {
        public static readonly MetaGroup Empty = new MetaGroup("");

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
        public MetaGroup(string name)
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

    public readonly struct ReadonlyMetaGroup
    {
        public static readonly ReadonlyMetaGroup Empty = new ReadonlyMetaGroup(MetaGroup.Empty);
        private readonly MetaGroup _source;
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
        public ReadonlyMetaGroup(MetaGroup source)
        {
            _source = source;
        }

        public static implicit operator ReadonlyMetaGroup(MetaGroup group)
        {
            return new ReadonlyMetaGroup(group);
        }
    }
}
