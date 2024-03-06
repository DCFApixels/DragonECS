using System;
using System.Collections.Generic;
using System.Linq;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaTagsAttribute : EcsMetaAttribute
    {
        private readonly string[] _tags = Array.Empty<string>();
        public IReadOnlyCollection<string> Tags
        {
            get { return _tags; }
        }

        [Obsolete("With empty parameters, this attribute makes no sense.")]
        public MetaTagsAttribute() { }
        public MetaTagsAttribute(params string[] tags)
        {
            _tags = tags.Where(o => !string.IsNullOrEmpty(o)).ToArray();
        }
    }
    public readonly ref struct MetaTags
    {
        public const string HIDDEN = EcsConsts.META_HIDDEN_TAG;
    }
}
