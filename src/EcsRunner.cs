using DCFApixels.DragonECS.RunnersCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.RequireDerived, UnityEngine.Scripting.Preserve]
#endif
    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class BindWithEcsRunnerAttribute : Attribute
    {
        private static readonly Type _baseType = typeof(EcsRunner<>);
        public readonly Type runnerType;
        public BindWithEcsRunnerAttribute(Type runnerType)
        {
            if (runnerType == null)
                throw new ArgumentNullException();
            if (!CheckSubclass(runnerType))
                throw new ArgumentException();
            this.runnerType = runnerType;
        }
        private bool CheckSubclass(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == _baseType)
                return true;
            if (type.BaseType != null)
                return CheckSubclass(type.BaseType);
            return false;
        }
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
            private static void CheckRunnerValide(Type type) //TODO доработать проверку валидности реалиазации ранера
            {
                Type targetInterface = typeof(TInterface);
                if (type.IsAbstract)
                {
                    throw new Exception();
                }

                Type GetRunnerBaseType(Type type)
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EcsRunner<>))
                        return type;
                    if (type.BaseType != null)
                        return GetRunnerBaseType(type.BaseType);
                    return null;
                }
                Type baseType = GetRunnerBaseType(type);
                Type baseTypeArgument = baseType.GenericTypeArguments[0];

                if (baseTypeArgument != targetInterface)
                {
                    throw new Exception();
                }

                if (!type.GetInterfaces().Any(o => o == targetInterface))
                {
                    throw new EcsRunnerImplementationException($"Runner {GetGenericTypeFullName(type, 1)} does not implement interface {GetGenericTypeFullName(baseTypeArgument, 1)}.");
                }
            }

            private static TInterface Instantiate(EcsPipeline source, TInterface[] targets, bool isHasFilter, object filter)
            {
                if (_subclass == null)
                {
                    Type interfaceType = typeof(TInterface);
                    if (interfaceType.TryGetCustomAttribute(out BindWithEcsRunnerAttribute atr))
                    {
                        Type runnerType = atr.runnerType;
                        if (interfaceType.IsGenericType)
                        {
                            Type[] genericTypes = interfaceType.GetGenericArguments();
                            runnerType = runnerType.MakeGenericType(genericTypes);
                        }
                        CheckRunnerValide(runnerType);
                        _subclass = runnerType;
                    }
                    else
                    {
                        throw new EcsFrameworkException("Процесс не связан с раннером, используйте атрибуут BindWithEcsRunner(Type runnerType)");
                    }
                }

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
            protected virtual void OnSetup() { } //rename to OnInitialize
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

    public static class EcsProcessUtility
    {
        private struct ProcessInterface
        {
            public Type interfaceType;
            public string processName;
            public ProcessInterface(Type interfaceType, string processName)
            {
                this.interfaceType = interfaceType;
                this.processName = processName;
            }
        }
        private static Dictionary<Type, ProcessInterface> _processes = new Dictionary<Type, ProcessInterface>();
        private static HashSet<Type> _systems = new HashSet<Type>();

        static EcsProcessUtility()
        {
            Type processBasicInterface = typeof(IEcsProcess);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.GetInterface(nameof(IEcsProcess)) != null || type == processBasicInterface)
                    {
                        if (type.IsInterface)
                        {
                            string name = type.Name;
                            if (name[0] == 'I' && name.Length > 1 && char.IsUpper(name[1]))
                                name = name.Substring(1);
                            name = Regex.Replace(name, @"\bEcs|Process\b", "");
                            if (Regex.IsMatch(name, "`\\w{1,}$"))
                            {
                                var s = name.Split("`");
                                name = s[0] + $"<{s[1]}>";
                            }
                            _processes.Add(type, new ProcessInterface(type, name));
                        }
                        else
                        {
                            _systems.Add(type);
                        }
                    }
                }
            }
        }

        #region Systems
        public static bool IsSystem(Type type) => _systems.Contains(type);
        public static bool IsEcsSystem(this Type type) => _systems.Contains(type);
        #endregion

        #region Process
        public static bool IsProcessInterface(Type type)
        {
            if (type.IsGenericType) type = type.GetGenericTypeDefinition();
            return _processes.ContainsKey(type);
        }
        public static bool IsEcsProcessInterface(this Type type) => IsProcessInterface(type);

        public static string GetProcessInterfaceName(Type type)
        {
            if (type.IsGenericType) type = type.GetGenericTypeDefinition();
            return _processes[type].processName;
        }
        public static bool TryGetProcessInterfaceName(Type type, out string name)
        {
            if (type.IsGenericType) type = type.GetGenericTypeDefinition();
            bool result = _processes.TryGetValue(type, out ProcessInterface data);
            name = data.processName;
            return result;
        }

        public static IEnumerable<Type> GetEcsProcessInterfaces(this Type self)
        {
            return self.GetInterfaces().Where(o => o.IsEcsProcessInterface());
        }
        #endregion

    }
}
