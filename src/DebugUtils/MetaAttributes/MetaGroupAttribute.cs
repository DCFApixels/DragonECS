#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaGroupAttribute : EcsMetaAttribute
    {
        public const char SEPARATOR = MetaGroup.SEPARATOR;
        public readonly string Name = string.Empty;

        [Obsolete(EcsMetaAttributeHalper.EMPTY_NO_SENSE_MESSAGE)]
        public MetaGroupAttribute() { }
        public MetaGroupAttribute(string name) { Name = name; }
        public MetaGroupAttribute(params string[] path) { Name = string.Join(SEPARATOR, path); }
    }
    [DebuggerDisplay("{Name}")]
    public class MetaGroup
    {
        public const char SEPARATOR = '/';
        private const string SEPARATOR_STR = "/";
        public const string UNGROUPED = "<UNGROUPED>";
        private const string PATTERN = @"Module(?=/)";
        public static readonly MetaGroup Empty = new MetaGroup(UNGROUPED);

        public readonly string Name;
        private string[] _splited = null;
        public IReadOnlyCollection<string> Splited
        {
            get
            {
                if (_splited == null)
                {
                    _splited = EcsMetaAttributeHalper.Split(SEPARATOR, Name);
                }
                return _splited;
            }
        }
        public bool IsEmpty
        {
            get { return this == Empty; }
        }
        private MetaGroup(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Name = UNGROUPED;
                return;
            }
            name = Regex.Replace(name, @"(\s*[\/\\]+\s*)+", SEPARATOR_STR).Trim();
            if (name[name.Length - 1] != SEPARATOR)
            {
                name += SEPARATOR;
            }
            if (name[0] == SEPARATOR)
            {
                name = name.Substring(1);
            }
            Name = Regex.Replace(name, PATTERN, "");
            Name = string.Intern(Name);
        }
        public static MetaGroup FromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Empty;
            }
            return new MetaGroup(name);
        }
        public static MetaGroup FromNameSpace(Type type)
        {
            if (string.IsNullOrWhiteSpace(type.Namespace))
            {
                return Empty;
            }
            return new MetaGroup(type.Namespace.Replace('.', SEPARATOR));
        }
        public override string ToString() { return Name; }
    }
}
