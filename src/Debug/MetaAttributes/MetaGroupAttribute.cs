using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaGroupAttribute : EcsMetaAttribute
    {
        public readonly MetaGroupRef Data;

        [Obsolete("With empty parameters, this attribute makes no sense.")]
        public MetaGroupAttribute() { }
        public MetaGroupAttribute(string name)
        {
            Data = new MetaGroupRef(name);
        }
        public MetaGroupAttribute(params string[] path)
        {
            Data = new MetaGroupRef(string.Join("/", path));
        }
    }
    public class MetaGroupRef
    {
        public static readonly MetaGroupRef Empty = new MetaGroupRef("");

        public readonly string Name;
        private string[] path = null;
        private static string pattern = @"Module(?=/)";
        private static char[] separatpor = new char[] { '/' };
        public IReadOnlyCollection<string> Splited
        {
            get
            {
                if (path == null)
                {
                    path = Name.Split(separatpor, StringSplitOptions.RemoveEmptyEntries);
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
            Name = Regex.Replace(name, pattern, ""); ;
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
