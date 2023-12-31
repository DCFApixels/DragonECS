namespace DCFApixels.DragonECS
{
    public class EcsConsts
    {
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
    }
}
