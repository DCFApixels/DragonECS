#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.PoolsCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
#if DEBUG || !REFLECTION_DISABLED
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
        ITypeMeta BaseMeta { get; }
    }
    public static class ITypeMetaExstensions
    {
        public static TypeMeta FindRootTypeMeta(this ITypeMeta meta)
        {
            ITypeMeta result = meta;
            while (result.BaseMeta != null) { result = meta.BaseMeta; }
            return (TypeMeta)result;
        }
    }
    /// <summary> Expanding meta information over Type. </summary>
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.DEBUG_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "Intended for extending meta information of types, for customization of type display in the editor. You can get it by using the object.GetMeta() or Type.ToMeta() extension method. Meta information is collected from meta attributes.")]
    [MetaID("DragonECS_248D587C9201EAEA881F27871B4D18A6")]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public sealed class TypeMeta : ITypeMeta
    {
        private const string NULL_NAME = "NULL";
        public static readonly TypeMeta NullTypeMeta;

        private static readonly object _lock = new object();
        private static readonly Dictionary<Type, TypeMeta> _metaCache = new Dictionary<Type, TypeMeta>();
        private static int _increment = 1;

        private readonly int _uniqueID;
        internal readonly Type _type;

        private bool _isCustomName;
        private bool _isCustomColor;
        private bool _isHidden;
        private bool _isObsolete;

        private string _name;
        private string _typeName;

        private MetaColor _color;
        private MetaDescription _description;
        private MetaGroup _group;
        private IReadOnlyList<string> _tags;
        private string _metaID;
        private EcsTypeCode _typeCode;

        private bool _isProcess;
        private bool _isComponent;
        private bool _isPool;

        private InitFlag _initFlags = InitFlag.None;

        #region Constructors
        static TypeMeta()
        {
            NullTypeMeta = new TypeMeta(typeof(void))
            {
                _isCustomName = false,
                _isCustomColor = true,
                _isHidden = true,

                _name = NULL_NAME,
                _typeName = NULL_NAME,
                _color = MetaColor.Black,
                _description = new MetaDescription("", NULL_NAME),
                _group = MetaGroup.Empty,
                _tags = Array.Empty<string>(),
                _metaID = string.Empty,
                _typeCode = EcsTypeCodeManager.Get(typeof(void)),

                _initFlags = InitFlag.All,
            };
            _metaCache.Add(typeof(void), NullTypeMeta);
        }
        public static TypeMeta Get(Type type)
        {
            lock (_lock) //TODO посмотреть можно ли тут убрать лок
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
            _uniqueID = _increment++;
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
                (_color, _isCustomColor) = MetaGenerator.GetColor(this);
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
                _isObsolete = _tags.Contains(MetaTags.OBSOLETE);
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
        public bool IsObsolete
        {
            get
            {
                InitTags();
                return _isObsolete;
            }
        }
        public bool IsHiddenOrObsolete
        {
            get
            {
                return IsHidden || IsObsolete;
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
        public bool IsHasMetaID() { return string.IsNullOrEmpty(MetaID) == false; }
        #endregion

        #region TypeCode
        public EcsTypeCode TypeCode
        {
            get
            {
                if (_initFlags.HasFlag(InitFlag.TypeCode) == false)
                {
                    _typeCode = EcsTypeCodeManager.Get(_type);
                    _initFlags |= InitFlag.TypeCode;
                }
                return _typeCode;
            }
        }
        #endregion

        #region ReflectionInfo
        public bool IsComponent
        {
            get
            {
                if (_initFlags.HasFlag(InitFlag.ReflectionInfo) == false)
                {
                    MetaGenerator.GetReflectionInfo(this);
                    _initFlags |= InitFlag.ReflectionInfo;
                }
                return _isComponent;
            }
        }
        public bool IsProcess
        {
            get
            {
                if (_initFlags.HasFlag(InitFlag.ReflectionInfo) == false)
                {
                    MetaGenerator.GetReflectionInfo(this);
                    _initFlags |= InitFlag.ReflectionInfo;
                }
                return _isProcess;
            }
        }
        public bool IsPool
        {
            get
            {
                if (_initFlags.HasFlag(InitFlag.ReflectionInfo) == false)
                {
                    MetaGenerator.GetReflectionInfo(this);
                    _initFlags |= InitFlag.ReflectionInfo;
                }
                return _isPool;
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
            ReflectionInfo = 1 << 7,
            //MemberType = 1 << 8,

            All = Name | Group | Color | Description | Tags | TypeCode | MetaID | ReflectionInfo //| MemberType
        }
        #endregion

        #region Other
        ITypeMeta ITypeMeta.BaseMeta
        {
            get { return null; }
        }
        private static bool CheckEcsMemener(Type checkedType)
        {
#if DEBUG || !REFLECTION_DISABLED
            return checkedType.IsInterface == false && checkedType.IsAbstract == false && typeof(IEcsMember).IsAssignableFrom(checkedType);
#else
            EcsDebug.PrintWarning($"Reflection is not available, the {nameof(TypeMeta)}.{nameof(CheckEcsMemener)} method does not work.");
            return false;
#endif
        }
        public static bool TryGetCustomMeta(Type type, out TypeMeta meta)
        {
            if (IsHasCustomMeta(type))
            {
                meta = type.ToMeta();
                return true;
            }
            meta = null;
            return false;
        }
        public static bool IsHasCustomMeta(Type type)
        {
#if DEBUG || !REFLECTION_DISABLED
            return CheckEcsMemener(type) || Attribute.GetCustomAttributes(type, typeof(EcsMetaAttribute), false).Length > 0;
#else
            EcsDebug.PrintWarning($"Reflection is not available, the {nameof(TypeMeta)}.{nameof(IsHasMeta)} method does not work.");
            return false;
#endif
        }
        public static bool IsHasMetaID(Type type)
        {
#if DEBUG || !REFLECTION_DISABLED
            return TryGetCustomMeta(type, out TypeMeta meta) && meta.IsHasMetaID();
#else
            EcsDebug.PrintWarning($"Reflection is not available, the {nameof(TypeMeta)}.{nameof(IsHasMetaID)} method does not work.");
            return false;
#endif
        }
        public override string ToString() { return Name; }
        /// <returns> Unique ID </returns>
        public override int GetHashCode() { return _uniqueID; }
        private class DebuggerProxy : ITypeMeta
        {
            private readonly TypeMeta _meta;

            public int UniqueID
            {
                get { return _meta._uniqueID; }
            }
            ITypeMeta ITypeMeta.BaseMeta
            {
                get { return null; }
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

        #region Obsolete
        [Obsolete("Use TryGetCustomMeta(type)")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool IsHasMeta(Type type)
        {
            return IsHasCustomMeta(type);
        }
        #endregion

        #region MetaGenerator
        private static class MetaGenerator
        {
            private const int GENERIC_NAME_DEPTH = 3;

            #region GetMetaName/GetTypeName
            public static string GetTypeName(Type type)
            {
                return EcsDebugUtility.GetGenericTypeName(type, GENERIC_NAME_DEPTH);
            }
            public static (string, bool) GetMetaName(Type type)
            {
#if DEBUG || !REFLECTION_DISABLED //в дебажных утилитах REFLECTION_DISABLED только в релизном билде работает
                bool isCustom = type.TryGetAttribute(out MetaNameAttribute atr) && string.IsNullOrEmpty(atr.name) == false;
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
            private static MetaColor AutoColor(TypeMeta meta)
            {
                int hash;
                if (meta.Group.IsEmpty)
                {
                    hash = meta.Type.Name.GetHashCode();
                }
                else
                {
                    hash = meta.Group.Name.GetHashCode();
                }
                return MetaColor.FromHashCode(hash).UpContrast();
            }
            public static (MetaColor, bool) GetColor(TypeMeta meta)
            {
#if DEBUG || !REFLECTION_DISABLED //в дебажных утилитах REFLECTION_DISABLED только в релизном билде работает
                bool isCustom = meta.Type.TryGetAttribute(out MetaColorAttribute atr);
                return (isCustom ? atr.color : AutoColor(meta), isCustom);
#else
                EcsDebug.PrintWarning($"Reflection is not available, the {nameof(MetaGenerator)}.{nameof(GetColor)} method does not work.");
                return (MetaColor.White, false);
#endif
            }
            #endregion

            #region GetGroup
            public static MetaGroup GetGroup(Type type)
            {
#if DEBUG || !REFLECTION_DISABLED //в дебажных утилитах REFLECTION_DISABLED только в релизном билде работает
                if (type.TryGetAttribute(out MetaGroupAttribute atr))
                {
                    return MetaGroup.FromName(atr.Name);
                }
                else
                {
                    return MetaGroup.FromNameSpace(type);
                }
#else
                EcsDebug.PrintWarning($"Reflection is not available, the {nameof(MetaGenerator)}.{nameof(GetGroup)} method does not work.");
                return MetaGroup.Empty;
#endif
            }
            #endregion

            #region GetDescription
            public static MetaDescription GetDescription(Type type)
            {
#if DEBUG || !REFLECTION_DISABLED //в дебажных утилитах REFLECTION_DISABLED только в релизном билде работает
                bool isCustom = type.TryGetAttribute(out MetaDescriptionAttribute atr);
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
#if DEBUG || !REFLECTION_DISABLED //в дебажных утилитах REFLECTION_DISABLED только в релизном билде работает
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
#if DEBUG || !REFLECTION_DISABLED //в дебажных утилитах REFLECTION_DISABLED только в релизном билде работает
                var atr = type.GetCustomAttribute<MetaIDAttribute>();

                if (atr == null)
                {
                    return string.Empty;
                }
                else
                {
                    string id = atr.ID;
                    if (type.IsGenericType && type.IsGenericTypeDefinition == false)
                    {
                        var metaIDs = type.GetGenericArguments().Select(o => GetMetaID(o));
                        if (metaIDs.Any(o => string.IsNullOrEmpty(o)))
                        {
                            id = string.Empty;
                        }
                        else
                        {
                            id = $"{id}<{string.Join(", ", metaIDs)}>";
                        }
                    }
                    id = string.Intern(id);
                    return id;
                }
#else
                EcsDebug.PrintWarning($"Reflection is not available, the {nameof(MetaGenerator)}.{nameof(GetMetaID)} method does not work.");
                return string.Empty;
#endif
            }
            #endregion

            #region GetReflectionInfo
            public static void GetReflectionInfo(TypeMeta meta)
            {
                meta._isComponent = typeof(IEcsComponentMember).IsAssignableFrom(meta.Type);
                meta._isProcess = typeof(IEcsProcess).IsAssignableFrom(meta.Type);
                meta._isPool = typeof(IEcsPoolImplementation).IsAssignableFrom(meta.Type);
            }
            #endregion
        }
        #endregion
    }
}