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
    public sealed class MetaGroupAttribute : DragonMetaAttribute
    {
        public const char SEPARATOR = MetaGroup.SEPARATOR;
        public readonly string Name = string.Empty;

        [Obsolete(DragonMetaAttributeHalper.EMPTY_NO_SENSE_MESSAGE)]
        public MetaGroupAttribute() { }
        public MetaGroupAttribute(string name) { Name = name; }
        public MetaGroupAttribute(params string[] path)
        {
            Name = path == null ? string.Empty : string.Join(SEPARATOR, path);
        }
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
        public ReadOnlySpan<string> Splited
        {
            get
            {
                if (_splited == null)
                {
                    _splited = DragonMetaAttributeHalper.Split(SEPARATOR, Name);
                }
                return _splited;
            }
        }
        public IReadOnlyCollection<string> SplitedEnumerable
        {
            get
            {
                if (_splited == null)
                {
                    _splited = DragonMetaAttributeHalper.Split(SEPARATOR, Name);
                }
                return _splited;
            }
        }
        public bool IsEmpty
        {
            get { return Name == UNGROUPED; }
        }
        private MetaGroup(string name)
        {
            if (string.IsNullOrEmpty(name) || name == UNGROUPED)
            {
                Name = UNGROUPED;
                return;
            }
            name = Regex.Replace(name, @"(\s*[\/\\]+\s*)+", SEPARATOR_STR).Trim().Trim(SEPARATOR);
            if (name.Length == 0)
            {
                Name = UNGROUPED;
                return;
            }

            name = Regex.Replace(name + SEPARATOR, PATTERN, "");
            name = Regex.Replace(name, @"(\s*[\/\\]+\s*)+", SEPARATOR_STR).Trim().Trim(SEPARATOR);
            Name = name.Length == 0 ? UNGROUPED : string.Intern(name + SEPARATOR);
        }
        public static MetaGroup FromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Empty;
            }
            return new MetaGroup(name);
        }
        public static MetaGroup FromName(params string[] path)
        {
            return path == null ? Empty : FromName(string.Join(SEPARATOR, path));
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
