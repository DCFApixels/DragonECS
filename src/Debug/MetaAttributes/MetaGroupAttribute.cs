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
        private static string pattern = @"Module(?=/)";
        private static char[] separatpor = new char[] { '/' };

        public readonly string Name;
        private string[] path = null;
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
            Name = Regex.Replace(name, pattern, ""); ;
        }
        public override string ToString() { return Name; }
    }
}
