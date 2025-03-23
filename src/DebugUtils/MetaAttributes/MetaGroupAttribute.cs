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
        public readonly Type InheritingGroupType = null;
        public readonly string RelativeName = string.Empty;

        [Obsolete(EcsMetaAttributeHalper.EMPTY_NO_SENSE_MESSAGE)]
        public MetaGroupAttribute() { }
        public MetaGroupAttribute(string name) { RelativeName = name; }
        public MetaGroupAttribute(params string[] path) { RelativeName = string.Join(SEPARATOR, path); }
        public MetaGroupAttribute(Type inheritingGroupType) { InheritingGroupType = inheritingGroupType; }
        public MetaGroupAttribute(Type inheritingGroupType, string relativeName)
        {
            InheritingGroupType = inheritingGroupType;
            RelativeName = relativeName;
        }
        public MetaGroupAttribute(Type inheritingGroupType, params string[] relativePath)
        {
            InheritingGroupType = inheritingGroupType;
            RelativeName = string.Join(SEPARATOR, relativePath);
        }
    }
    [DebuggerDisplay("{Name}")]
    public class MetaGroup
    {
        public const char SEPARATOR = '/';
        private const string SEPARATOR_STR = "/";
        public const string UNGROUPED = "<UNGROUPED>";
        private const string PATTERN = @"Module(?=/)";
        public static readonly MetaGroup Empty = new MetaGroup(null, UNGROUPED);

        public readonly MetaGroup ParentGroup;
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
        private MetaGroup(MetaGroup parentGroup, string name)
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
            return FromName(null, name);
        }
        public static MetaGroup FromName(MetaGroup parentGroup, string name)
        {
            if(parentGroup == null || parentGroup == Empty)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Empty;
                }
                return new MetaGroup(null, name);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return new MetaGroup(parentGroup, parentGroup.Name);
                }
                return new MetaGroup(parentGroup, parentGroup.Name + name);
            }
        }
        public static MetaGroup FromNameSpace(Type type)
        {
            if (string.IsNullOrWhiteSpace(type.Namespace))
            {
                return Empty;
            }
            return new MetaGroup(null, type.Namespace.Replace('.', SEPARATOR));
        }
        public override string ToString() { return Name; }
    }
}
