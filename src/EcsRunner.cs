using DCFApixels.DragonECS.RunnersCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static DCFApixels.DragonECS.EcsDebugUtility;

namespace DCFApixels.DragonECS
{
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
            {
                throw new ArgumentNullException();
            }
            if (!CheckSubclass(runnerType))
            {
                throw new ArgumentException();
            }
            this.runnerType = runnerType;
        }
        private bool CheckSubclass(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == _baseType)
            {
                return true;
            }
            if (type.BaseType != null)
            {
                return CheckSubclass(type.BaseType);
            }
            return false;
        }
    }

    public interface IEcsProcess { }

    namespace RunnersCore
    {
        public interface IEcsRunner
        {
            EcsPipeline Pipeline { get; }
            Type Interface { get; }
            EcsProcessRaw ProcessRaw { get; }
            bool IsDestroyed { get; }
            bool IsEmpty { get; }
            void Destroy();
        }

#if UNITY_2020_3_OR_NEWER
        [UnityEngine.Scripting.RequireDerived, UnityEngine.Scripting.Preserve]
#endif
        public abstract class EcsRunner<TProcess> : IEcsProcess, IEcsRunner
            where TProcess : IEcsProcess
        {
            #region Register
            private static Type _runnerImplementationType;
            internal static void Register(Type subclass)
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_runnerImplementationType != null)
                {
                    throw new EcsRunnerImplementationException($"The Runner<{typeof(TProcess).FullName}> can have only one implementing subclass");
                }

                Type interfaceType = typeof(TProcess);

                var interfaces = interfaceType.GetInterfaces();
                if (interfaceType.IsInterface == false)
                {
                    throw new ArgumentException($"{typeof(TProcess).FullName} is not interface");
                }
                if (interfaces.Length != 1 || interfaces[0] != typeof(IEcsProcess))
                {
                    throw new ArgumentException($"{typeof(TProcess).FullName} does not directly inherit the {nameof(IEcsProcess)} interface");
                }
#endif
                _runnerImplementationType = subclass;
            }
            #endregion

            #region Instantiate
            private static void CheckRunnerValide(Type type) //TODO доработать проверку валидности реалиазации ранера
            {
                Type targetInterface = typeof(TProcess);
                if (type.IsAbstract)
                {
                    throw new Exception();
                }

                Type GetRunnerBaseType(Type inType)
                {
                    if (inType.IsGenericType && inType.GetGenericTypeDefinition() == typeof(EcsRunner<>))
                        return inType;
                    if (inType.BaseType != null)
                        return GetRunnerBaseType(inType.BaseType);
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


            public static TProcess Instantiate(EcsPipeline source)
            {
                EcsProcess<TProcess> process = source.GetProcess<TProcess>();
                if (_runnerImplementationType == null)
                {
                    Type interfaceType = typeof(TProcess);
                    if (interfaceType.TryGetCustomAttribute(out BindWithEcsRunnerAttribute atr))
                    {
                        Type runnerImplementationType = atr.runnerType;
                        if (interfaceType.IsGenericType)
                        {
                            Type[] genericTypes = interfaceType.GetGenericArguments();
                            runnerImplementationType = runnerImplementationType.MakeGenericType(genericTypes);
                        }
                        CheckRunnerValide(runnerImplementationType);
                        _runnerImplementationType = runnerImplementationType;
                    }
                    else
                    {
                        throw new EcsFrameworkException("Процесс не связан с раннером, используйте атрибуут BindWithEcsRunner(Type runnerType)");
                    }
                }
                var instance = (EcsRunner<TProcess>)Activator.CreateInstance(_runnerImplementationType);
                return (TProcess)(IEcsProcess)instance.Set(source, process);
            }
            #endregion

            private EcsPipeline _source;
            private EcsProcess<TProcess> _process;
            private bool _isDestroyed;

            #region Properties
            public EcsPipeline Pipeline
            {
                get { return _source; }
            }
            public Type Interface
            {
                get { return typeof(TProcess); }
            }
            public EcsProcessRaw ProcessRaw
            {
                get { return _process; }
            }
            public EcsProcess<TProcess> Process
            {
                get { return _process; }
            }
            public bool IsDestroyed
            {
                get { return _isDestroyed; }
            }
            public bool IsEmpty
            {
                get { return _process.IsNullOrEmpty; }
            }
            #endregion

            private EcsRunner<TProcess> Set(EcsPipeline source, EcsProcess<TProcess> process)
            {
                _source = source;
                this._process = process;
                OnSetup();
                return this;
            }
            internal void Rebuild()
            {
                Set(_source, _source.GetProcess<TProcess>());
            }
            public void Destroy()
            {
                _isDestroyed = true;
                _source.OnRunnerDestroy_Internal(this);
                _source = null;
                _process = EcsProcess<TProcess>.Empty;
                OnDestroy();
            }
            protected virtual void OnSetup() { } //TODO rename to OnInitialize
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
                                var s = name.Split('`');
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
