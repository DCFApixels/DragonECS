namespace DCFApixels.DragonECS
{
    public class EcsConsts
    {
        public const string AUTHOR = "DCFApixels";
        public const string FRAMEWORK_NAME = "DragonECS";

        public const string EXCEPTION_MESSAGE_PREFIX = "[" + FRAMEWORK_NAME + "] ";
        public const string DEBUG_PREFIX = "[DEBUG] ";
        public const string DEBUG_WARNING_TAG = "WARNING";
        public const string DEBUG_ERROR_TAG = "ERROR";
        public const string DEBUG_PASS_TAG = "PASS";

        public const string PRE_BEGIN_LAYER = nameof(PRE_BEGIN_LAYER);
        public const string BEGIN_LAYER = nameof(BEGIN_LAYER);
        public const string BASIC_LAYER = nameof(BASIC_LAYER);
        public const string END_LAYER = nameof(END_LAYER);
        public const string POST_END_LAYER = nameof(POST_END_LAYER);

        public const string META_HIDDEN_TAG = "HiddenInDebagging";

        public const int MAGIC_PRIME = 314159;

        /// defs

        public const bool ENABLE_DRAGONECS_DEBUGGER =
#if ENABLE_DRAGONECS_DEBUGGER
            true;
#else
            false;
#endif
        public const bool ENABLE_DRAGONECS_ASSERT_CHEKS =
#if ENABLE_DRAGONECS_ASSERT_CHEKS
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
        public const bool DISABLE_DEBUG =
#if DISABLE_DEBUG
            true;
#else
            false;
#endif

        public const bool ENABLE_DUMMY_SPAN =
#if ENABLE_DUMMY_SPAN
            true;
#else
            false;
#endif
        public const bool DISABLE_CATH_EXCEPTIONS =
#if DISABLE_CATH_EXCEPTIONS
            true;
#else
            false;
#endif
        public const bool DISABLE_DRAGONECS_DEBUGGER =
#if DISABLE_DRAGONECS_DEBUGGER
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
    enum Option 
    {
        NullChecks = 1,
        ArrayBoundsChecks = 2
    }
    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    class Il2CppSetOptionAttribute : Attribute 
    {
        public Option Option { get; private set; }
        public object Value { get; private set; }
        public Il2CppSetOptionAttribute (Option option, object value) { Option = option; Value = value; }
    }
}
#endif