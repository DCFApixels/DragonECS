using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace DCFApixels.DragonECS
{
    public interface ITypeMeta
    {
        Type Type { get; }
        string Name { get; }
        MetaColor Color { get; }
        MetaDescription Description { get; }
        MetaGroup Group { get; }
        IReadOnlyCollection<string> Tags { get; }
    }

    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public sealed class TypeMeta : ITypeMeta
    {
        private static readonly Dictionary<Type, TypeMeta> _metaCache = new Dictionary<Type, TypeMeta>();

        internal readonly Type _type;

        private bool _isCustomName;
        private bool _isCustomColor;
        private bool _isHidden;

        private string _name;
        private MetaColor _color;
        private MetaDescription _description;
        private MetaGroup _group;
        private IReadOnlyCollection<string> _tags;
        private int _typeCode;

        private InitFlag _initFlags = InitFlag.None;

        #region Constructors
        public static TypeMeta Get(Type type)
        {
            if (_metaCache.TryGetValue(type, out TypeMeta result) == false)
            {
                result = new TypeMeta(type);
                _metaCache.Add(type, result);
            }
            return result;
        }
        private TypeMeta(Type type)
        {
            _type = type;
        }
        #endregion

        #region Type
        public Type Type
        {
            get { return _type; }
        }
        #endregion

        #region Name
        private void InitName()
        {
            if (_initFlags.HasFlag(InitFlag.Name) == false)
            {
                (_name, _isCustomName) = MetaGenerator.GetMetaName(_type);
                _initFlags |= InitFlag.Name;
            }
        }
        public bool IsCustomName
        {
            get
            {
                InitName();
                return _isCustomName;
            }
        }
        public string Name
        {
            get
            {
                InitName();
                return _name;
            }
        }
        #endregion

        #region Color
        private void InitColor()
        {
            if (_initFlags.HasFlag(InitFlag.Color) == false)
            {
                (_color, _isCustomColor) = MetaGenerator.GetColor(_type);
                _initFlags |= InitFlag.Color;
            }
        }
        public bool IsCustomColor
        {
            get
            {
                InitColor();
                return _isCustomColor;
            }
        }
        public MetaColor Color
        {
            get
            {
                InitColor();
                return _color;
            }
        }
        #endregion

        #region Description
        public MetaDescription Description
        {
            get
            {
                if (_initFlags.HasFlag(InitFlag.Description) == false)
                {
                    _description = MetaGenerator.GetDescription(_type);
                    _initFlags |= InitFlag.Description;
                }
                return _description;
            }
        }
        #endregion

        #region Group
        public MetaGroup Group
        {
            get
            {
                if (_initFlags.HasFlag(InitFlag.Group) == false)
                {
                    _group = MetaGenerator.GetGroup(_type);
                    _initFlags |= InitFlag.Group;
                }
                return _group;
            }
        }
        #endregion

        #region Tags
        private void InitTags()
        {
            if (_initFlags.HasFlag(InitFlag.Tags) == false)
            {
                _tags = MetaGenerator.GetTags(_type);
                _initFlags |= InitFlag.Tags;
                _isHidden = _tags.Contains(MetaTags.HIDDEN);
            }
        }
        public IReadOnlyCollection<string> Tags
        {
            get
            {
                InitTags();
                return _tags;
            }
        }
        public bool IsHidden
        {
            get
            {
                InitTags();
                return _isHidden;
            }
        }
        #endregion

        #region TypeCode
        public int TypeCode
        {
            get
            {
                if (_initFlags.HasFlag(InitFlag.TypeCode) == false)
                {
                    _typeCode = EcsTypeCode.Get(_type);
                    _initFlags |= InitFlag.TypeCode;
                }
                return _typeCode;
            }
        }
        #endregion

        #region InitializeAll
        public TypeMeta InitializeAll()
        {
            if (_initFlags != InitFlag.All)
            {
                _ = Name;
                _ = Group;
                _ = Color;
                _ = Description;
                _ = Tags;
                _ = TypeCode;
            }
            return this;
        }
        #endregion

        #region InitFlag
        [Flags]
        private enum InitFlag : byte
        {
            None = 0,
            Name = 1 << 0,
            Group = 1 << 1,
            Color = 1 << 2,
            Description = 1 << 3,
            Tags = 1 << 4,
            TypeCode = 1 << 5,

            All = Name | Group | Color | Description | Tags | TypeCode
        }
        #endregion

        #region Other
        public override string ToString()
        {
            return Name;
        }
        private class DebuggerProxy : ITypeMeta
        {
            private readonly TypeMeta _meta;
            public Type Type
            {
                get { return _meta.Type; }
            }
            public string Name
            {
                get { return _meta.Name; }
            }
            public MetaColor Color
            {
                get { return _meta.Color; }
            }
            public MetaDescription Description
            {
                get { return _meta.Description; }
            }
            public MetaGroup Group
            {
                get { return _meta.Group; }
            }
            public IReadOnlyCollection<string> Tags
            {
                get { return _meta.Tags; }
            }
            public DebuggerProxy(TypeMeta meta)
            {
                _meta = meta;
            }
        }
        #endregion

        #region MetaGenerator
        private static class MetaGenerator
        {
            private const int GENERIC_NAME_DEPTH = 3;

            #region GetMetaName
            public static (string, bool) GetMetaName(Type type)
            {
                bool isCustom = type.TryGetCustomAttribute(out MetaNameAttribute atr) && string.IsNullOrEmpty(atr.name) == false;
                if (isCustom)
                {
                    if ((type.IsGenericType && atr.isHideGeneric == false) == false)
                    {
                        return (atr.name, isCustom);
                    }
                    string genericParams = "";
                    Type[] typeParameters = type.GetGenericArguments();
                    for (int i = 0; i < typeParameters.Length; ++i)
                    {
                        string paramTypeName = EcsDebugUtility.GetGenericTypeName(typeParameters[i], GENERIC_NAME_DEPTH);
                        genericParams += (i == 0 ? paramTypeName : $", {paramTypeName}");
                    }
                    return ($"{atr.name}<{genericParams}>", isCustom);
                }
                return (EcsDebugUtility.GetGenericTypeName(type, GENERIC_NAME_DEPTH), isCustom);
            }
            #endregion

            #region GetColor
            private static MetaColor AutoColor(Type type)
            {
                return new MetaColor(type.Name).UpContrast();//.Desaturate(0.48f) / 1.18f;
            }
            public static (MetaColor, bool) GetColor(Type type)
            {
                bool isCustom = type.TryGetCustomAttribute(out MetaColorAttribute atr);
                return (isCustom ? atr.color : AutoColor(type), isCustom);
            }
            #endregion

            #region GetGroup
            public static MetaGroup GetGroup(Type type)
            {
                return type.TryGetCustomAttribute(out MetaGroupAttribute atr) ? atr.Data : MetaGroup.Empty;
            }
            #endregion

            #region GetDescription
            public static MetaDescription GetDescription(Type type)
            {
                bool isCustom = type.TryGetCustomAttribute(out MetaDescriptionAttribute atr);
                return isCustom ? atr.Data : MetaDescription.Empty;
            }
            #endregion

            #region GetTags
            public static IReadOnlyCollection<string> GetTags(Type type)
            {
                var atr = type.GetCustomAttribute<MetaTagsAttribute>();
                return atr != null ? atr.Tags : Array.Empty<string>();
            }
            #endregion
        }
        #endregion
    }
}