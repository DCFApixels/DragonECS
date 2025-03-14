#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using System;
using System.Collections.Generic;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaTagsAttribute : EcsMetaAttribute
    {
        public const char SEPARATOR = ',';
        private readonly string[] _tags = Array.Empty<string>();
        public IReadOnlyList<string> Tags
        {
            get { return _tags; }
        }

        [Obsolete(EcsMetaAttributeHalper.EMPTY_NO_SENSE_MESSAGE)]
        public MetaTagsAttribute() { }
        public MetaTagsAttribute(string tags)
        {
            _tags = EcsMetaAttributeHalper.Split(SEPARATOR, tags);
            for (int i = 0; i < _tags.Length; i++)
            {
                _tags[i] = string.Intern(_tags[i]);
            }
        }
        public MetaTagsAttribute(string tag0, string tag1) : this($"{tag0},{tag1}") { }
        public MetaTagsAttribute(string tag0, string tag1, string tag2) : this($"{tag0},{tag1},{tag2}") { }
        public MetaTagsAttribute(string tag0, string tag1, string tag2, string tag3) : this($"{tag0},{tag1},{tag2},{tag3}") { }
        public MetaTagsAttribute(string tag0, string tag1, string tag2, string tag3, string tag4) : this($"{tag0},{tag1},{tag2},{tag3},{tag4}") { }
        public MetaTagsAttribute(string tag0, string tag1, string tag2, string tag3, string tag4, string tag5) : this($"{tag0},{tag1},{tag2},{tag3},{tag4},{tag5}") { }
        public MetaTagsAttribute(string tag0, string tag1, string tag2, string tag3, string tag4, string tag5, string tag6) : this($"{tag0},{tag1},{tag2},{tag3},{tag4},{tag5},{tag6}") { }
        public MetaTagsAttribute(string tag0, string tag1, string tag2, string tag3, string tag4, string tag5, string tag6, string tag7) : this($"{tag0},{tag1},{tag2},{tag3},{tag4},{tag5},{tag6},{tag7}") { }
        public MetaTagsAttribute(params string[] tags) : this(string.Join(SEPARATOR, tags)) { }
    }
    public readonly ref struct MetaTags
    {
        public const string HIDDEN = EcsConsts.META_HIDDEN_TAG;
        public const string OBSOLETE = EcsConsts.META_OBSOLETE_TAG;
        public const string ENGINE_MEMBER = EcsConsts.META_ENGINE_MEMBER_TAG;
    }
}
