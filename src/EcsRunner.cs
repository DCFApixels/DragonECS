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

        public EcsRunnerFilterAttribute(object filter)
        {
            interfaceType = null;
            this.filter = filter;
        }
    }

    public interface IEcsSystem { }
    public interface IEcsRunner { }


    public static class IEcsProcessorExtensions
    {
        public static bool IsRunner(this IEcsSystem self)
        {
            return self is IEcsRunner;
        }
    }


    internal static class EcsRunnerActivator
    {
        private static Dictionary<Guid, Type> _runnerTypes; //interface guid/ Runner type pairs;

        static EcsRunnerActivator()
        {
            List<Exception> exceptions = new List<Exception>();

            Type runnerBaseType = typeof(EcsRunner<>);

            List<Type> newRunnerTypes = new List<Type>();
            newRunnerTypes = Assembly.GetAssembly(runnerBaseType)
                .GetTypes()
                .Where(type => type.BaseType != null && type.BaseType.IsGenericType && runnerBaseType == type.BaseType.GetGenericTypeDefinition())
                .ToList();

#if DEBUG
            for (int i = 0; i < newRunnerTypes.Count; i++)
            {
                var e = CheckRunnerValide(newRunnerTypes[i]);
                if (e != null)
                {
                    newRunnerTypes.RemoveAt(i--);
                    exceptions.Add(e);
                }
            }
#endif
            _runnerTypes = new Dictionary<Guid, Type>();
            foreach (var item in newRunnerTypes)
            {
                Type intrf = item.GetInterfaces().Where(o => o != typeof(IEcsRunner) && o != typeof(IEcsSystem)).First(); //TODO оптимизировать это место
                _runnerTypes.Add(intrf.GUID, item);
            }

            if (exceptions.Count > 0)
            {
                foreach (var item in exceptions) throw item;
            }
        }

        private static Exception CheckRunnerValide(Type type) //TODO доработать проверку валидности реалиазации ранера
        {
            if (type.ReflectedType != null)
                return new Exception($"{type.FullName}.ReflectedType must be Null, but equal to {type.ReflectedType.FullName}");

            //var interfaces = type.GetInterfaces(); 
            //if (interfaces.Length != 1 || interfaces[0].GUID != typeof())
            //{
            //}

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

            if (!_runnerTypes.TryGetValue(interfaceGuid, out Type runnerType))
            {
                throw new Exception();
            }
            if (interfaceType.IsGenericType)
            {
                Type[] genericTypes = interfaceType.GetGenericArguments();
                runnerType = runnerType.MakeGenericType(genericTypes);
            }
            EcsRunner<TInterface>.Init(runnerType);
        }
    }

    public abstract class EcsRunner<TInterface> : IEcsSystem, IEcsRunner
        where TInterface : IEcsSystem
    {
        internal static void Init(Type subclass)
        {

#if DEBUG || !DRAGONECS_NO_SANITIZE_CHECKS
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
            if (interfaces.Length != 1 || interfaces[0] != typeof(IEcsSystem))
            {
                throw new ArgumentException($"{typeof(TInterface).FullName} does not directly inherit the {nameof(IEcsSystem)} interface");
            }
#endif
            _subclass = subclass;
        }

        public static TInterface Instantiate(IEnumerable<IEcsSystem> targets, object filter)
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

            return Instantiate(newTargets.Select(o => (TInterface)o).ToArray());
        }
        public static TInterface Instantiate(IEnumerable<IEcsSystem> targets)
        {
            return Instantiate(targets.Where(o => o is TInterface).Select(o => (TInterface)o).ToArray());
        }
        internal static TInterface Instantiate(TInterface[] targets)
        {
            if (_subclass == null)
                EcsRunnerActivator.InitFor<TInterface>();

            var instance = (EcsRunner<TInterface>)Activator.CreateInstance(_subclass);
            return (TInterface)(IEcsSystem)instance.Set(targets);
        }

        private static Type _subclass;

        protected static void SetSublcass(Type type) => _subclass = type;

        protected TInterface[] targets;

        private EcsRunner<TInterface> Set(TInterface[] targets)
        {
            this.targets = targets;
            return this;
        }
    }
}
