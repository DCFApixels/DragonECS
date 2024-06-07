using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaTagsAttribute : EcsMetaAttribute
    {
        private readonly string[] _tags = Array.Empty<string>();
        private static char[] _separatpor = new char[] { ',' };
        public IReadOnlyList<string> Tags
        {
            get { return _tags; }
        }

        [Obsolete("With empty parameters, this attribute makes no sense.")]
        public MetaTagsAttribute() { }
        public MetaTagsAttribute(string tags)
        {
            _tags = tags.Split(_separatpor, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (int i = 0; i < _tags.Length; i++)
            {
                _tags[i] = string.Intern(_tags[i]);
            }
        }
        public MetaTagsAttribute(params string[] tags) : this(string.Join(',', tags)) { }
    }
    public readonly ref struct MetaTags
    {
        public const string HIDDEN = EcsConsts.META_HIDDEN_TAG;

        private static string[] _tags = new string[64];
    }
}
