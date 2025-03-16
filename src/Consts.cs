using System;

namespace DCFApixels.DragonECS
{
    public static class EcsConsts
    {
        public const string AUTHOR = "DCFApixels";
        public const string FRAMEWORK_NAME = "DragonECS";
        public const string NAME_SPACE = AUTHOR + "." + FRAMEWORK_NAME + ".";
        public const string EXCEPTION_MESSAGE_PREFIX = "[" + FRAMEWORK_NAME + "] ";
        public const string DEBUG_PREFIX = "[DEBUG] ";
        public const string DEBUG_WARNING_TAG = "WARNING";
        public const string DEBUG_ERROR_TAG = "ERROR";
        public const string DEBUG_PASS_TAG = "PASS";

        public const string PRE_BEGIN_LAYER = NAME_SPACE + nameof(PRE_BEGIN_LAYER);
        public const string BEGIN_LAYER = NAME_SPACE + nameof(BEGIN_LAYER);
        public const string BASIC_LAYER = NAME_SPACE + nameof(BASIC_LAYER);
        public const string END_LAYER = NAME_SPACE + nameof(END_LAYER);
        public const string POST_END_LAYER = NAME_SPACE + nameof(POST_END_LAYER);

        public const string META_HIDDEN_TAG = "HiddenInDebagging";
        public const string META_OBSOLETE_TAG = "Obsolete";
        public const string META_ENGINE_MEMBER_TAG = "EngineMember";

        public const int MAGIC_PRIME = 314159;

        public const int NULL_ENTITY_ID = 0;
        public const short NULL_WORLD_ID = 0;
        /// meta subgroups

        public const string PACK_GROUP = "_" + FRAMEWORK_NAME + "/_Core";
        public const string WORLDS_GROUP = "Worlds";
        public const string DI_GROUP = "DI";
        public const string POOLS_GROUP = "Pools";
        public const string PROCESSES_GROUP = "Processes";
        public const string DEBUG_GROUP = "Debug";
        public const string OTHER_GROUP = "Other";
        public const string OBSOLETE_GROUP = "Obsolete";
        public const string TEMPLATES_GROUP = "Templates";
        public const string IMPLEMENTATIONS_GROUP = "Implementation";

        public const string COMPONENTS_GROUP = "Components";
        public const string SYSTEMS_GROUP = "Systems";
        public const string MODULES_GROUP = "Modules";
    }

    public static class EcsDefines
    {
        public const bool DRAGONECS_ENABLE_DEBUG_SERVICE =
#if DRAGONECS_ENABLE_DEBUG_SERVICE
            true;
#else
            false;
#endif
        public const bool DRAGONECS_DISABLE_POOLS_EVENTS =
#if DRAGONECS_DISABLE_POOLS_EVENTS
            true;
#else
            false;
#endif
        public const bool DRAGONECS_DISABLE_CATH_EXCEPTIONS =
#if DRAGONECS_DISABLE_CATH_EXCEPTIONS
            true;
#else
            false;
#endif
        public const bool DRAGONECS_STABILITY_MODE =
#if DRAGONECS_STABILITY_MODE
            true;
#else
            false;
#endif
        public const bool DRAGONECS_DEEP_DEBUG =
#if DRAGONECS_DEEP_DEBUG
            true;
#else
            false;
#endif
        public const bool DISABLE_DEBUG =
#if DISABLE_DEBUG
            true;
#else
            false;
#endif
        public const bool REFLECTION_DISABLED =
#if REFLECTION_DISABLED
            true;
#else
            false;
#endif



        [Obsolete("DRAGONECS_ENABLE_DEBUG_SERVICE")]
        public const bool ENABLE_DRAGONECS_DEBUGGER =
#if ENABLE_DRAGONECS_DEBUGGER
            true;
#else
            false;
#endif
        [Obsolete("DRAGONECS_DISABLE_POOLS_EVENTS")]
        public const bool DISABLE_POOLS_EVENTS =
#if DISABLE_POOLS_EVENTS
            true;
#else
            false;
#endif
        [Obsolete("DRAGONECS_DISABLE_CATH_EXCEPTIONS")]
        public const bool DISABLE_CATH_EXCEPTIONS =
#if DISABLE_CATH_EXCEPTIONS
            true;
#else
            false;
#endif
        [Obsolete("DRAGONECS_STABILITY_MODE")]
        public const bool ENABLE_DRAGONECS_ASSERT_CHEKS =
#if ENABLE_DRAGONECS_ASSERT_CHEKS
            true;
#else
            false;
#endif
        [Obsolete]
        public const bool ENABLE_DUMMY_SPAN =
#if ENABLE_DUMMY_SPAN
            true;
#else
            false;
#endif
    }
}
//#if UNITY_2020_3_OR_NEWER
//        [UnityEngine.Scripting.RequireDerived, UnityEngine.Scripting.Preserve]
//#endif




#if ENABLE_IL2CPP
// Unity IL2CPP performance optimization attribute.
namespace Unity.IL2CPP.CompilerServices
{
    using System;
    internal enum Option
    {
        NullChecks = 1,
        ArrayBoundsChecks = 2,
        DivideByZeroChecks = 3,
    }
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Delegate, Inherited = false, AllowMultiple = true)]
    internal class Il2CppSetOptionAttribute : Attribute
    {
        public Option Option { get; private set; }
        public object Value { get; private set; }
        public Il2CppSetOptionAttribute(Option option, object value)
        {
            Option = option;
            Value = value;
        }
    }
}
#endif