#if DISABLE_DEBUG
#undef DEBUG
#endif
using System;
using System.Collections.Generic;
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

namespace DCFApixels.DragonECS.Core.Internal
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
        internal static void Pipeline_MethodCalledAfterInitialization(string methodName)
        {
            throw new InvalidOperationException($"It is forbidden to call {methodName}, after initialization {nameof(EcsPipeline)}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Pipeline_MethodCalledBeforeInitialization(string methodName)
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
        [MethodImpl(MethodImplOptions.NoInlining)]
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
        internal static void Query_ArgumentDifferentWorldsException()
        {
            ArgumentDifferentWorldsException();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentDifferentWorldsException()
        {
            throw new ArgumentException("World ID mismatch: the expected and actual world identifiers do not match.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EmptyStack()
        {
            throw new InvalidOperationException("Invalid Operation Empty Stack.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentNull()
        {
            throw new ArgumentNullException(null, "Argument cannot be null.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentNull(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentNull(string paramName, string message)
        {
            throw new ArgumentNullException(paramName, message);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentOutOfRange()
        {
            throw new ArgumentOutOfRangeException("index", "index is less than 0 or is equal to or greater than Count.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArgumentOutOfRange(string paramName, object actualValue, string message)
        {
            throw new ArgumentOutOfRangeException(paramName, actualValue, message);
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
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void OpeningClosingMethodsBalanceError()
        {
            throw new InvalidOperationException("Error of opening - closing methods. Closing method was called more often than opening method.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
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
        internal static void InvalidOperationException(string message)
        {
            throw new InvalidOperationException(message);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NotSupportedException(string message)
        {
            throw new NotSupportedException(message);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NotImplementedException(string message)
        {
            throw new NotImplementedException(message);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IndexOutOfRangeException(string message)
        {
            throw new IndexOutOfRangeException(message);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void KeyNotFoundException<TKey>(TKey key)
        {
            throw new KeyNotFoundException($"The key '{key}' was not found.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void NullInstanceException(string message)
        {
            throw new NullInstanceException(message);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EntitySpan_EqualsNotSupported()
        {
            throw new NotSupportedException("Equals() is not supported for Entity Span. Use the equality operator instead.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EntitySpan_GetHashCodeNotSupported()
        {
            throw new NotSupportedException("GetHashCode() is not supported for Entity Span.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EcsReadonlyGroup_EqualsNotSupported()
        {
            throw new NotSupportedException("Equals() is not supported for EcsReadonlyGroup. Use explicit group comparison methods instead.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EcsReadonlyGroup_GetHashCodeNotSupported()
        {
            throw new NotSupportedException("GetHashCode() is not supported for EcsReadonlyGroup.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EcsGroupEnumerator_ResetNotSupported()
        {
            throw new NotSupportedException("Reset() is not supported by EcsGroup.Enumerator.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ArrayEnumerator_ResetNotSupported()
        {
            throw new NotSupportedException("Reset() is not supported by this array enumerator.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void BitEnumerator_ResetNotSupported()
        {
            throw new NotSupportedException("Reset() is not supported by this bit enumerator.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void UnsafeArrayEnumerator_ResetNotSupported()
        {
            throw new NotSupportedException("Reset() is not supported by UnsafeArray enumerator.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EntitySpan_IndexOutOfRange()
        {
            throw new IndexOutOfRangeException("Index is outside the bounds of Entity Span.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void EntitySpan_SliceOutOfRange()
        {
            throw new ArgumentOutOfRangeException("index", "Slice range is outside the bounds of Entity Span.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Pool_EnumerableIsIntelliSenseOnly(string poolTypeName)
        {
            throw new NotImplementedException($"{poolTypeName} enumerable implementation exists only for IntelliSense. Iterate entities via queries or groups.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Implementation_RunnerDoesNotImplementInterface(Type runnerType, Type interfaceType)
        {
            throw new ImplementationException($"Runner {EcsDebugUtility.GetGenericTypeFullName(runnerType, 1)} does not implement interface {EcsDebugUtility.GetGenericTypeFullName(interfaceType, 1)}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Implementation_RunnerImplementationRulesViolation(Type runnerType, Type expectedProcessType, Type actualProcessType)
        {
            throw new ImplementationException($"Runner {EcsDebugUtility.GetGenericTypeFullName(runnerType, 1)} does not match the implementation rules. Expected process type: {EcsDebugUtility.GetGenericTypeFullName(expectedProcessType, 1)}; actual runner process type: {EcsDebugUtility.GetGenericTypeFullName(actualProcessType, 1)}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Injection_NodeNotFound(Type type)
        {
            throw new InjectionException($"The injection graph is missing a node for {type.Name} type. To create a node, use the Injector.AddNode<{type.Name}>() method directly in the injector or in the implementation of the IInjectionUnit for {type.Name}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Injection_RequiredNodeNotFound(Type requiredInjectionType)
        {
            throw new InjectionException($"A systems in the pipeline implements IEcsInject<{requiredInjectionType.Name}> interface, but no suitable injection node was found in the Injector. To create a node, use Injector.AddNode<{requiredInjectionType.Name}>() or implement the IInjectionUnit interface for the type being injected.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Injection_ExtractNotFound(Type type)
        {
            throw new InjectionException($"InjectionList does not contain an injected dependency assignable to {type.Name}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Module_GenericTypeMismatch(Type actualType, Type expectedType)
        {
            throw new ImplementationException($"Module generic type mismatch: {actualType.Name} must inherit EcsModule<{actualType.Name}>, but inherits EcsModule<{expectedType.Name}>.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ComponentIsNotAllowedInWorld(Type componentType, Type worldType)
        {
            throw new InvalidOperationException($"Using component {componentType.GetMeta().TypeName} is not allowed in the {worldType.GetMeta().TypeName} world.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Pool_DifferentComponentTypes()
        {
            throw new ArgumentException("The component instance type and the pool component type are different.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Pool_AlreadyHasComponent<T>(int entityID)
        {
            throw new ArgumentException($"Entity({entityID}) already has component {EcsDebugUtility.GetGenericTypeName<T>()}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Pool_DoesNotHaveComponent<T>(int entityID)
        {
            throw new ArgumentException($"Entity({entityID}) has no component {EcsDebugUtility.GetGenericTypeName<T>()}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Pool_AlreadyHasComponent(Type type, int entityID)
        {
            throw new ArgumentException($"Entity({entityID}) already has component {EcsDebugUtility.GetGenericTypeName(type)}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Pool_DoesNotHaveComponent(Type type, int entityID)
        {
            throw new ArgumentException($"Entity({entityID}) has no component {EcsDebugUtility.GetGenericTypeName(type)}.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Pool_IsLocked()
        {
            throw new InvalidOperationException("The pool is currently locked and cannot add or remove components.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Pool_ComponentTypeIDMismatch(IEcsPool pool, int expectedComponentTypeID, int actualComponentTypeID)
        {
            Type poolType = pool.GetType();
            throw new ImplementationException($"Pool {EcsDebugUtility.GetGenericTypeFullName(poolType, 1)} has invalid ComponentTypeID. Expected component type id: {expectedComponentTypeID}; actual pool ComponentTypeID: {actualComponentTypeID}. This is usually a custom pool implementation error: ComponentTypeID must match the component type registered for this pool.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void StaticMask_HasRepeatedConstraints(string constraintName)
        {
            throw new ArgumentException($"The values in the {constraintName} constraints are repeated.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void StaticMask_HasConflictingConstraints(string leftConstraintName, string rightConstraintName)
        {
            throw new ArgumentException($"Conflicting {leftConstraintName} and {rightConstraintName} constraints.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DependencyGraph_LockedVertexCannotBeRemoved<T>(T vertex)
        {
            throw new InvalidOperationException($"The {vertex} vertex cannot be removed.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void DependencyGraph_CyclicDependencyDetected(string[] cycleDependencies)
        {
            string details = cycleDependencies == null || cycleDependencies.Length == 0 ? string.Empty : $" Cycle edges path: {string.Join(", ", cycleDependencies)}";
            throw new InvalidOperationException("Cyclic dependency detected." + details);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AppendOnlyTable_AlreadyInitialized()
        {
            throw new InvalidOperationException("Table is already initialized. To reinitialize use Reset (if implemented) or restart the application.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void AppendOnlyTable_KeyAlreadyExists<TKey>(TKey key)
        {
            throw new ArgumentException($"An element with the key '{key}' already exists.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IdDispenser_IsAlreadyInUse(int id)
        {
            throw new ArgumentException($"Id {id} is already in use.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IdDispenser_IsHasBeenReserved(int id)
        {
            throw new ArgumentException($"Id {id} has been reserved.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IdDispenser_IsNotUsed(int id)
        {
            throw new ArgumentException($"Id {id} is not used.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IdDispenser_IsNotAvailable(int id)
        {
            throw new ArgumentException($"Id {id} is not available.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IdDispenser_IsNullID(int id)
        {
            throw new ArgumentException($"Id {id} cannot be released because it is used as a null id.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MetaColor_InvalidHexFormat(string input)
        {
            throw new ArgumentException($"Invalid hex color format: {input}");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SparseArray_KeyAlreadyExists<TKey>(TKey key)
        {
            throw new ArgumentException($"Cannot add key '{key}' because SparseArray already contains it.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MemoryAllocator_HandledPointerIsNull()
        {
            throw new ArgumentNullException("handledPtr", "Cannot free memory because the handled pointer is null.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe void MemoryAllocator_CannotCast<T, U>(long totalBytes)
            where T : unmanaged
            where U : unmanaged
        {
            throw new InvalidOperationException($"Cannot cast Memory<{typeof(T).Name}> to Memory<{typeof(U).Name}> because the size of the underlying memory ({totalBytes} bytes) is not a multiple of the size of {typeof(U).Name} ({sizeof(U)} bytes).");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MemoryAllocator_CastLengthExceedsIntMax(long newLengthLong)
        {
            throw new InvalidOperationException($"Resulting length ({newLengthLong}) exceeds int.MaxValue.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void MemoryAllocator_SpanLengthOutOfRange(int requestedLength, int memoryLength)
        {
            throw new ArgumentOutOfRangeException("length", requestedLength, $"Requested span length cannot be greater than allocated memory length ({memoryLength}).");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void WhereToGroupExecutor_CacheMismatch()
        {
            throw new InvalidOperationException("WhereToGroup cached query result does not match direct mask filtering.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Aspect_CanOnlyBeUsedDuringInitialization(string methodName)
        {
            throw new InvalidOperationException($"{methodName} can only be used during field initialization and in the constructor.");
        }
    }
}

