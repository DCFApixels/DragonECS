using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public readonly struct EcsProfilerMarker
    {
        public readonly int id;
        public EcsProfilerMarker(int id) => this.id = id;
        public EcsProfilerMarker(string name) => id = EcsDebug.RegisterMark(name);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Begin() => EcsDebug.ProfileMarkBegin(id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void End() => EcsDebug.ProfileMarkEnd(id);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AutoScope Auto() => new AutoScope(id);

        public readonly struct AutoScope : IDisposable
        {
            private readonly int id;
            public AutoScope(int id)
            {
                this.id = id;
                EcsDebug.ProfileMarkBegin(id);
            }
            public void Dispose()
            {
                EcsDebug.ProfileMarkEnd(id);
            }
        }
    }

    public static class EcsDebug
    {
        public static void Set<T>() where T : DebugService, new() => DebugService.Set<T>();
        public static void Set(DebugService service) => DebugService.Set(service);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Print(object v) => DebugService.Instance.Print(v);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Print(string tag, object v)
        {
#if !DISABLE_ECS_DEBUG
            DebugService.Instance.Print(tag, v);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RegisterMark(string name)
        {
#if !DISABLE_ECS_DEBUG
            return DebugService.Instance.RegisterMark(name);
#else
            return 0;
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DeleteMark(string name)
        {
#if !DISABLE_ECS_DEBUG
            DebugService.Instance.DeleteMark(name);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ProfileMarkBegin(int id)
        {
#if !DISABLE_ECS_DEBUG
            DebugService.Instance.ProfileMarkBegin(id);
#endif
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ProfileMarkEnd(int id)
        {
#if !DISABLE_ECS_DEBUG
            return DebugService.Instance.ProfileMarkEnd(id);
#else
            return 0;
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

        public static void Set<T>() where T : DebugService, new() => Set(new T());
        public static void Set(DebugService service)
        {
            _instance = service;
            OnServiceChanged(_instance);
        }

        public static Action<DebugService> OnServiceChanged = delegate { };

        private IntDispenser _idDispenser = new IntDispenser(0);
        private Dictionary<string, int> _nameIdTable = new Dictionary<string, int>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Print(object v) => Print(null, v);
        public abstract void Print(string tag, object v);

        public int RegisterMark(string name)
        {
            int id;
            if(!_nameIdTable.TryGetValue(name, out id))
            {
                id = _idDispenser.GetFree();
                _nameIdTable.Add(name, id);
            }
            OnNewMark(id, name);
            return id;
        }
        public void DeleteMark(string name)
        {
            int id = _nameIdTable[name];
            _nameIdTable.Remove(name);
            _idDispenser.Release(id);
            OnDelMark(id);

        }

        protected abstract void OnNewMark(int id, string name);
        protected abstract void OnDelMark(int id);

        public abstract void ProfileMarkBegin(int id);
        public abstract double ProfileMarkEnd(int id);
    }

    public sealed class DefaultDebugService : DebugService
    {
        private Stopwatch[] _stopwatchs;

        public DefaultDebugService()
        {
#if !DISABLE_ECS_DEBUG
            _stopwatchs = new Stopwatch[64];
#endif
        }

        public override void Print(string tag, object v)
        {
            Console.WriteLine($"[{tag}] {v}");
        }

        public override void ProfileMarkBegin(int id)
        {
            _stopwatchs[id].Start();
        }

        public override double ProfileMarkEnd(int id)
        {
            _stopwatchs[id].Stop();
            var time = _stopwatchs[id].Elapsed;
            _stopwatchs[id].Reset();
            return time.TotalSeconds;
        }

        protected override void OnDelMark(int id)
        {
            _stopwatchs[id] = null;
        }

        protected override void OnNewMark(int id, string name)
        {
            if (id >= _stopwatchs.Length) Array.Resize(ref _stopwatchs, _stopwatchs.Length << 1);
            _stopwatchs[id] = new Stopwatch();
        }
    }
}