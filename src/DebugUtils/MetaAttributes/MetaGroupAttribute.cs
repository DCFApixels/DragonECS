using DCFApixels.DragonECS.Core;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DCFApixels.DragonECS
{
    using static MetaGroupAttribute;

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaGroupAttribute : EcsMetaAttribute
    {
        public const char SEPARATOR = '/';
        public readonly MetaGroup Data;

        [Obsolete(EcsMetaAttributeHalper.EMPTY_NO_SENSE_MESSAGE)]
        public MetaGroupAttribute() { }
        public MetaGroupAttribute(string name)
        {
            Data = new MetaGroup(name);
        }
        //public MetaGroupAttribute(string name0, string name1) : this($"{name0}/{name1}") { }
        //public MetaGroupAttribute(string name0, string name1, string name2) : this($"{name0}/{name1}/{name2}") { }
        //public MetaGroupAttribute(string name0, string name1, string name2, string name3) : this($"{name0}/{name1}/{name2}/{name3}") { }
        //public MetaGroupAttribute(string name0, string name1, string name2, string name3, string name4) : this($"{name0}/{name1}/{name2}/{name3}/{name4}") { }
        //public MetaGroupAttribute(string name0, string name1, string name2, string name3, string name4, string name5) : this($"{name0}/{name1}/{name2}/{name3}/{name4}/{name5}") { }
        public MetaGroupAttribute(params string[] path) : this(string.Join(SEPARATOR, path)) { }
    }
    public class MetaGroup
    {
        public static readonly MetaGroup Empty = new MetaGroup("");
        private static string _pattern = @"Module(?=/)";

        public readonly string Name;
        private string[] _path = null;
        public IReadOnlyCollection<string> Splited
        {
            get
            {
                if (_path == null)
                {
                    _path = EcsMetaAttributeHalper.Split(SEPARATOR, Name);
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
            name = name.Replace('\\', SEPARATOR);
            if (name[name.Length - 1] != SEPARATOR)
            {
                name += SEPARATOR;
            }
            Name = Regex.Replace(name, _pattern, "");
            Name = string.Intern(Name);
        }
        public override string ToString() { return Name; }
    }
}
