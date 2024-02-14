using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    [Serializable]
    public class EcsFrameworkException : Exception
    {
        public EcsFrameworkException() { }
        public EcsFrameworkException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public EcsFrameworkException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
    }
    [Serializable]
    public class EcsRunnerImplementationException : EcsFrameworkException
    {
        public EcsRunnerImplementationException() { }
        public EcsRunnerImplementationException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public EcsRunnerImplementationException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
    }
}

namespace DCFApixels.DragonECS.Internal
{
    internal static class Throw
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentNull()
        {
            throw new ArgumentNullException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConstraintIsAlreadyContainedInMask(Type type)
        {
            throw new EcsFrameworkException($"The {EcsDebugUtility.GetGenericTypeName(type)} constraint is already contained in the mask.");
        }

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //public static void ArgumentDifferentWorldsException()
        //{
        //    throw new ArgumentException("The groups belong to different worlds.");
        //}
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentOutOfRange()
        {
            throw new ArgumentOutOfRangeException($"index is less than 0 or is equal to or greater than Count.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Group_AlreadyContains(int entityID)
        {
            throw new EcsFrameworkException($"This group already contains entity {entityID}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Group_DoesNotContain(int entityID)
        {
            throw new EcsFrameworkException($"This group does not contain entity {entityID}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Group_ArgumentDifferentWorldsException()
        {
            throw new ArgumentException("The groups belong to different worlds.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Pipeline_MethodCalledAfterInitialisation(string methodName)
        {
            throw new MethodAccessException($"It is forbidden to call {methodName}, after initialization {nameof(EcsPipeline)}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Pipeline_MethodCalledBeforeInitialisation(string methodName)
        {
            throw new MethodAccessException($"It is forbidden to call {methodName}, before initialization {nameof(EcsPipeline)}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Pipeline_MethodCalledAfterDestruction(string methodName)
        {
            throw new MethodAccessException($"It is forbidden to call {methodName}, after destroying {nameof(EcsPipeline)}.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void World_InvalidIncrementComponentsBalance()
        {
            throw new MethodAccessException("Invalid increment components balance.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void World_GroupDoesNotBelongWorld()
        {
            throw new MethodAccessException("The Group does not belong in this world.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void World_MaskDoesntBelongWorld()
        {
            throw new EcsFrameworkException($"The mask doesn't belong in this world");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void World_EntityIsNotContained(int entityID)
        {
            throw new EcsFrameworkException($"An entity with identifier {entityID} is not contained in this world");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void World_EntityIsAlreadyСontained(int entityID)
        {
            throw new EcsFrameworkException($"An entity with identifier {entityID} is already contained in this world");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void World_PoolAlreadyCreated()
        {
            throw new EcsFrameworkException("The pool has already been created.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Ent_ThrowIsNotAlive(entlong entity)
        {
            if (entity.IsNull)
                throw new EcsFrameworkException($"The {entity} is null.");
            else
                throw new EcsFrameworkException($"The {entity} is not alive.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UndefinedException()
        {
            throw new Exception();
        }
    }
}

