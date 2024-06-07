using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaGroupAttribute : EcsMetaAttribute
    {
        public readonly MetaGroup Data;

        [Obsolete("With empty parameters, this attribute makes no sense.")]
        public MetaGroupAttribute() { }
        public MetaGroupAttribute(string name)
        {
            Data = new MetaGroup(name);
        }
        public MetaGroupAttribute(params string[] path)
        {
            Data = new MetaGroup(string.Join("/", path));
        }
    }
    public class MetaGroup
    {
        public static readonly MetaGroup Empty = new MetaGroup("");
        private static string _pattern = @"Module(?=/)";
        private static char[] _separatpor = new char[] { '/' };

        public readonly string Name;
        private string[] _path = null;
        public IReadOnlyCollection<string> Splited
        {
            get
            {
                if (_path == null)
                {
                    _path = Name.Split(_separatpor, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                }
                return _path;
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
            Name = Regex.Replace(name, _pattern, "");
            Name = string.Intern(Name);
        }
        public override string ToString() { return Name; }
    }
}
