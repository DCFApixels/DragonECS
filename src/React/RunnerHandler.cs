using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    sealed class RunnerFilterAttribute : Attribute
    {
        public readonly Type interfaceType;
        public readonly object filter;
        public RunnerFilterAttribute(Type interfaceType, object filter)
        {
            this.interfaceType = interfaceType;
            this.filter = filter;
        }
    }

    public interface IProcessor { }

    public static class IProcessorExtensions
    {
        public static bool IsRunner(this IProcessor self)
        {
            return self is IRunner;
        }
    }

    internal static class RunnerActivator
    {
        private static bool _isInit = false;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Init()
        {
            if (_isInit) return;
            Type targetType = typeof(Runner<>);
            var subclasses = Assembly.GetAssembly(targetType).GetTypes().Where(type => type.BaseType != null && type.BaseType.IsGenericType && targetType == type.BaseType.GetGenericTypeDefinition());
            foreach (var item in subclasses)
            {
                item.BaseType.GetMethod("Init", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { item });
            }
            _isInit = true;
        }
    }

    public interface IRunner { }

    public abstract class Runner<TInterface> : IProcessor, IRunner
        where TInterface : IProcessor
    {
        internal static void Init(Type subclass)
        {

#if DEBUG || DCFAECS_NO_SANITIZE_CHECKS
            if (_subclass != null)
            {
                throw new ArgumentException($"The Runner<{typeof(TInterface).FullName}> can only have one subclass");
            }

            Type interfaceType = typeof(TInterface);

            var interfaces = interfaceType.GetInterfaces();
            if (interfaceType.IsInterface == false)
            {
                throw new ArgumentException($"{typeof(TInterface).FullName} is not interface");
            }
            if (interfaces.Length != 1 || interfaces[0] != typeof(IProcessor))
            {
                throw new ArgumentException($"{typeof(TInterface).FullName} does not directly inherit the {nameof(IProcessor)} interface");
            }
#endif
            _subclass = subclass;
        }

        public static TInterface Instantiate(IEnumerable<IProcessor> targets, object filter)
        {
            Type interfaceType = typeof(TInterface);

            IEnumerable<IProcessor> newTargets;

            if (filter != null)
            {
                newTargets = targets.Where(o =>
                {
                    if (o is TInterface == false) return false;
                    var atr = o.GetType().GetCustomAttribute<RunnerFilterAttribute>();
                    return atr != null && atr.interfaceType == interfaceType && atr.filter.Equals(filter);
                });
            }
            else
            {
                newTargets = targets.Where(o =>
                {
                    if (o is TInterface == false) return false;
                    var atr = o.GetType().GetCustomAttribute<RunnerFilterAttribute>();
                    return atr == null || atr.interfaceType == interfaceType && atr.filter == null;
                });
            }

            return Instantiate(newTargets.Select(o => (TInterface)o).ToArray());
        }
        public static TInterface Instantiate(IEnumerable<IProcessor> targets)
        {
            return Instantiate(targets.Where(o => o is TInterface).Select(o => (TInterface)o).ToArray());
        }
        internal static TInterface Instantiate(TInterface[] targets)
        {
            RunnerActivator.Init();
            var instance = (Runner<TInterface>)Activator.CreateInstance(_subclass);
            return (TInterface)(IProcessor)instance.Set(targets);
        }



        private static Type _subclass;
        protected TInterface[] targets;

        private Runner<TInterface> Set(TInterface[] targets)
        {
            this.targets = targets;
            return this;
        }
    }
}
