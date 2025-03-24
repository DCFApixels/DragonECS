#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static DCFApixels.DragonECS.EcsConsts;

namespace DCFApixels.DragonECS
{
    #region EcsProfilerMarker
    public readonly struct EcsProfilerMarker
    {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
        public readonly int id;
#endif
        internal EcsProfilerMarker(int id)
        {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
            this.id = id;
#endif
        }
        public EcsProfilerMarker(string name)
        {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
            id = DebugService.CurrentThreadInstance.RegisterMark(name);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin()
        {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
            DebugService.CurrentThreadInstance.ProfilerMarkBegin(id);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void End()
        {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
            DebugService.CurrentThreadInstance.ProfilerMarkEnd(id);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AutoScope Auto()
        {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
            return new AutoScope(id);
#else
            return default;
#endif
        }
        public readonly ref struct AutoScope
        {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
            private readonly int _id;
#endif
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public AutoScope(int id)
            {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
                _id = id;
                DebugService.CurrentThreadInstance.ProfilerMarkBegin(id);
#endif
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
                DebugService.CurrentThreadInstance.ProfilerMarkEnd(_id);
#endif
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator EcsProfilerMarker(string markerName) { return new EcsProfilerMarker(markerName); }
    }
    #endregion

    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(PACK_GROUP, DEBUG_GROUP)]
    [MetaDescription(AUTHOR, "Debugging utility. To modify or change the behavior, create a new class inherited from DebugService and set this service using DebugService.Set<T>().")]
    [MetaID("DragonECS_10A4587C92013B55820D8604D718A1C3")]
    public static class EcsDebug
    {
        #region Set
        public static void Set<T>() where T : DebugService, new()
        {
            DebugService.Set<T>();
        }
        public static void Set(DebugService service)
        {
            DebugService.Set(service);
        }
        #endregion

        #region Print
#if UNITY_2021_3_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintWarning(object v)
        {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
            OnPrint(DEBUG_WARNING_TAG, v);
            DebugService.CurrentThreadInstance.PrintWarning(v);
#endif
        }
#if UNITY_2021_3_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintError(object v)
        {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
            OnPrint(DEBUG_ERROR_TAG, v);
            DebugService.CurrentThreadInstance.PrintError(v);
#endif
        }
#if UNITY_2021_3_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintErrorAndBreak(object v)
        {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
            OnPrint(DEBUG_ERROR_TAG, v);
            DebugService.CurrentThreadInstance.PrintErrorAndBreak(v);
#endif
        }
#if UNITY_2021_3_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintPass(object v)
        {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
            OnPrint(DEBUG_PASS_TAG, v);
            DebugService.CurrentThreadInstance.PrintPass(v);
#endif
        }
#if UNITY_2021_3_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Print()
        {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
            OnPrint(string.Empty, null);
            DebugService.CurrentThreadInstance.Print();
#endif
        }
#if UNITY_2021_3_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Print(object v)
        {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
            OnPrint(string.Empty, v);
            DebugService.CurrentThreadInstance.Print(v);
#endif
        }
#if UNITY_2021_3_OR_NEWER
        [UnityEngine.HideInCallstack]
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Print(string tag, object v)
        {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
            OnPrint(tag, v);
            DebugService.CurrentThreadInstance.Print(tag, v);
#endif
        }
        #endregion

        #region Other
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Break()
        {
#if DEBUG || DRAGONECS_ENABLE_DEBUG_SERVICE
            DebugService.CurrentThreadInstance.Break();
#endif
        }
        #endregion

        #region Events
        public static OnPrintHandler OnPrint = delegate { };
        public delegate void OnPrintHandler(string tag, object v);
        #endregion
    }

    //------------------------------------------------------------------------------------------------------------//

    public abstract class DebugService
    {
        private static DebugService _instance;
        private readonly static object _lock = new object();

        private readonly static HashSet<DebugService> _threadServiceClonesSet = new HashSet<DebugService>();

        [ThreadStatic]
        private static DebugService _currentThreadInstanceClone;
        [ThreadStatic]
        private static DebugService _currentThreadInstance; // для сравнения

        private readonly static IdDispenser _idDispenser = new IdDispenser(16, 0);
        private readonly static Dictionary<string, int> _nameIdTable = new Dictionary<string, int>();

        #region Properties
        public static bool IsNullOrDefault
        {
            get { return _instance == null || _instance is NullDebugService || _instance is DefaultDebugService; }
        }
        public static DebugService Instance
        {
            get { return _instance; }
        }
        public static DebugService CurrentThreadInstance
        {// ts завист от Set
            get
            {
                if (_currentThreadInstance != _instance)
                {
                    lock (_lock)
                    {
                        if (_currentThreadInstance != _instance)
                        {
                            _currentThreadInstanceClone = _instance.CreateThreadInstance();
                            _threadServiceClonesSet.Add(_currentThreadInstanceClone);
                            _currentThreadInstance = _instance;

                            foreach (var record in _nameIdTable)
                            {
                                _currentThreadInstanceClone.OnNewProfilerMark(record.Value, record.Key);
                            }
                        }
                    }
                }
                return _currentThreadInstanceClone;
            }
        }
        #endregion

        #region Static Constructor
        static DebugService()
        {
#if !UNITY_5_3_OR_NEWER
            Set(new NullDebugService());
#else
            Set(new DefaultDebugService());
#endif
        }
        #endregion

        #region Set
        public static void Set<T>() where T : DebugService, new()
        {// ts
            lock (_lock)
            {
                if (CurrentThreadInstance is T == false)
                {
                    Set(new T());
                }
            }
        }
        public static void Set(DebugService service)
        {// ts
            lock (_lock)
            {
                if (service == null)
                {
                    service = new NullDebugService();
                }
                if (_instance != service)
                {
                    var oldService = _instance;
                    _instance = service;
                    foreach (var record in _nameIdTable)
                    {
                        service.OnNewProfilerMark(record.Value, record.Key);
                    }
                    oldService?.OnDisableBaseService(service);
                    service.OnEnableBaseService(oldService);
                    OnServiceChanged(service);
                }
            }
        }
        #endregion

        #region OnEnable/OnDisable/CreateThreadInstance
        protected virtual void OnEnableBaseService(DebugService prevService) { }
        protected virtual void OnDisableBaseService(DebugService nextService) { }
        protected abstract DebugService CreateThreadInstance();
        #endregion

        #region Print/Break
        public abstract void Print(string tag, object v);
        public abstract void Break();
        #endregion

        #region ProfilerMarkesrs
        public int RegisterMark(string name)
        {
            int id;
            if (_nameIdTable.TryGetValue(name, out id) == false)
            {
                lock (_lock)
                {
                    if (_nameIdTable.TryGetValue(name, out id) == false)
                    {
                        id = _idDispenser.UseFree();
                        _nameIdTable.Add(name, id);
                        foreach (var service in _threadServiceClonesSet)
                        {
                            service.OnNewProfilerMark(id, name);
                        }
                    }
                }
            }
            return id;
        }
        public void DeleteMark(string name)
        {
            lock (_lock)
            {
                int id = _nameIdTable[name];
                _nameIdTable.Remove(name);
                _idDispenser.Release(id);
                foreach (var service in _threadServiceClonesSet)
                {
                    service.OnNewProfilerMark(id, name);
                }
                OnDelProfilerMark(id);
            }
        }

        protected abstract void OnNewProfilerMark(int id, string name);
        protected abstract void OnDelProfilerMark(int id);

        public abstract void ProfilerMarkBegin(int id);
        public abstract void ProfilerMarkEnd(int id);
        #endregion

        #region Utils
        protected static string AutoConvertObjectToString(object o)
        {
            if (o is string str)
            {
                return str;
            }
            if (o is IEnumerable enumerable)
            {
                return string.Join(", ", enumerable.Cast<object>());
            }
            return o.ToString();
        }

        public readonly struct MarkerInfo
        {
            public readonly string Name;
            public readonly int ID;
            public MarkerInfo(string name, int iD)
            {
                Name = name;
                ID = iD;
            }
            public override string ToString() { return this.AutoToString(); }
        }
        #endregion

        #region Events
        public static event OnServiceChangedHandler OnServiceChanged = delegate { };

        public delegate void OnServiceChangedHandler(DebugService service);
        #endregion
    }
}

namespace DCFApixels.DragonECS.Core
{
    #region DebugServiceExtensions
    public static class DebugServiceExtensions
    {
        public static void PrintWarning(this DebugService self, object v)
        {
            self.Print(DEBUG_WARNING_TAG, v);
        }
        public static void PrintError(this DebugService self, object v)
        {
            self.Print(DEBUG_ERROR_TAG, v);
        }
        public static void PrintErrorAndBreak(this DebugService self, object v)
        {
            self.Print(DEBUG_ERROR_TAG, v);
            self.Break();
        }
        public static void PrintPass(this DebugService self, object v)
        {
            self.Print(DEBUG_PASS_TAG, v);
        }
        public static void Print(this DebugService self, object v)
        {
            self.Print(null, v);
        }
        public static void Print(this DebugService self)
        {
            self.Print("");
        }
        //TODO PrintJson возможно будет добавлено когда-то
    }
    #endregion

    #region DefaultServices
    //------------------------------------------------------------------------------------------------------------//

    public sealed class NullDebugService : DebugService
    {
        protected sealed override DebugService CreateThreadInstance() { return this; }
        public sealed override void Break() { }
        public sealed override void Print(string tag, object v) { }
        public sealed override void ProfilerMarkBegin(int id) { }
        public sealed override void ProfilerMarkEnd(int id) { }
        protected sealed override void OnDelProfilerMark(int id) { }
        protected sealed override void OnNewProfilerMark(int id, string name) { }
    }

    //------------------------------------------------------------------------------------------------------------//

    public sealed class DefaultDebugService : DebugService
    {
        private const string PROFILER_MARKER = "ProfilerMark";
        private const string PROFILER_MARKER_CACHE = "[" + PROFILER_MARKER + "] ";

        private MarkerData[] _stopwatchs = new MarkerData[64];
        private char[] _buffer = new char[128];

        private object _lock = new object();

        public DefaultDebugService()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }
        protected sealed override DebugService CreateThreadInstance()
        {
            return new DefaultDebugService();
        }
        public sealed override void Print(string tag, object v)
        {
            if (string.IsNullOrEmpty(tag))
            {
                Console.WriteLine(AutoConvertObjectToString(v));
            }
            else
            {
                var color = Console.ForegroundColor;
                switch (tag)
                {
                    case DEBUG_ERROR_TAG:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case DEBUG_WARNING_TAG:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case DEBUG_PASS_TAG:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                }
                Console.WriteLine($"[{tag}] {AutoConvertObjectToString(v)}");
                Console.ForegroundColor = color;
            }
        }
        public sealed override void Break()
        {
            lock (_lock)
            {
                var color = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Press Enter to сontinue.");
                Console.ForegroundColor = color;
                Console.ReadKey();
            }
        }

        public sealed override void ProfilerMarkBegin(int id)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            _stopwatchs[id].Stopwatch.Start();

            Console.Write(PROFILER_MARKER_CACHE);
            Console.Write(_stopwatchs[id].Name);
            Console.WriteLine("> ");

            Console.ForegroundColor = color;
        }
        public sealed override void ProfilerMarkEnd(int id)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            _stopwatchs[id].Stopwatch.Stop();
            var time = _stopwatchs[id].Stopwatch.Elapsed;
            _stopwatchs[id].Stopwatch.Reset();

            Console.Write(PROFILER_MARKER_CACHE);
            Console.Write("> ");
            Console.Write(_stopwatchs[id].Name);
            Console.Write(" s:");

            int written = 0;
            ConvertDoubleToText(time.TotalSeconds, _buffer, ref written);
            Console.WriteLine(_buffer, 0, written);

            Console.ForegroundColor = color;
        }

        protected sealed override void OnDelProfilerMark(int id)
        {
            _stopwatchs[id] = default;
        }
        protected sealed override void OnNewProfilerMark(int id, string name)
        {
            if (id >= _stopwatchs.Length)
            {
                Array.Resize(ref _stopwatchs, id << 1);
            }
            _stopwatchs[id] = new MarkerData(new System.Diagnostics.Stopwatch(), name, id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ConvertDoubleToText(double value, char[] stringBuffer, ref int written)
        {
            int bufferLength = stringBuffer.Length - 1;

            decimal decimalValue = (decimal)value;
            int intValue = (int)decimalValue;
            decimal decimalPartValue = decimalValue - intValue;

            int index = written;

            if (intValue == 0)
            {
                stringBuffer[index++] = '0';
            }
            else
            {
                while (intValue > 0)
                {
                    int digit = intValue % 10;
                    stringBuffer[index++] = (char)('0' + digit);
                    intValue /= 10;
                }

                Array.Reverse(stringBuffer, 0, index);
            }

            if (decimalPartValue != 0)
            {
                stringBuffer[index++] = '.';
            }

            int pathBufferLength = bufferLength - index;
            int zeroPartLength = 0;
            for (int i = 0; i < pathBufferLength; i++)
            {
                decimalPartValue = 10 * decimalPartValue;
                int digit = (int)decimalPartValue;
                if (digit == 0)
                {
                    zeroPartLength++;
                }
                else
                {
                    zeroPartLength = 0;
                }
                stringBuffer[index++] = (char)('0' + digit);
                decimalPartValue -= digit;
            }

            written = bufferLength - zeroPartLength;
        }

        private readonly struct MarkerData
        {
            public readonly System.Diagnostics.Stopwatch Stopwatch;
            public readonly string Name;
            public readonly int ID;
            public MarkerData(System.Diagnostics.Stopwatch stopwatch, string name, int id)
            {
                Stopwatch = stopwatch;
                Name = name;
                ID = id;
            }
            public override string ToString()
            {
                return this.AutoToString();
            }
        }
    }
    #endregion
}