using DCFApixels.DragonECS.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public readonly struct EcsProfilerMarker
    {
        public readonly int id;
        internal EcsProfilerMarker(int id) => this.id = id;
        public EcsProfilerMarker(string name) => id = DebugService.Instance.RegisterMark(name);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin() => DebugService.Instance.ProfilerMarkBegin(id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void End() => DebugService.Instance.ProfilerMarkEnd(id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AutoScope Auto() => new AutoScope(id);
        public readonly ref struct AutoScope
        {
            private readonly int _id;
            public AutoScope(int id)
            {
                _id = id;
                DebugService.Instance.ProfilerMarkBegin(id);
            }
            public void Dispose() => DebugService.Instance.ProfilerMarkEnd(_id);
        }
    }

    public static class EcsDebug
    {
        public const string WARNING_TAG = EcsConsts.DEBUG_WARNING_TAG;
        public const string ERROR_TAG = EcsConsts.DEBUG_ERROR_TAG;

        public static void Set<T>() where T : DebugService, new() => DebugService.Set<T>();
        public static void Set(DebugService service) => DebugService.Set(service);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PrintWarning(object v) => Print(EcsConsts.DEBUG_WARNING_TAG, v);
        public static void PrintError(object v) => Print(EcsConsts.DEBUG_ERROR_TAG, v);
        public static void Print()
        {
#if !DISABLE_DRAGONECS_DEBUGGER
            DebugService.Instance.Print("");
#endif
        }
        public static void Print(object v)
        {
#if !DISABLE_DRAGONECS_DEBUGGER
            DebugService.Instance.Print(v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Print(string tag, object v)
        {
#if !DISABLE_DRAGONECS_DEBUGGER
            DebugService.Instance.Print(tag, v);
#endif
        }
        public static void Break()
        {
            {
#if !DISABLE_DRAGONECS_DEBUGGER
                DebugService.Instance.Break();
#endif
            }
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
                    _instance = new DefaultDebugService();
                return _instance;
            }
        }

        public static void Set<T>() where T : DebugService, new() => Set(new T());
        public static void Set(DebugService service)
        {
            _instance = service;
            OnServiceChanged(_instance);
        }

        public static Action<DebugService> OnServiceChanged = delegate { };

        private IntDispenser _idDispenser = new IntDispenser(-1);
        private Dictionary<string, int> _nameIdTable = new Dictionary<string, int>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Print(object v) => Print(null, v);
        public abstract void Print(string tag, object v);
        public abstract void Break();
        public int RegisterMark(string name)
        {
            int id;
            if (!_nameIdTable.TryGetValue(name, out id))
            {
                id = _idDispenser.GetFree();
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

    public sealed class DefaultDebugService : DebugService
    {
        private Stopwatch[] _stopwatchs;
        private string[] _stopwatchsNames;
        public DefaultDebugService()
        {
#if !DISABLE_DRAGONECS_DEBUGGER
            _stopwatchs = new Stopwatch[64];
            _stopwatchsNames = new string[64];
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
        public override void ProfilerMarkBegin(int id)
        {
            _stopwatchs[id].Start();
        }
        public override void ProfilerMarkEnd(int id)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            _stopwatchs[id].Stop();
            var time = _stopwatchs[id].Elapsed;
            _stopwatchs[id].Reset();
            Print("ProfilerMark", _stopwatchsNames[id] + " s:" + time.TotalSeconds);
            Console.ForegroundColor = color;
        }
        protected override void OnDelProfilerMark(int id)
        {
            _stopwatchs[id] = null;
        }
        protected override void OnNewProfilerMark(int id, string name)
        {
            if (id >= _stopwatchs.Length)
            {
                Array.Resize(ref _stopwatchs, _stopwatchs.Length << 1);
                Array.Resize(ref _stopwatchsNames, _stopwatchsNames.Length << 1);
            }
            _stopwatchs[id] = new Stopwatch();
            _stopwatchsNames[id] = name;
        }
    }
}