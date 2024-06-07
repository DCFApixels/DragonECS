using System;
using System.Runtime.Serialization;

namespace DCFApixels.DragonECS.Docs
{
    [Serializable]
    [DataContract]
    public class DragonDocsMeta : IComparable<DragonDocsMeta>
    {
        [NonSerialized] private Type _sourceType;
        [NonSerialized] private bool _isInitSourceType = false;

        [DataMember] public readonly string AssemblyQualifiedName = string.Empty;

        [DataMember] public readonly string Name = string.Empty;
        [DataMember] public readonly bool IsCustomName = false;
        [DataMember] public readonly MetaColor Color = MetaColor.BlackColor;
        [DataMember] public readonly bool IsCustomColor = false;
        [DataMember] public readonly string Autor = string.Empty;
        [DataMember] public readonly string Description = string.Empty;

        [DataMember] public readonly string Group = string.Empty;
        [DataMember] public readonly string[] Tags = Array.Empty<string>();

        public DragonDocsMeta(TypeMeta meta)
        {
            _sourceType = meta.Type;
            AssemblyQualifiedName = meta.Type.AssemblyQualifiedName;

            Name = meta.Name;
            IsCustomName = meta.IsCustomName;
            Color = meta.Color;
            IsCustomColor = meta.IsCustomColor;
            Autor = meta.Description.Author;
            Description = meta.Description.Text;

            Group = meta.Group.Name;
            Tags = new string[meta.Tags.Count];
            for (int i = 0, n = meta.Tags.Count; i < n; i++)
            {
                Tags[i] = meta.Tags[i];
            }
        }

        public bool TryGetSourceType(out Type type)
        {
            type = GetSourceType();
            return type != null;
        }
        private Type GetSourceType()
        {
            if (_isInitSourceType) { return _sourceType; }
            _isInitSourceType = true;
            _sourceType = Type.GetType(AssemblyQualifiedName);
            return _sourceType;
        }

        int IComparable<DragonDocsMeta>.CompareTo(DragonDocsMeta other)
        {
            int c = string.Compare(Group, other.Group);
            return c == 0 ? c : string.Compare(Name, other.Name);
        }
    }
}