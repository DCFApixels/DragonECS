using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace DCFApixels.DragonECS
{
    public class EcsThrowHalper
    {
        public static readonly EcsThrowHalper Throw = new EcsThrowHalper();
        private EcsThrowHalper() { }
    }

    public static class EcsThrowHalper_Core
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Pool_AlreadyHasComponent<T>(this EcsThrowHalper _, int entityID)
        {
            throw new EcsFrameworkException($"Entity({entityID}) already has component {EcsDebugUtility.GetGenericTypeName<T>()}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Pool_NotHaveComponent<T>(this EcsThrowHalper _, int entityID)
        {
            throw new EcsFrameworkException($"Entity({entityID}) has no component {EcsDebugUtility.GetGenericTypeName<T>()}.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentNull(this EcsThrowHalper _)
        {
            throw new ArgumentNullException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConstraintIsAlreadyContainedInMask(this EcsThrowHalper _, Type type)
        {
            throw new EcsFrameworkException($"The {EcsDebugUtility.GetGenericTypeName(type)} constraint is already contained in the mask.");
        }

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public static void ArgumentDifferentWorldsException()
        //{
        //    throw new ArgumentException("The groups belong to different worlds.");
        //}
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentOutOfRange(this EcsThrowHalper _)
        {
            throw new ArgumentOutOfRangeException($"index is less than 0 or is equal to or greater than Count.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Group_AlreadyContains(this EcsThrowHalper _, int entityID)
        {
            throw new EcsFrameworkException($"This group already contains entity {entityID}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Group_DoesNotContain(this EcsThrowHalper _, int entityID)
        {
            throw new EcsFrameworkException($"This group does not contain entity {entityID}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Group_ArgumentDifferentWorldsException(this EcsThrowHalper _)
        {
            throw new ArgumentException("The groups belong to different worlds.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Pipeline_MethodCalledAfterInitialisation(this EcsThrowHalper _, string methodName)
        {
            throw new MethodAccessException($"It is forbidden to call {methodName}, after initialization {nameof(EcsPipeline)}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Pipeline_MethodCalledBeforeInitialisation(this EcsThrowHalper _, string methodName)
        {
            throw new MethodAccessException($"It is forbidden to call {methodName}, before initialization {nameof(EcsPipeline)}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Pipeline_MethodCalledAfterDestruction(this EcsThrowHalper _, string methodName)
        {
            throw new MethodAccessException($"It is forbidden to call {methodName}, after destroying {nameof(EcsPipeline)}.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void World_InvalidIncrementComponentsBalance(this EcsThrowHalper _)
        {
            throw new MethodAccessException("Invalid increment components balance.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void World_GroupDoesNotBelongWorld(this EcsThrowHalper _)
        {
            throw new MethodAccessException("The Group does not belong in this world.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Ent_ThrowIsNotAlive(this EcsThrowHalper _, entlong entity)
        {
            if (entity.IsNull)
                throw new EcsFrameworkException($"The {entity} is null.");
            else
                throw new EcsFrameworkException($"The {entity} is not alive.");
        }
    }

    [Serializable]
    public class EcsFrameworkException : Exception
    {
        public EcsFrameworkException() { }
        public EcsFrameworkException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public EcsFrameworkException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
        protected EcsFrameworkException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
    [Serializable]
    public class EcsRunnerImplementationException : EcsFrameworkException
    {
        public EcsRunnerImplementationException() { }
        public EcsRunnerImplementationException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public EcsRunnerImplementationException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
        protected EcsRunnerImplementationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
