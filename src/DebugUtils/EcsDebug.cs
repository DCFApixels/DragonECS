using DCFApixels.DragonECS.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    using static EcsConsts;
    public readonly struct EcsProfilerMarker
    {
#if ((DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER)
        public readonly int id;
#endif
        internal EcsProfilerMarker(int id)
        {
#if ((DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER)
            this.id = id;
#endif
        }
        public EcsProfilerMarker(string name)
        {
#if ((DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER)
            id = DebugService.Instance.RegisterMark(name);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin()
        {
#if ((DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER)
            DebugService.Instance.ProfilerMarkBegin(id);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void End()
        {
#if ((DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER)
            DebugService.Instance.ProfilerMarkEnd(id);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AutoScope Auto()
        {
#if ((DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER)
            return new AutoScope(id);
#else
            return default;
#endif
        }
        public readonly ref struct AutoScope
        {
#if ((DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER)
            private readonly int _id;
#endif
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public AutoScope(int id)
            {
#if ((DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER)
                _id = id;
                DebugService.Instance.ProfilerMarkBegin(id);
#endif
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
#if ((DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER)
                DebugService.Instance.ProfilerMarkEnd(_id);
#endif
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator EcsProfilerMarker(string markerName) { return new EcsProfilerMarker(markerName); }
    }

    [MetaColor(MetaColor.DragonRose)]
    [MetaGroup(PACK_GROUP, DEBUG_GROUP)]
    [MetaDescription(AUTHOR, "Debugging utility. To modify or change the behavior, create a new class inherited from DebugService and set this service using DebugService.Set<T>().")]
    public static class EcsDebug
    {
        public static void Set<T>() where T : DebugService, new()
        {
            DebugService.Set<T>();
        }
        public static void Set(DebugService service)
        {
            DebugService.Set(service);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintWarning(object v)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER
            DebugService.Instance.PrintWarning(v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintError(object v)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER
            DebugService.Instance.PrintError(v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintErrorAndBreak(object v)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER
            DebugService.Instance.PrintErrorAndBreak(v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintPass(object v)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER
            DebugService.Instance.PrintPass(v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Print()
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER
            DebugService.Instance.Print();
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Print(object v)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER
            DebugService.Instance.Print(v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Print(string tag, object v)
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER
            DebugService.Instance.Print(tag, v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Break()
        {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER
            DebugService.Instance.Break();
#endif
        }
    }

    public abstract class DebugService
    {
        private static DebugService _instance;
        private static object _lock = new object();

        public static DebugService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance = new DefaultDebugService();
                    }
                }
                return _instance;
            }
        }

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
        public IEnumerable<MarkerInfo> MarkerInfos
        {
            get { return _nameIdTable.Select(o => new MarkerInfo(o.Key, o.Value)); }
        }

        public static void Set<T>() where T : DebugService, new()
        {
            lock (_lock)
            {
                if (Instance is T == false)
                {
                    Set(new T());
                }
            }
        }
        public static void Set(DebugService service)
        {
            lock (_lock)
            {
                if (_instance != service)
                {
                    var oldService = _instance;
                    _instance = service;
                    if (_instance != null)
                    { //TODO Так, всеже треды влияют друг на друга, скоерее всего проблема в использовании _nameIdTable/ Так вроде пофиксил, но не понял как конкретно
                        foreach (var info in oldService.MarkerInfos)
                        {
                            service._idDispenser.Use(info.ID);
                            service._nameIdTable.TryAdd(info.Name, info.ID);
                            service.OnNewProfilerMark(info.ID, info.Name);
                        }
                    }
                    service.OnServiceSetup(oldService);
                    OnServiceChanged(service);
                }
            }
        }
        protected virtual void OnServiceSetup(DebugService oldService) { }

        public static Action<DebugService> OnServiceChanged = delegate { };

        private IdDispenser _idDispenser = new IdDispenser(16, 0);
        private Dictionary<string, int> _nameIdTable = new Dictionary<string, int>();
        public abstract void Print(string tag, object v);
        public abstract void Break();
        public int RegisterMark(string name)
        {
            int id;
            if (!_nameIdTable.TryGetValue(name, out id))
            {
                lock (_lock)
                {
                    if (!_nameIdTable.TryGetValue(name, out id))
                    {
                        id = _idDispenser.UseFree();
                        _nameIdTable.Add(name, id);
                        OnNewProfilerMark(id, name);
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
                OnDelProfilerMark(id);
            }
        }

        protected abstract void OnNewProfilerMark(int id, string name);
        protected abstract void OnDelProfilerMark(int id);

        public abstract void ProfilerMarkBegin(int id);
        public abstract void ProfilerMarkEnd(int id);
    }
    public static class DebugServiceExtensions
    {
        public static void PrintWarning(this DebugService self, object v)
        {
            self.Print(EcsConsts.DEBUG_WARNING_TAG, v);
        }
        public static void PrintError(this DebugService self, object v)
        {
            self.Print(EcsConsts.DEBUG_ERROR_TAG, v);
        }
        public static void PrintErrorAndBreak(this DebugService self, object v)
        {
            self.Print(EcsConsts.DEBUG_ERROR_TAG, v);
            self.Break();
        }
        public static void PrintPass(this DebugService self, object v)
        {
            self.Print(EcsConsts.DEBUG_PASS_TAG, v);
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
    public sealed class DefaultDebugService : DebugService
    {
#if !UNITY_5_3_OR_NEWER
        private const string PROFILER_MARKER = "ProfilerMark";
        private const string PROFILER_MARKER_CACHE = "[" + PROFILER_MARKER + "] ";

        private readonly struct MarkerData
        {
            public readonly Stopwatch Stopwatch;
            public readonly string Name;
            public readonly int ID;
            public MarkerData(Stopwatch stopwatch, string name, int id)
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
        private MarkerData[] _stopwatchs;
        [ThreadStatic]
        private static char[] _buffer;

        public DefaultDebugService()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            _stopwatchs = new MarkerData[64];
        }

        public override void Print(string tag, object v)
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
                    case EcsDebug.ERROR_TAG:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case EcsDebug.WARNING_TAG:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case EcsDebug.PASS_TAG:
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                }
                Console.WriteLine($"[{tag}] {AutoConvertObjectToString(v)}");
                Console.ForegroundColor = color;
            }
        }
        public override void Break()
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Press Enter to сontinue.");
            Console.ReadKey();
            Console.ForegroundColor = color;
        }

        public override void ProfilerMarkBegin(int id)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            _stopwatchs[id].Stopwatch.Start();

            Console.Write(PROFILER_MARKER_CACHE);
            Console.Write(_stopwatchs[id].Name);
            Console.WriteLine("> ");

            Console.ForegroundColor = color;
        }
        public override void ProfilerMarkEnd(int id)
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
            if (_buffer == null) { _buffer = new char[128]; }
            ConvertDoubleToText(time.TotalSeconds, _buffer, ref written);
            Console.WriteLine(_buffer, 0, written);

            Console.ForegroundColor = color;
        }

        protected override void OnDelProfilerMark(int id)
        {
            _stopwatchs[id] = default;
        }
        protected override void OnNewProfilerMark(int id, string name)
        {
            if (id >= _stopwatchs.Length)
            {
                Array.Resize(ref _stopwatchs, _stopwatchs.Length << 1);
            }
            _stopwatchs[id] = new MarkerData(new Stopwatch(), name, id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void ConvertDoubleToText(double value, char[] stringBuffer, ref int written)
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
#endif
        public override void Break() { }
        public override void Print(string tag, object v) { }
        public override void ProfilerMarkBegin(int id) { }
        public override void ProfilerMarkEnd(int id) { }
        protected override void OnDelProfilerMark(int id) { }
        protected override void OnNewProfilerMark(int id, string name) { }
    }
}