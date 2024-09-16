using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#if (DEBUG && !DISABLE_DEBUG) || !REFLECTION_DISABLED
using System.Reflection;
#endif

namespace DCFApixels.DragonECS
{
    public interface ITypeMeta
    {
        Type Type { get; }
        string Name { get; }
        MetaColor Color { get; }
        MetaDescription Description { get; }
        MetaGroup Group { get; }
        IReadOnlyList<string> Tags { get; }
        //string MetaID { get; }
        ITypeMeta BaseMeta { get; }
    }
    public static class ITypeMetaExstensions
    {
        public static TypeMeta FindRootTypeMeta(this ITypeMeta meta)
        {
            ITypeMeta result = null;
            while ((result = meta.BaseMeta) != null) { }
            return (TypeMeta)result;
        }
    }
    /// <summary> Expanding meta information over Type. </summary>
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.DEBUG_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Intended for extending meta information of types, for customization of type display in the editor. You can get it by using the object.GetMeta() or Type.ToMeta() extension method. Meta information is collected from meta attributes.")]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public sealed class TypeMeta : ITypeMeta
    {
        private static readonly Dictionary<Type, TypeMeta> _metaCache = new Dictionary<Type, TypeMeta>();

        internal readonly Type _type;

        private bool _isCustomName;
        private bool _isCustomColor;
        private bool _isHidden;

        private string _name;
        private string _typeName;

        private MetaColor _color;
        private MetaDescription _description;
        private MetaGroup _group;
        private IReadOnlyList<string> _tags;
        private string _metaID;
        private int _typeCode;

        private InitFlag _initFlags = InitFlag.None;

        private static object _lock = new object();

        //private EcsMemberType _memberType;

        public ITypeMeta BaseMeta
        {
            get { return null; }
        }

        #region Constructors
        public static TypeMeta Get(Type type)
        {
            lock (_lock)
            {
                if (_metaCache.TryGetValue(type, out TypeMeta result) == false)
                {
                    result = new TypeMeta(type);
                    _metaCache.Add(type, result);
                }
                return result;
            }
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

        //#region EcsMemberType
        //public EcsMemberType EcsMemberType
        //{
        //    get { return _memberType; }
        //}
        //#endregion

        #region Name
        private void InitName()
        {
            if (_initFlags.HasFlag(InitFlag.Name) == false)
            {
                (_name, _isCustomName) = MetaGenerator.GetMetaName(_type);
                _typeName = _isCustomName ? MetaGenerator.GetTypeName(_type) : _name;
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
        public string TypeName
        {
            get
            {
                InitName();
                return _typeName;
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
        public IReadOnlyList<string> Tags
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

        #region MetaID
        private void InitMetaID()
        {
            if (_initFlags.HasFlag(InitFlag.MetaID) == false)
            {
                _metaID = MetaGenerator.GetMetaID(_type);
                _initFlags |= InitFlag.MetaID;
            }
        }
        public string MetaID
        {
            get
            {
                InitMetaID();
                return _metaID;
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
                _ = MetaID;
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
            MetaID = 1 << 5,
            TypeCode = 1 << 6,
            //MemberType = 1 << 7,

            All = Name | Group | Color | Description | Tags | TypeCode | MetaID //| MemberType
        }
        #endregion

        #region Other
        public override string ToString()
        {
            return Name;
        }
        public override int GetHashCode()
        {
            return _color.GetHashCode() ^ _name[0].GetHashCode() ^ _name[_name.Length - 1].GetHashCode();
        }
        private class DebuggerProxy : ITypeMeta
        {
            private readonly TypeMeta _meta;

            public ITypeMeta BaseMeta
            {
                get { return _meta.BaseMeta; }
            }
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
            public IReadOnlyList<string> Tags
            {
                get { return _meta.Tags; }
            }
            public string MetaID
            {
                get { return _meta.MetaID; }
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

            //private static HashSet<Type> _;

            //#region GetMemberType
            //public static EcsMemberType GetMemberType(Type type)
            //{
            //    throw new NotImplementedException();
            //}
            //#endregion

            #region GetMetaName/GetTypeName
            public static string GetTypeName(Type type)
            {
                return EcsDebugUtility.GetGenericTypeName(type, GENERIC_NAME_DEPTH);
            }
            public static (string, bool) GetMetaName(Type type)
            {
#if (DEBUG && !DISABLE_DEBUG) || !REFLECTION_DISABLED //в дебажных утилитах REFLECTION_DISABLED только в релизном билде работает
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
#else
                EcsDebug.PrintWarning($"Reflection is not available, the {nameof(MetaGenerator)}.{nameof(GetMetaName)} method does not work.");
                return (type.Name, false);
#endif
            }
            #endregion

            #region GetColor
            private static MetaColor AutoColor(Type type)
            {
                return new MetaColor(type.Name).UpContrast();//.Desaturate(0.48f) / 1.18f;
            }
            public static (MetaColor, bool) GetColor(Type type)
            {
#if (DEBUG && !DISABLE_DEBUG) || !REFLECTION_DISABLED //в дебажных утилитах REFLECTION_DISABLED только в релизном билде работает
                bool isCustom = type.TryGetCustomAttribute(out MetaColorAttribute atr);
                return (isCustom ? atr.color : AutoColor(type), isCustom);
#else
                EcsDebug.PrintWarning($"Reflection is not available, the {nameof(MetaGenerator)}.{nameof(GetColor)} method does not work.");
                return (AutoColor(type), false);
#endif
            }
            #endregion

            #region GetGroup
            public static MetaGroup GetGroup(Type type)
            {
#if (DEBUG && !DISABLE_DEBUG) || !REFLECTION_DISABLED //в дебажных утилитах REFLECTION_DISABLED только в релизном билде работает
                return type.TryGetCustomAttribute(out MetaGroupAttribute atr) ? atr.Data : MetaGroup.Empty;
#else
                EcsDebug.PrintWarning($"Reflection is not available, the {nameof(MetaGenerator)}.{nameof(GetGroup)} method does not work.");
                return MetaGroup.Empty;
#endif
            }
            #endregion

            #region GetDescription
            public static MetaDescription GetDescription(Type type)
            {
#if (DEBUG && !DISABLE_DEBUG) || !REFLECTION_DISABLED //в дебажных утилитах REFLECTION_DISABLED только в релизном билде работает
                bool isCustom = type.TryGetCustomAttribute(out MetaDescriptionAttribute atr);
                return isCustom ? atr.Data : MetaDescription.Empty;
#else
                EcsDebug.PrintWarning($"Reflection is not available, the {nameof(MetaGenerator)}.{nameof(GetDescription)} method does not work.");
                return MetaDescription.Empty;
#endif
            }
            #endregion

            #region GetTags
            public static IReadOnlyList<string> GetTags(Type type)
            {
#if (DEBUG && !DISABLE_DEBUG) || !REFLECTION_DISABLED //в дебажных утилитах REFLECTION_DISABLED только в релизном билде работает
                var atr = type.GetCustomAttribute<MetaTagsAttribute>();
                return atr != null ? atr.Tags : Array.Empty<string>();
#else
                EcsDebug.PrintWarning($"Reflection is not available, the {nameof(MetaGenerator)}.{nameof(GetTags)} method does not work.");
                return Array.Empty<string>();
#endif
            }
            #endregion

            #region GetMetaID
            public static string GetMetaID(Type type)
            {
#if (DEBUG && !DISABLE_DEBUG) || !REFLECTION_DISABLED //в дебажных утилитах REFLECTION_DISABLED только в релизном билде работает
                var atr = type.GetCustomAttribute<MetaIDAttribute>();
                return atr != null ? atr.ID : string.Empty;
#else
                EcsDebug.PrintWarning($"Reflection is not available, the {nameof(MetaGenerator)}.{nameof(GetTags)} method does not work.");
                return string.Empty;
#endif
            }
            #endregion
        }
        #endregion
    }

    //public enum EcsMemberType : byte
    //{
    //    Undefined = 0,
    //
    //    Component = 1,
    //    System = 2,
    //    Other = 3,
    //}
}