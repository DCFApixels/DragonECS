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
        public const char SEPARATOR = '/';
        public readonly MetaGroup Data;

        [Obsolete(EcsMetaAttributeHalper.EMPTY_NO_SENSE_MESSAGE)]
        public MetaGroupAttribute() { Data = MetaGroup.Empty; }
        public MetaGroupAttribute(string name) { Data = new MetaGroup(name); }
        public MetaGroupAttribute(params string[] path) : this(string.Join(SEPARATOR, path)) { }
    }
    [DebuggerDisplay("{Name}")]
    public class MetaGroup
    {
        public const char SEPARATOR = MetaGroupAttribute.SEPARATOR;
        public const string UNGROUPED = "<UNGROUPED>";
        private const string PATTERN = @"Module(?=/)";
        public static readonly MetaGroup Empty = new MetaGroup(UNGROUPED);

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
        public bool IsEmpty
        {
            get { return this == Empty; }
        }
        public MetaGroup(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Name = UNGROUPED;
                return;
            }
            name = name.Replace('\\', SEPARATOR);
            if (name[name.Length - 1] != SEPARATOR)
            {
                name += SEPARATOR;
            }
            Name = Regex.Replace(name, PATTERN, "");
            Name = string.Intern(Name);
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
