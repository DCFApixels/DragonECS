#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core.Internal;
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
    [MetaID("DragonECS_EF8A557C9201E6F04D4A76DC670BDE19")]
    public interface IEcsProcess : IEcsMember { }

    namespace RunnersCore
    {
        //добавить инъекцию в раннеры
        public abstract class EcsRunner
        {
            internal abstract void Init_Internal(EcsPipeline source);

            #region CheckRunnerValide
            public static void CheckRunnerTypeIsValide(Type runnerType, Type processInterfaceType)
            {
#if DEBUG
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
                    throw new ImplementationException($"Runner {GetGenericTypeFullName(runnerType, 1)} does not implement interface {GetGenericTypeFullName(baseTypeArgument, 1)}.");
                }
#pragma warning restore IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
#endif
            }
            #endregion

            public delegate void ActionWithData<in TProcess, T>(TProcess process, ref T Data);
        }

        [MetaColor(MetaColor.DragonRose)]
        [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.OTHER_GROUP)]
        [MetaDescription(EcsConsts.AUTHOR, "...")]
        [MetaTags(MetaTags.HIDDEN)]
        [MetaID("DragonECS_E49B557C92010E46DF1602972BC988BC")]
        public interface IEcsRunner : IEcsProcess
        {
            EcsPipeline Pipeline { get; }
            Type Interface { get; }
            EcsProcessRaw ProcessRaw { get; }
            bool IsEmpty { get; }
        }

        [MetaColor(MetaColor.DragonRose)]
        [MetaGroup(EcsConsts.PACK_GROUP, EcsConsts.OTHER_GROUP)]
        [MetaDescription(EcsConsts.AUTHOR, "...")]
        [MetaTags(MetaTags.HIDDEN)]
        [MetaID("DragonECS_7DB3557C9201F85E0E1C17D7B19D9CEE")]
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


            #region RunHelper
#if DEBUG
            public
#else
            public readonly
#endif
                struct RunHelper
            {
                private readonly EcsProcess<TProcess> _process;
#if DEBUG
                private Delegate _cacheCheck;
                private bool _cacheCheckInit;
                private readonly EcsProfilerMarker[] _markers;
#endif

                #region Constructors
                public RunHelper(EcsRunner<TProcess> runner) : this(runner,
#if DEBUG
                    typeof(TProcess).ToMeta().Name)
#else
                    string.Empty)
#endif
                { }

                public RunHelper(EcsRunner<TProcess> runner, string methodName)
                {
                    _process = runner.Process;
#if DEBUG
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
#if DEBUG
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
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Run(Action<TProcess> translationCallback)
                {
#if DEBUG
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
#if DRAGONECS_DISABLE_CATH_EXCEPTIONS
                            throw e;
#else
                            EcsDebug.PrintError(e);
#endif
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
#if DRAGONECS_DISABLE_CATH_EXCEPTIONS
                            throw e;
#else
                            EcsDebug.PrintError(e);
#endif
                        }
                    }
#endif
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Run<TData>(ActionWithData<TProcess, TData> translationCallback, ref TData data)
                {
#if DEBUG
                    CheckCache(translationCallback);
                    for (int i = 0, n = _process.Length < _markers.Length ? _process.Length : _markers.Length; i < n; i++)
                    {
                        _markers[i].Begin();
                        try
                        {
                            translationCallback(_process[i], ref data);
                        }
                        catch (Exception e)
                        {
#if DRAGONECS_DISABLE_CATH_EXCEPTIONS
                            throw e;
#else
                            EcsDebug.PrintError(e);
#endif
                        }
                        _markers[i].End();
                    }
#else
                    foreach (var item in _process)
                    {
                        try
                        {
                            translationCallback(item, ref data);
                        }
                        catch (Exception e)
                        {
#if DRAGONECS_DISABLE_CATH_EXCEPTIONS
                            throw e;
#else
                            EcsDebug.PrintError(e);
#endif
                        }
                    }
#endif
                }
                #endregion
            }
            #endregion

            #region RunHelperWithFinally
#if DEBUG
            public
#else
            public readonly
#endif
                struct RunHelperWithFinally<TProcessFinally> where TProcessFinally : class, IEcsProcess
            {
                private readonly Pair[] _pairs;
#if DEBUG
                private Delegate _cacheCheck;
                private Delegate _cacheCheckF;
                private bool _cacheCheckInit;
                private readonly EcsProfilerMarker[] _markers;
#endif

                #region Constructors
                public RunHelperWithFinally(EcsRunner<TProcess> runner) : this(runner,
#if DEBUG
                    typeof(TProcess).ToMeta().Name)
#else
                    string.Empty)
#endif
                { }

                public RunHelperWithFinally(EcsRunner<TProcess> runner, string methodName)
                {
                    _pairs = new Pair[runner.Process.Length];
                    for (int i = 0; i < runner.Process.Length; i++)
                    {
                        _pairs[i] = new Pair(runner.Process[i]);
                    }
#if DEBUG
                    _cacheCheck = null;
                    _cacheCheckF = null;
                    _cacheCheckInit = false;
                    _markers = new EcsProfilerMarker[_pairs.Length];
                    for (int i = 0; i < _pairs.Length; i++)
                    {
                        _markers[i] = new EcsProfilerMarker($"{_pairs[i].run.GetMeta().Name}.{methodName}");
                    }
#endif
                }
                #endregion

                #region Utils
                private readonly struct Pair
                {
                    public readonly TProcess run;
                    public readonly TProcessFinally runFinally;
                    public Pair(TProcess run)
                    {
                        this.run = run;
                        runFinally = run as TProcessFinally;
                    }
                }
#if DEBUG
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private void CheckCache(Delegate d, Delegate df)
                {
                    if (_cacheCheckInit == false)
                    {
                        if (_cacheCheck == null)
                        {
                            _cacheCheck = d;
                            _cacheCheckF = df;
                        }
                        else
                        {
                            if (ReferenceEquals(_cacheCheck, d) == false || ReferenceEquals(_cacheCheckF, df) == false)
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
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Run(
                    Action<TProcess> translationCallback,
                    Action<TProcessFinally> translationFinnalyCallback)
                {
#if DEBUG
                    CheckCache(translationCallback, translationFinnalyCallback);
                    for (int i = 0, n = _pairs.Length < _markers.Length ? _pairs.Length : _markers.Length; i < n; i++)
                    {
                        var pair = _pairs[i];
                        _markers[i].Begin();
                        try
                        {
                            translationCallback(pair.run);
                        }
                        catch (Exception e)
                        {
#if DRAGONECS_DISABLE_CATH_EXCEPTIONS
                            throw e;
#else
                            EcsDebug.PrintError(e);
#endif
                        }
                        finally
                        {
                            if (pair.runFinally != null)
                            {
                                translationFinnalyCallback(pair.runFinally);
                            }
                        }
                        _markers[i].End();
                    }
#else
                    foreach (var item in _pairs)
                    {
                        try
                        {
                            translationCallback(item.run);
                        }
                        catch (Exception e)
                        {
#if DRAGONECS_DISABLE_CATH_EXCEPTIONS
                            throw e;
#else
                            EcsDebug.PrintError(e);
#endif
                        }
                        finally
                        {
                            translationFinnalyCallback(item.runFinally);
                        }
                    }
#endif
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Run<TData>(
                    ActionWithData<TProcess, TData> translationCallback,
                    ActionWithData<TProcessFinally, TData> translationFinnalyCallback,
                    ref TData data)
                {
#if DEBUG
                    CheckCache(translationCallback, translationFinnalyCallback);
                    for (int i = 0, n = _pairs.Length < _markers.Length ? _pairs.Length : _markers.Length; i < n; i++)
                    {
                        var pair = _pairs[i];
                        _markers[i].Begin();
                        try
                        {
                            translationCallback(pair.run, ref data);
                        }
                        catch (Exception e)
                        {
#if DRAGONECS_DISABLE_CATH_EXCEPTIONS
                            throw e;
#else
                            EcsDebug.PrintError(e);
#endif
                        }
                        finally
                        {
                            if (pair.runFinally != null)
                            {
                                translationFinnalyCallback(pair.runFinally, ref data);
                            }
                        }
                        _markers[i].End();
                    }
#else
                    foreach (var pair in _pairs)
                    {
                        try
                        {
                            translationCallback(pair.run, ref data);
                        }
                        catch (Exception e)
                        {
#if DRAGONECS_DISABLE_CATH_EXCEPTIONS
                            throw e;
#else
                            EcsDebug.PrintError(e);
#endif
                        }
                        finally
                        {
                            if (pair.runFinally != null)
                            {
                                translationFinnalyCallback(pair.runFinally, ref data);
                            }
                        }
                    }
#endif
                }
                #endregion
            }
            #endregion

            //----
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