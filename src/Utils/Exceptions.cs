#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    [Serializable]
    public class DeepDebugException : Exception
    {
        public DeepDebugException() { }
        public DeepDebugException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public DeepDebugException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
    }
    [Serializable]
    public class NullInstanceException : Exception
    {
        public NullInstanceException() { }
        public NullInstanceException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public NullInstanceException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
    }
    [Serializable]
    public class ImplementationException : Exception
    {
        public ImplementationException() { }
        public ImplementationException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public ImplementationException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
    }
    [Serializable]
    public class InjectionException : Exception
    {
        public InjectionException() { }
        public InjectionException(string message) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message) { }
        public InjectionException(string message, Exception inner) : base(EcsConsts.EXCEPTION_MESSAGE_PREFIX + message, inner) { }
    }
}

namespace DCFApixels.DragonECS.Internal
{
    internal static class Throw
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ConstraintIsAlreadyContainedInMask(EcsTypeCode typeCode)
        {
            string typeName = EcsDebugUtility.GetGenericTypeName(EcsTypeCodeManager.FindTypeOfCode(typeCode).Type);
            throw new ArgumentException($"The {typeName} constraint is already contained in the mask.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Group_AlreadyContains(int entityID)
        {
            throw new ArgumentException($"This group already contains entity {entityID}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Group_DoesNotContain(int entityID)
        {
            throw new ArgumentException($"This group does not contain entity {entityID}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Group_ArgumentDifferentWorldsException()
        {
            throw new ArgumentException("The groups belong to different worlds.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Pipeline_MethodCalledAfterInitialisation(string methodName)
        {
            throw new InvalidOperationException($"It is forbidden to call {methodName}, after initialization {nameof(EcsPipeline)}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Pipeline_MethodCalledBeforeInitialisation(string methodName)
        {
            throw new InvalidOperationException($"It is forbidden to call {methodName}, before initialization {nameof(EcsPipeline)}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Pipeline_MethodCalledAfterDestruction(string methodName)
        {
            throw new InvalidOperationException($"It is forbidden to call {methodName}, after destroying {nameof(EcsPipeline)}.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void World_InvalidIncrementComponentsBalance()
        {
            throw new InvalidOperationException("Invalid increment components balance.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void World_GroupDoesNotBelongWorld()
        {
            throw new InvalidOperationException("The Group does not belong in this world.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void World_MaskDoesntBelongWorld()
        {
            throw new InvalidOperationException($"The mask doesn't belong in this world");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void World_EntityIsNotContained(int entityID)
        {
            throw new ArgumentException($"An entity with identifier {entityID} is not contained in this world");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void World_EntityIsAlreadyСontained(int entityID)
        {
            throw new ArgumentException($"An entity with identifier {entityID} is already contained in this world");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void World_PoolAlreadyCreated()
        {
            throw new ArgumentException("The pool has already been created.");
        }
        public static void World_WorldCantBeDestroyed()
        {
            throw new InvalidOperationException("This world can't be destroyed");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void World_MethodCalledAfterEntityCreation(string methodName)
        {
            throw new InvalidOperationException($"The method {methodName} can only be executed before creating entities in the world.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Ent_ThrowIsNotAlive(EcsWorld world, int entityID)
        {
            Ent_ThrowIsNotAlive((world, entityID));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Ent_ThrowIsNotAlive(entlong entity)
        {
            if (entity.IsNull)
            {
                throw new InvalidOperationException($"The {entity} is null.");
            }
            else
            {
                throw new InvalidOperationException($"The {entity} is not alive.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Quiery_ArgumentDifferentWorldsException()
        {
            throw new ArgumentException("The groups belong to different worlds.");
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentNull()
        {
            throw new ArgumentNullException();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentNull(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentOutOfRange()
        {
            throw new ArgumentOutOfRangeException($"index is less than 0 or is equal to or greater than Count.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UndefinedException()
        {
            throw new Exception();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DeepDebugException()
        {
            throw new DeepDebugException();
        }
        internal static void OpeningClosingMethodsBalanceError()
        {
            throw new InvalidOperationException("Error of opening - closing methods. Closing method was called more often than opening method.");
        }
        internal static void CantReuseBuilder()
        {
            throw new InvalidOperationException("Builder has already worked out, use the new builder to build again.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Exception(string message)
        {
            throw new Exception(message);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentException(string message)
        {
            throw new ArgumentException(message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Aspect_CanOnlyBeUsedDuringInitialization(string methodName)
        {
            throw new InvalidOperationException($"{methodName} can only be used during field initialization and in the constructor.");
        }
    }
}

