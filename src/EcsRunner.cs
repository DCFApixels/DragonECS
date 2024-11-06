using DCFApixels.DragonECS.Internal;
using DCFApixels.DragonECS.RunnersCore;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using static DCFApixels.DragonECS.EcsDebugUtility;

namespace DCFApixels.DragonECS
{
    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.OTHER_GROUP)]
    [MetaDescription(EcsConsts.AUTHOR, "...")]
    [MetaTags(MetaTags.HIDDEN)]
    [MetaID("EF8A557C9201E6F04D4A76DC670BDE19")]
    public interface IEcsProcess : IEcsMember { }

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
                    Throw.Exception("The instance of a runner cannot be abstract.");
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

        [MetaColor(MetaColor.DragonRose)]
        [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.OTHER_GROUP)]
        [MetaDescription(EcsConsts.AUTHOR, "...")]
        [MetaTags(MetaTags.HIDDEN)]
        [MetaID("E49B557C92010E46DF1602972BC988BC")]
        public interface IEcsRunner : IEcsProcess
        {
            EcsPipeline Pipeline { get; }
            Type Interface { get; }
            EcsProcessRaw ProcessRaw { get; }
            bool IsEmpty { get; }
        }
        //TODO добавить функцию фильтрации систем по string, за счет создания отдельных ранеров для отдельных string
        [MetaColor(MetaColor.DragonRose)]
        [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.OTHER_GROUP)]
        [MetaDescription(EcsConsts.AUTHOR, "...")]
        [MetaTags(MetaTags.HIDDEN)]
        [MetaID("7DB3557C9201F85E0E1C17D7B19D9CEE")]
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
                    Throw.Exception("Reinitialization.");
                }
                _isInit = true;
                _source = source;
                _process = source.GetProcess<TProcess>();
                OnSetup();
            }
            protected virtual void OnSetup() { }
            #endregion

            #region Simple
            public struct RunHelper
            {
                private readonly EcsProcess<TProcess> _process;
#if DEBUG && !DISABLE_DEBUG
                private Delegate _cacheCheck;
                private bool _cacheCheckInit;
                private readonly EcsProfilerMarker[] _markers;
#endif

                #region Constructors
                public RunHelper(EcsRunner<TProcess> runner) : this(runner,
#if DEBUG && !DISABLE_DEBUG
                    typeof(TProcess).ToMeta().Name)
#else
                    string.Empty)
#endif
                { }

                public RunHelper(EcsRunner<TProcess> runner, string methodName)
                {
                    _process = runner.Process;
#if DEBUG && !DISABLE_DEBUG
                    _cacheCheck = null;
                    _cacheCheckInit = false;
                    _markers = new EcsProfilerMarker[_process.Length];
                    for (int i = 0; i < _process.Length; i++)
                    {
                        _markers[i] = new EcsProfilerMarker($"{_process[i].GetMeta().Name}.{methodName}");
                    }
#endif
                }
                #endregion

                #region Utils
#if DEBUG && !DISABLE_DEBUG
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private void CheckCache(Delegate d)
                {
                    if (_cacheCheckInit == false)
                    {
                        if (_cacheCheck == null)
                        {
                            _cacheCheck = d;
                        }
                        else
                        {
                            if (ReferenceEquals(_cacheCheck, d) == false)
                            {
                                EcsDebug.PrintWarning("The delegate is not cached");
                            }
                            _cacheCheckInit = true;
                        }
                    }
                }
#endif
                #endregion

                #region Do
#pragma warning disable CS0162 // Обнаружен недостижимый код
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Run(Action<TProcess> translationCallback)
                {
#if DEBUG && !DISABLE_DEBUG
                    CheckCache(translationCallback);
                    for (int i = 0, n = _process.Length < _markers.Length ? _process.Length : _markers.Length; i < n; i++)
                    {
                        _markers[i].Begin();
                        try
                        {
                            translationCallback(_process[i]);
                        }
                        catch (Exception e)
                        {
#if DISABLE_CATH_EXCEPTIONS
                            throw;
#endif
                            EcsDebug.PrintError(e);
                        }
                        _markers[i].End();
                    }
#else
                    foreach (var item in _process)
                    {
                        try
                        {
                            translationCallback(item);
                        }
                        catch (Exception e)
                        {
#if DISABLE_CATH_EXCEPTIONS
                            throw;
#endif
                            EcsDebug.PrintError(e);
                        }
                    }
#endif
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Run<T0>(Action<TProcess, T0> translationCallback, T0 t0)
                {
#if DEBUG && !DISABLE_DEBUG
                    CheckCache(translationCallback);
                    for (int i = 0, n = _process.Length < _markers.Length ? _process.Length : _markers.Length; i < n; i++)
                    {
                        _markers[i].Begin();
                        try
                        {
                            translationCallback(_process[i], t0);
                        }
                        catch (Exception e)
                        {
#if DISABLE_CATH_EXCEPTIONS
                            throw;
#endif
                            EcsDebug.PrintError(e);
                        }
                        _markers[i].End();
                    }
#else
                    foreach (var item in _process)
                    {
                        try
                        {
                            translationCallback(item, t0);
                        }
                        catch (Exception e)
                        {
#if DISABLE_CATH_EXCEPTIONS
                            throw;
#endif
                            EcsDebug.PrintError(e);
                        }
                    }
#endif
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Run<T0, T1>(Action<TProcess, T0, T1> translationCallback, T0 t0, T1 t1)
                {
#if DEBUG && !DISABLE_DEBUG
                    CheckCache(translationCallback);
                    for (int i = 0, n = _process.Length < _markers.Length ? _process.Length : _markers.Length; i < n; i++)
                    {
                        _markers[i].Begin();
                        try
                        {
                            translationCallback(_process[i], t0, t1);
                        }
                        catch (Exception e)
                        {
#if DISABLE_CATH_EXCEPTIONS
                            throw;
#endif
                            EcsDebug.PrintError(e);
                        }
                        _markers[i].End();
                    }
#else
                    foreach (var item in _process)
                    {
                        try
                        {
                            translationCallback(item, t0, t1);
                        }
                        catch (Exception e)
                        {
#if DISABLE_CATH_EXCEPTIONS
                            throw;
#endif
                            EcsDebug.PrintError(e);
                        }
                    }
#endif
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Run<T0, T1, T2>(Action<TProcess, T0, T1, T2> translationCallback, T0 t0, T1 t1, T2 t2)
                {
#if DEBUG && !DISABLE_DEBUG
                    CheckCache(translationCallback);
                    for (int i = 0, n = _process.Length < _markers.Length ? _process.Length : _markers.Length; i < n; i++)
                    {
                        _markers[i].Begin();
                        try
                        {
                            translationCallback(_process[i], t0, t1, t2);
                        }
                        catch (Exception e)
                        {
#if DISABLE_CATH_EXCEPTIONS
                            throw;
#endif
                            EcsDebug.PrintError(e);
                        }
                        _markers[i].End();
                    }
#else
                    foreach (var item in _process)
                    {
                        try
                        {
                            translationCallback(item, t0, t1, t2);
                        }
                        catch (Exception e)
                        {
#if DISABLE_CATH_EXCEPTIONS
                            throw;
#endif
                            EcsDebug.PrintError(e);
                        }
                    }
#endif
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Run<T0, T1, T2, T3>(Action<TProcess, T0, T1, T2, T3> translationCallback, T0 t0, T1 t1, T2 t2, T3 t3)
                {
#if DEBUG && !DISABLE_DEBUG
                    CheckCache(translationCallback);
                    for (int i = 0, n = _process.Length < _markers.Length ? _process.Length : _markers.Length; i < n; i++)
                    {
                        _markers[i].Begin();
                        try
                        {
                            translationCallback(_process[i], t0, t1, t2, t3);
                        }
                        catch (Exception e)
                        {
#if DISABLE_CATH_EXCEPTIONS
                            throw;
#endif
                            EcsDebug.PrintError(e);
                        }
                        _markers[i].End();
                    }
#else
                    foreach (var item in _process)
                    {
                        try
                        {
                            translationCallback(item, t0, t1, t2, t3);
                        }
                        catch (Exception e)
                        {
#if DISABLE_CATH_EXCEPTIONS
                            throw;
#endif
                            EcsDebug.PrintError(e);
                        }
                    }
#endif
                }
#pragma warning restore CS0162 // Обнаружен недостижимый код
                //------------------------
                #endregion
            }
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