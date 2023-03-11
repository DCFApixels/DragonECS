using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

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
    }

    public interface IEcsProcessor { }

    public static class IEcsProcessorExtensions
    {
        public static bool IsRunner(this IEcsProcessor self)
        {
            return self is IEcsRunner;
        }
    }

    internal static class EcsRunnerActivator
    {
        private static bool _isInit = false;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Init()
        {
            if (_isInit) return;
            Type targetType = typeof(EcsRunner<>);
            var subclasses = Assembly.GetAssembly(targetType).GetTypes().Where(type => type.BaseType != null && type.BaseType.IsGenericType && targetType == type.BaseType.GetGenericTypeDefinition());
            foreach (var item in subclasses)
            {
                item.BaseType.GetMethod("Init", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { item });
            }
            _isInit = true;
        }
    }


    public interface IEcsRunner { }

    public abstract class EcsRunner<TInterface> : IEcsProcessor, IEcsRunner
        where TInterface : IEcsProcessor
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
            if (interfaces.Length != 1 || interfaces[0] != typeof(IEcsProcessor))
            {
                throw new ArgumentException($"{typeof(TInterface).FullName} does not directly inherit the {nameof(IEcsProcessor)} interface");
            }
#endif
            _subclass = subclass;
        }

        public static TInterface Instantiate(IEnumerable<IEcsProcessor> targets, object filter)
        {
            Type interfaceType = typeof(TInterface);

            IEnumerable<TInterface> newTargets = targets.OfType<TInterface>();

            if (filter != null)
            {
                newTargets = newTargets.Where(o =>
                {
                    if (o is TInterface == false) return false;
                    var atr = o.GetType().GetCustomAttribute<EcsRunnerFilterAttribute>();
                    return atr != null && atr.interfaceType == interfaceType && atr.filter.Equals(filter);
                });
            }
            else
            {
                newTargets = newTargets.Where(o =>
                {
                    if (o is TInterface == false) return false;
                    var atr = o.GetType().GetCustomAttribute<EcsRunnerFilterAttribute>();
                    return atr == null || atr.interfaceType == interfaceType && atr.filter == null;
                });
            }

            return Instantiate(newTargets.ToArray());
        }
        public static TInterface Instantiate(IEnumerable<IEcsProcessor> targets)
        {
            return Instantiate(targets.OfType<TInterface>().ToArray());
        }
        internal static TInterface Instantiate(TInterface[] targets)
        {
            EcsRunnerActivator.Init();
            var instance = (EcsRunner<TInterface>)Activator.CreateInstance(_subclass);
            return (TInterface)(IEcsProcessor)instance.Set(targets);
        }

        private static Type _subclass;
        protected TInterface[] targets;

        private EcsRunner<TInterface> Set(TInterface[] targets)
        {
            this.targets = targets;
            return this;
        }
    }
}
