#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using System;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class MetaDescriptionAttribute : EcsMetaAttribute
    {
        public readonly MetaDescription Data;
        public MetaDescriptionAttribute(string text)
        {
            Data = new MetaDescription(null, text);
        }
        public MetaDescriptionAttribute(string author, string text)
        {
            Data = new MetaDescription(author, text);
        }
    }
    public class MetaDescription
    {
        public static readonly MetaDescription Empty = new MetaDescription(null, null);
        public readonly string Author;
        public readonly string Text;
        public bool IsHasAutor
        {
            get { return string.IsNullOrEmpty(Author) == false; }
        }
        public MetaDescription(string text) : this(null, text) { }
        public MetaDescription(string author, string text)
        {
            if (author == null) { author = string.Empty; }
            if (text == null) { text = string.Empty; }
            Author = author;
            Text = text;
        }
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Author))
            {
                return Text;
            }
            else
            {
                return $"[{Author}] Text";
            }
        }

    }
}
