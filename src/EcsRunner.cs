using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    using RunnersCore;
    using System.ComponentModel;

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    sealed class EcsRunnerFilterAttribute : Attribute
    {
        public readonly Type interfaceType;
        public readonly object filter;
        public EcsRunnerFilterAttribute(Type interfaceType, object filter)
        {
            this.interfaceType = interfaceType;
            this.filter = filter;
        }
        public EcsRunnerFilterAttribute(object filter) : this(null, filter) { }
    }

    public interface IEcsSystem { }

    namespace RunnersCore
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public interface IEcsRunner
        {
            public EcsPipeline Source { get; }
            public Type Interface { get; }
            public IList Targets { get; }
            public object Filter { get; }
            public bool IsHasFilter { get; }
            public bool IsDestroyed { get; }
            public bool IsEmpty { get; }

            public void Destroy();
        }

        internal static class EcsRunnerActivator
        {
            private static Dictionary<Guid, Type> _runnerHandlerTypes; //interface guid/Runner handler type pairs;

            static EcsRunnerActivator()
            {
                List<Exception> delayedExceptions = new List<Exception>();
                Type runnerBaseType = typeof(EcsRunner<>);
                List<Type> runnerHandlerTypes = new List<Type>();

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    runnerHandlerTypes.AddRange(assembly.GetTypes()
                        .Where(type => type.BaseType != null && type.BaseType.IsGenericType && runnerBaseType == type.BaseType.GetGenericTypeDefinition()));
                }

#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            for (int i = 0; i < runnerHandlerTypes.Count; i++)
            {
                var e = CheckRunnerValide(runnerHandlerTypes[i]);
                if (e != null)
                {
                    runnerHandlerTypes.RemoveAt(i--);
                    delayedExceptions.Add(e);
                }
            }
#endif
                _runnerHandlerTypes = new Dictionary<Guid, Type>();
                foreach (var item in runnerHandlerTypes)
                {
                    Type interfaceType = item.BaseType.GenericTypeArguments[0];
                    _runnerHandlerTypes.Add(interfaceType.GUID, item);
                }

                if (delayedExceptions.Count > 0)
                {
                    foreach (var item in delayedExceptions) EcsDebug.Print(EcsConsts.DEBUG_ERROR_TAG, item.Message);
                    throw delayedExceptions[0];
                }
            }

            private static Exception CheckRunnerValide(Type type) //TODO доработать проверку валидности реалиазации ранера
            {
                Type baseType = type.BaseType;
                Type baseTypeArgument = baseType.GenericTypeArguments[0];

                if (type.ReflectedType != null)
                {
                    return new EcsRunnerImplementationException($"{type.FullName}.ReflectedType must be Null, but equal to {type.ReflectedType.FullName}.");
                }
                if (!baseTypeArgument.IsInterface)
                {
                    return new EcsRunnerImplementationException($"Argument T of class EcsRunner<T>, can only be an inetrface.The {baseTypeArgument.FullName} type is not an interface.");
                }

                var interfaces = type.GetInterfaces();

                if (!interfaces.Any(o => o == baseTypeArgument))
                {
                    return new EcsRunnerImplementationException($"Runner {type.FullName} does not implement interface {baseTypeArgument.FullName}.");
                }

                return null;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void InitFor<TInterface>() where TInterface : IEcsSystem
            {
                Type interfaceType = typeof(TInterface);
                Type nonGenericInterfaceType = interfaceType;
                if (nonGenericInterfaceType.IsGenericType)
                {
                    nonGenericInterfaceType = nonGenericInterfaceType.GetGenericTypeDefinition();
                }
                Guid interfaceGuid = nonGenericInterfaceType.GUID;

                if (!_runnerHandlerTypes.TryGetValue(interfaceGuid, out Type runnerType))
                {
                    throw new Exception();
                }
                if (interfaceType.IsGenericType)
                {
                    Type[] genericTypes = interfaceType.GetGenericArguments();
                    runnerType = runnerType.MakeGenericType(genericTypes);
                }
                EcsRunner<TInterface>.Register(runnerType);
            }
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract class EcsRunner<TInterface> : IEcsSystem, IEcsRunner
            where TInterface : IEcsSystem
        {
            #region Register
            private static Type _subclass;
            internal static void Register(Type subclass)
            {
#if (DEBUG && !DISABLE_DEBUG) || !DRAGONECS_NO_SANITIZE_CHECKS
            if (_subclass != null)
            {
                throw new EcsRunnerImplementationException($"The Runner<{typeof(TInterface).FullName}> can have only one implementing subclass");
            }

            Type interfaceType = typeof(TInterface);

            var interfaces = interfaceType.GetInterfaces();
            if (interfaceType.IsInterface == false)
            {
                throw new ArgumentException($"{typeof(TInterface).FullName} is not interface");
            }
            if (interfaces.Length != 1 || interfaces[0] != typeof(IEcsSystem))
            {
                throw new ArgumentException($"{typeof(TInterface).FullName} does not directly inherit the {nameof(IEcsSystem)} interface");
            }
#endif
                _subclass = subclass;
            }
            #endregion

            #region FilterSystems
            private static TInterface[] FilterSystems(IEnumerable<IEcsSystem> targets)
            {
                return targets.Where(o => o is TInterface).Select(o => (TInterface)o).ToArray();
            }
            private static TInterface[] FilterSystems(IEnumerable<IEcsSystem> targets, object filter)
            {
                Type interfaceType = typeof(TInterface);

                IEnumerable<IEcsSystem> newTargets;

                if (filter != null)
                {
                    newTargets = targets.Where(o =>
                    {
                        if (o is TInterface == false) return false;
                        var atr = o.GetType().GetCustomAttribute<EcsRunnerFilterAttribute>();
                        return atr != null && atr.interfaceType == interfaceType && atr.filter.Equals(filter);
                    });
                }
                else
                {
                    newTargets = targets.Where(o =>
                    {
                        if (o is TInterface == false) return false;
                        var atr = o.GetType().GetCustomAttribute<EcsRunnerFilterAttribute>();
                        return atr == null || atr.interfaceType == interfaceType && atr.filter == null;
                    });
                }
                return newTargets.Select(o => (TInterface)o).ToArray();
            }
            #endregion

            #region Instantiate
            private static TInterface Instantiate(EcsPipeline source, TInterface[] targets, bool isHasFilter, object filter)
            {
                if (_subclass == null)
                    EcsRunnerActivator.InitFor<TInterface>();

                var instance = (EcsRunner<TInterface>)Activator.CreateInstance(_subclass);
                return (TInterface)(IEcsSystem)instance.Set(source, targets, isHasFilter, filter);
            }
            public static TInterface Instantiate(EcsPipeline source)
            {
                return Instantiate(source, FilterSystems(source.AllSystems), false, null);
            }
            public static TInterface Instantiate(EcsPipeline source, object filter)
            {
                return Instantiate(source, FilterSystems(source.AllSystems, filter), true, filter);
            }
            #endregion

            private EcsPipeline _source;
            protected TInterface[] targets;
            private ReadOnlyCollection<TInterface> _targetsSealed;
            private object _filter;
            private bool _isHasFilter;
            private bool _isDestroyed;

            #region Properties
            public EcsPipeline Source => _source;
            public Type Interface => typeof(TInterface);
            public IList Targets => _targetsSealed;
            public object Filter => _filter;
            public bool IsHasFilter => _isHasFilter;
            public bool IsDestroyed => _isDestroyed;
            public bool IsEmpty => targets == null || targets.Length <= 0;
            #endregion

            private EcsRunner<TInterface> Set(EcsPipeline source, TInterface[] targets, bool isHasFilter, object filter)
            {
                _source = source;
                this.targets = targets;
                _targetsSealed = new ReadOnlyCollection<TInterface>(targets);
                _filter = filter;
                _isHasFilter = isHasFilter;
                OnSetup();
                return this;
            }

            internal void Rebuild()
            {
                if (_isHasFilter)
                    Set(_source, FilterSystems(_source.AllSystems), _isHasFilter, _filter);
                else
                    Set(_source, FilterSystems(_source.AllSystems, _filter), _isHasFilter, _filter);
            }

            public void Destroy()
            {
                _isDestroyed = true;
                _source.OnRunnerDestroy(this);
                _source = null;
                targets = null;
                _targetsSealed = null;
                _filter = null;
                OnDestroy();
            }

            protected virtual void OnSetup() { }
            protected virtual void OnDestroy() { }
        }
    }
    #region Extensions
    public static class EcsRunner
    {
        public static void Destroy(IEcsSystem runner) => ((IEcsRunner)runner).Destroy();
    }
    public static class IEcsSystemExtensions
    {
        public static bool IsRunner(this IEcsSystem self)
        {
            return self is IEcsRunner;
        }
    }
    #endregion
}
