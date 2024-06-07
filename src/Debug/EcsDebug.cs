using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public readonly struct EcsProfilerMarker
    {
        public readonly int id;
        internal EcsProfilerMarker(int id) { this.id = id; }
        public EcsProfilerMarker(string name) { id = DebugService.Instance.RegisterMark(name); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin() { DebugService.Instance.ProfilerMarkBegin(id); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void End() { DebugService.Instance.ProfilerMarkEnd(id); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AutoScope Auto() { return new AutoScope(id); }
        public readonly ref struct AutoScope
        {
            private readonly int _id;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public AutoScope(int id)
            {
                _id = id;
                DebugService.Instance.ProfilerMarkBegin(id);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                DebugService.Instance.ProfilerMarkEnd(_id);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator EcsProfilerMarker(string markerName) { return new EcsProfilerMarker(markerName); }
    }

    public static class EcsDebug
    {
        public const string WARNING_TAG = EcsConsts.DEBUG_WARNING_TAG;
        public const string ERROR_TAG = EcsConsts.DEBUG_ERROR_TAG;
        public const string PASS_TAG = EcsConsts.DEBUG_PASS_TAG;

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
        public static DebugService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DefaultDebugService();
                }
                return _instance;
            }
        }

        public static void Set<T>() where T : DebugService, new()
        {
            Set(new T());
        }
        public static void Set(DebugService service)
        {
            _instance = service;
            OnServiceChanged(_instance);
        }

        public static Action<DebugService> OnServiceChanged = delegate { };

        private IdDispenser _idDispenser = new IdDispenser(4, -1);
        private Dictionary<string, int> _nameIdTable = new Dictionary<string, int>();
        public abstract void Print(string tag, object v);
        public abstract void Break();
        public int RegisterMark(string name)
        {
            int id;
            if (!_nameIdTable.TryGetValue(name, out id))
            {
                id = _idDispenser.UseFree();
                _nameIdTable.Add(name, id);
            }
            OnNewProfilerMark(id, name);
            return id;
        }
        public void DeleteMark(string name)
        {
            int id = _nameIdTable[name];
            _nameIdTable.Remove(name);
            _idDispenser.Release(id);
            OnDelProfilerMark(id);
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
    }
    public sealed class DefaultDebugService : DebugService
    {
        private const string PROFILER_MARKER = "ProfilerMark";
        private struct MarkerData
        {
            public Stopwatch stopwatch;
            public string name;
            public MarkerData(Stopwatch stopwatch, string name)
            {
                this.stopwatch = stopwatch;
                this.name = name;
            }
        }
        private MarkerData[] _stopwatchs;
        [ThreadStatic]
        private static char[] _buffer;
        public DefaultDebugService()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER
            _stopwatchs = new MarkerData[64];
#endif
        }

        public override void Print(string tag, object v)
        {
            if (string.IsNullOrEmpty(tag))
            {
                Console.WriteLine(v);
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
                Console.WriteLine($"[{tag}] {v}");
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

        private const string PROFILER_MARKER_CACHE = "[" + PROFILER_MARKER + "] ";
        public override void ProfilerMarkBegin(int id)
        {
#if ((DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER) && !UNITY_5_3_OR_NEWER

            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            _stopwatchs[id].stopwatch.Start();

            Console.Write(PROFILER_MARKER_CACHE);
            Console.Write(_stopwatchs[id].name);
            Console.WriteLine("> ");

            Console.ForegroundColor = color;
#endif
        }
        public override void ProfilerMarkEnd(int id)
        {
#if ((DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_DEBUGGER) && !UNITY_5_3_OR_NEWER
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            _stopwatchs[id].stopwatch.Stop();
            var time = _stopwatchs[id].stopwatch.Elapsed;
            _stopwatchs[id].stopwatch.Reset();

            Console.Write(PROFILER_MARKER_CACHE);
            Console.Write("> ");
            Console.Write(_stopwatchs[id].name);
            Console.Write(" s:");

            int written = 0;
            if (_buffer == null) { _buffer = new char[128]; }
            ConvertDoubleToText(time.TotalSeconds, _buffer, ref written);
            Console.WriteLine(_buffer, 0, written);

            Console.ForegroundColor = color;
#endif
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
            _stopwatchs[id] = new MarkerData(new Stopwatch(), name);
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
    }
}