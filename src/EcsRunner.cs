using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.RunnersCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static DCFApixels.DragonECS.EcsDebugUtility;

namespace DCFApixels.DragonECS
{
    public interface IEcsProcess { }

    namespace RunnersCore
    {
        public abstract class EcsRunner
        {
            internal abstract void Init_Internal(EcsPipeline source);

            #region CheckRunnerValide
            public static void CheckRunnerTypeIsValide(Type runnerType, Type processInterfaceType)
            {
                #region DEBUG
#pragma warning disable IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
                Type targetInterface = processInterfaceType;
                if (runnerType.IsAbstract || runnerType.IsInterface)
                {
                    Throw.UndefinedException();
                }
                Type GetRunnerBaseType(Type inType)
                {
                    if (inType.IsGenericType && inType.GetGenericTypeDefinition() == typeof(EcsRunner<>))
                    {
                        return inType;
                    }
                    if (inType.BaseType != null)
                    {
                        return GetRunnerBaseType(inType.BaseType);
                    }
                    return null;
                }
                Type baseType = GetRunnerBaseType(runnerType);
                Type baseTypeArgument = baseType.GenericTypeArguments[0];
                if (baseTypeArgument != targetInterface)
                {
                    Throw.UndefinedException();
                }

                if (!runnerType.GetInterfaces().Any(o => o == targetInterface))
                {
                    throw new EcsRunnerImplementationException($"Runner {GetGenericTypeFullName(runnerType, 1)} does not implement interface {GetGenericTypeFullName(baseTypeArgument, 1)}.");
                }
#pragma warning restore IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
                #endregion
            }
            #endregion
        }
        public interface IEcsRunner : IEcsProcess
        {
            EcsPipeline Pipeline { get; }
            Type Interface { get; }
            EcsProcessRaw ProcessRaw { get; }
            bool IsEmpty { get; }
        }

        public abstract class EcsRunner<TProcess> : EcsRunner, IEcsRunner, IEcsProcess
            where TProcess : IEcsProcess
        {
            private EcsPipeline _source;
            private EcsProcess<TProcess> _process;
            private bool _isInit = false;

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
            public bool IsEmpty
            {
                get { return _process.IsNullOrEmpty; }
            }
            #endregion

            #region Constructor Init OnSetup
            public EcsRunner() { }
            internal override sealed void Init_Internal(EcsPipeline source)
            {
                if (_isInit)
                {
                    Throw.UndefinedException();
                }
                _isInit = true;
                _source = source;
                _process = source.GetProcess<TProcess>();
                OnSetup();
            }
            protected virtual void OnSetup() { }
            #endregion
        }
    }

    #region Extensions
    public static class IEcsProcessExtensions
    {
        public static bool IsRunner(this IEcsProcess self)
        {
            return self is IEcsRunner;
        }
    }
    #endregion
}
