using DCFApixels.DragonECS.RunnersCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static DCFApixels.DragonECS.EcsDebugUtility;

namespace DCFApixels.DragonECS
{
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

    public interface IEcsProcess { }

    namespace RunnersCore
    {
        public interface IEcsRunner
        {
            EcsPipeline Source { get; }
            Type Interface { get; }
            IList Targets { get; }
            object Filter { get; }
            bool IsHasFilter { get; }
            bool IsDestroyed { get; }
            bool IsEmpty { get; }
            void Destroy();
        }

        internal static class EcsRunnerActivator
        {
            private static Dictionary<Type, Type> _runnerHandlerTypes; //interface base type/Runner handler type pairs;
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

#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
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
                _runnerHandlerTypes = new Dictionary<Type, Type>();
                foreach (var item in runnerHandlerTypes)
                {
                    Type interfaceType = item.BaseType.GenericTypeArguments[0];
                    _runnerHandlerTypes.Add(interfaceType.IsGenericType ? interfaceType.GetGenericTypeDefinition() : interfaceType, item);
                }
                if (delayedExceptions.Count > 0)
                {
                    throw new AggregateException(delayedExceptions);
                }
            }

            private static Exception CheckRunnerValide(Type type) //TODO доработать проверку валидности реалиазации ранера
            {
                Type baseType = type.BaseType;
                Type baseTypeArgument = baseType.GenericTypeArguments[0];

                if (type.ReflectedType != null)
                {
                    return new EcsRunnerImplementationException($"{GetGenericTypeFullName(type, 1)}.ReflectedType must be Null, but equal to {GetGenericTypeFullName(type.ReflectedType, 1)}.");
                }
                if (!baseTypeArgument.IsInterface)
                {
                    return new EcsRunnerImplementationException($"Argument T of class EcsRunner<T>, can only be an inetrface. The {GetGenericTypeFullName(baseTypeArgument, 1)} type is not an interface.");
                }

                var interfaces = type.GetInterfaces();

                if (!interfaces.Any(o => o == baseTypeArgument))
                {
                    return new EcsRunnerImplementationException($"Runner {GetGenericTypeFullName(type, 1)} does not implement interface {GetGenericTypeFullName(baseTypeArgument, 1)}.");
                }

                return null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static void InitFor<TInterface>() where TInterface : IEcsProcess
            {
                Type interfaceType = typeof(TInterface);

                if (!_runnerHandlerTypes.TryGetValue(interfaceType.IsGenericType ? interfaceType.GetGenericTypeDefinition() : interfaceType, out Type runnerType))
                {
                    throw new EcsRunnerImplementationException($"There is no implementation of a runner for the {GetGenericTypeFullName<TInterface>(1)} interface.");
                }
                if (interfaceType.IsGenericType)
                {
                    Type[] genericTypes = interfaceType.GetGenericArguments();
                    runnerType = runnerType.MakeGenericType(genericTypes);
                }
                EcsRunner<TInterface>.Register(runnerType);
            }
        }
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.RequireDerived, UnityEngine.Scripting.Preserve]
#endif
        public abstract class EcsRunner<TInterface> : IEcsProcess, IEcsRunner
            where TInterface : IEcsProcess
        {
            #region Register
            private static Type _subclass;
            internal static void Register(Type subclass)
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
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
                if (interfaces.Length != 1 || interfaces[0] != typeof(IEcsProcess))
                {
                    throw new ArgumentException($"{typeof(TInterface).FullName} does not directly inherit the {nameof(IEcsProcess)} interface");
                }
#endif
                _subclass = subclass;
            }
            #endregion

            #region FilterSystems
            private static TInterface[] FilterSystems(IEnumerable<IEcsProcess> targets)
            {
                return targets.Where(o => o is TInterface).Select(o => (TInterface)o).ToArray();
            }
            private static TInterface[] FilterSystems(IEnumerable<IEcsProcess> targets, object filter)
            {
                Type interfaceType = typeof(TInterface);

                IEnumerable<IEcsProcess> newTargets;

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
                if (_subclass == null) EcsRunnerActivator.InitFor<TInterface>();
                var instance = (EcsRunner<TInterface>)Activator.CreateInstance(_subclass);
                return (TInterface)(IEcsProcess)instance.Set(source, targets, isHasFilter, filter);
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
        public static void Destroy(IEcsProcess runner) => ((IEcsRunner)runner).Destroy();
    }
    public static class IEcsSystemExtensions
    {
        public static bool IsRunner(this IEcsProcess self)
        {
            return self is IEcsRunner;
        }
    }
    #endregion
}
