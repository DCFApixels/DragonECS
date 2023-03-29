using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
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

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    sealed class MyAttribute : Attribute
    {   
       
    }

    public interface IEcsSystem { }
    public interface IEcsRunner
    {
        public IList Targets { get; }
        public object Filter { get; }
        public bool IsHasFilter { get; }
    }


    public static class IEcsProcessorExtensions
    {
        public static bool IsRunner(this IEcsSystem self)
        {
            return self is IEcsRunner;
        }
    }

    internal static class EcsRunnerActivator
    {
        private static Dictionary<Guid, Type> _runnerHandlerTypes; //interface guid/Runner handler type pairs;

        static EcsRunnerActivator()
        {
            List<Exception> exceptions = new List<Exception>();

            Type runnerBaseType = typeof(EcsRunner<>);

            List<Type> runnerHandlerTypes = new List<Type>();
            runnerHandlerTypes = Assembly.GetAssembly(runnerBaseType)
                .GetTypes()
                .Where(type => type.BaseType != null && type.BaseType.IsGenericType && runnerBaseType == type.BaseType.GetGenericTypeDefinition())
                .ToList();

#if DEBUG || !DRAGONECS_NO_SANITIZE_CHECKS
            for (int i = 0; i < runnerHandlerTypes.Count; i++)
            {
                var e = CheckRunnerValide(runnerHandlerTypes[i]);
                if (e != null)
                {
                    runnerHandlerTypes.RemoveAt(i--);
                    exceptions.Add(e);
                }
            }
#endif
            _runnerHandlerTypes = new Dictionary<Guid, Type>();
            foreach (var item in runnerHandlerTypes)
            {
                Type interfaceType = item.GetInterfaces().Where(o => o != typeof(IEcsRunner) && o != typeof(IEcsSystem)).First(); //TODO оптимизировать это место
                _runnerHandlerTypes.Add(interfaceType.GUID, item);
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

    public abstract class EcsRunner<TInterface> : IEcsSystem, IEcsRunner
        where TInterface : IEcsSystem
    {
        #region Register
        private static Type _subclass;
        internal static void Register(Type subclass)
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
        private static TInterface Instantiate(EcsSystems source, TInterface[] targets, bool isHasFilter, object filter)
        {
            if (_subclass == null)
                EcsRunnerActivator.InitFor<TInterface>();

            var instance = (EcsRunner<TInterface>)Activator.CreateInstance(_subclass);
            return (TInterface)(IEcsSystem)instance.Set(source, targets, isHasFilter, filter);
        }
        public static TInterface Instantiate(EcsSystems source)
        {
            return Instantiate(source, FilterSystems(source.AllSystems), false, null);
        }
        public static TInterface Instantiate(EcsSystems source, object filter)
        {
            return Instantiate(source, FilterSystems(source.AllSystems, filter), true, filter);
        }
        #endregion

        private EcsSystems _source;
        protected TInterface[] targets;
        private ReadOnlyCollection<TInterface> _targetsSealed;
        private object _filter;
        private bool _isHasFilter;

        public EcsSystems Source => _source;
        public IList Targets => _targetsSealed;
        public object Filter => _filter;
        public bool IsHasFilter => _isHasFilter;

        private EcsRunner<TInterface> Set(EcsSystems source, TInterface[] targets, bool isHasFilter, object filter)
        {
            _source = source;
            this.targets = targets;
            _targetsSealed = new ReadOnlyCollection<TInterface>(targets);
            _filter = filter;
            _isHasFilter = isHasFilter;
            OnSetup();
            return this;
        }

        protected virtual void OnSetup() { }

        internal void Rebuild()
        {
            if(_isHasFilter)
                Set(_source, FilterSystems(_source.AllSystems), _isHasFilter, _filter);
            else
                Set(_source, FilterSystems(_source.AllSystems, _filter), _isHasFilter, _filter);
        }
    }
}
