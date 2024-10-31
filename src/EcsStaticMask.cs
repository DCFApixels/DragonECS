using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS
{
#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public sealed class EcsStaticMask : IEquatable<EcsStaticMask>, IEcsComponentMask
    {
        public static readonly EcsStaticMask Empty;
        public static readonly EcsStaticMask Broken;

        private static readonly Stack<BuilderInstance> _buildersPool = new Stack<BuilderInstance>();
        private static readonly ConcurrentDictionary<Key, EcsStaticMask> _ids = new ConcurrentDictionary<Key, EcsStaticMask>();
        private static readonly IdDispenser _idDIspenser = new IdDispenser(nullID: 0);
        private static readonly object _lock = new object();

        static EcsStaticMask()
        {
            Empty = CreateMask(new Key(new EcsTypeCode[0], new EcsTypeCode[0]));
            Broken = CreateMask(new Key(new EcsTypeCode[1] { (EcsTypeCode)1 }, new EcsTypeCode[1] { (EcsTypeCode)1 }));
        }

        private readonly int _id;
        /// <summary> Sorted </summary>
        private readonly EcsTypeCode[] _inc;
        /// <summary> Sorted </summary>
        private readonly EcsTypeCode[] _exc;

        #region Properties
        public int ID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _id; }
        }
        /// <summary> Sorted set including constraints presented as global type codes. </summary>
        public ReadOnlySpan<EcsTypeCode> IncTypeCodes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _inc; }
        }
        /// <summary> Sorted set excluding constraints presented as global type codes. </summary>
        public ReadOnlySpan<EcsTypeCode> ExcTypeCodes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _exc; }
        }
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _inc.Length == 0 && _exc.Length == 0; }
        }
        public bool IsBroken
        {
            get { return (_inc.Length & _exc.Length) == 1 && _inc[0] == _exc[0]; }
        }
        #endregion

        #region Constrcutors
        private static EcsStaticMask CreateMask(Key key)
        {
            if (_ids.TryGetValue(key, out EcsStaticMask result) == false)
            {
                lock (_lock)
                {
                    if (_ids.TryGetValue(key, out result) == false)
                    {
                        result = new EcsStaticMask(_idDIspenser.UseFree(), key);
                        _ids[key] = result;
                    }
                }
            }
            return result;
        }
        public static Builder New() { return Builder.New(); }
        public static Builder Inc<T>() { return Builder.New().Inc<T>(); }
        public static Builder Exc<T>() { return Builder.New().Exc<T>(); }
        public static Builder Inc(Type type) { return Builder.New().Inc(type); }
        public static Builder Exc(Type type) { return Builder.New().Exc(type); }
        public static Builder Inc(EcsTypeCode typeCode) { return Builder.New().Inc(typeCode); }
        public static Builder Exc(EcsTypeCode typeCode) { return Builder.New().Exc(typeCode); }
        private EcsStaticMask(int id, Key key)
        {
            _id = id;
            _inc = key.inc;
            _exc = key.exc;
        }
        #endregion

        #region Methods
        public EcsMask ToMask(EcsWorld world)
        {
            return EcsMask.FromStatic(world, this);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(EcsStaticMask other) { return _id == other._id; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return _id; }
        public override bool Equals(object obj) { return Equals((EcsStaticMask)obj); }
        public override string ToString() { return CreateLogString(_inc, _exc); }
        #endregion

        #region Builder
        private readonly struct Key : IEquatable<Key>
        {
            public readonly EcsTypeCode[] inc;
            public readonly EcsTypeCode[] exc;
            public readonly int hash;

            #region Constructors
            public Key(EcsTypeCode[] inc, EcsTypeCode[] exc)
            {
                this.inc = inc;
                this.exc = exc;
                unchecked
                {
                    hash = inc.Length + exc.Length;
                    for (int i = 0, iMax = inc.Length; i < iMax; i++)
                    {
                        hash = hash * EcsConsts.MAGIC_PRIME + (int)inc[i];
                    }
                    for (int i = 0, iMax = exc.Length; i < iMax; i++)
                    {
                        hash = hash * EcsConsts.MAGIC_PRIME - (int)exc[i];
                    }
                }
            }
            #endregion

            #region Object
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(Key other)
            {
                if (inc.Length != other.inc.Length) { return false; }
                if (exc.Length != other.exc.Length) { return false; }
                for (int i = 0; i < inc.Length; i++)
                {
                    if (inc[i] != other.inc[i]) { return false; }
                }
                for (int i = 0; i < exc.Length; i++)
                {
                    if (exc[i] != other.exc[i]) { return false; }
                }
                return true;
            }
            public override bool Equals(object obj) { return Equals((Key)obj); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() { return hash; }
            #endregion
        }
        public readonly struct Builder
        {
            private readonly BuilderInstance _builder;
            private readonly int _version;

            public static Builder New()
            {
                lock (_lock)
                {
                    if (_buildersPool.TryPop(out BuilderInstance builderInstance) == false)
                    {
                        builderInstance = new BuilderInstance();
                    }
                    return new Builder(builderInstance);
                }
            }
            private Builder(BuilderInstance builder)
            {
                _builder = builder;
                _version = builder._version;
            }
            public Builder Inc<T>() { return Inc(EcsTypeCodeManager.Get<T>()); }
            public Builder Exc<T>() { return Exc(EcsTypeCodeManager.Get<T>()); }
            public Builder Inc(Type type) { return Inc(EcsTypeCodeManager.Get(type)); }
            public Builder Exc(Type type) { return Exc(EcsTypeCodeManager.Get(type)); }
            public Builder Inc(EcsTypeCode typeCode)
            {
                if (_version != _builder._version) { Throw.UndefinedException(); }
                _builder.Inc(typeCode);
                return this;
            }
            public Builder Exc(EcsTypeCode typeCode)
            {
                if (_version != _builder._version) { Throw.UndefinedException(); }
                _builder.Exc(typeCode);
                return this;
            }
            public Builder Combine(EcsStaticMask mask)
            {
                if (_version != _builder._version) { Throw.UndefinedException(); }
                _builder.Combine(mask);
                return this;
            }
            public Builder Except(EcsStaticMask mask)
            {
                if (_version != _builder._version) { Throw.UndefinedException(); }
                _builder.Except(mask);
                return this;
            }

            public EcsStaticMask Build()
            {
                if (_version != _builder._version) { Throw.UndefinedException(); }
                lock (_lock)
                {
                    _buildersPool.Push(_builder);
                    return _builder.Build();
                }
            }
        }
        private class BuilderInstance
        {
            private readonly HashSet<EcsTypeCode> _inc = new HashSet<EcsTypeCode>();
            private readonly HashSet<EcsTypeCode> _exc = new HashSet<EcsTypeCode>();
            private readonly List<Combined> _combineds = new List<Combined>();
            private bool _sortedCombinedChecker = true;
            private readonly List<Excepted> _excepteds = new List<Excepted>();

            internal int _version;

            #region Constrcutors
            internal BuilderInstance() { }
            #endregion

            #region Inc/Exc/Combine/Except
            public void Inc(EcsTypeCode typeCode)
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(typeCode) || _exc.Contains(typeCode)) { Throw.ConstraintIsAlreadyContainedInMask(); }
#endif
                _inc.Add(typeCode);
            }
            public void Exc(EcsTypeCode typeCode)
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(typeCode) || _exc.Contains(typeCode)) { Throw.ConstraintIsAlreadyContainedInMask(); }
#endif
                _exc.Add(typeCode);
            }
            public void Combine(EcsStaticMask mask, int order = 0)
            {
                if (_sortedCombinedChecker && order != 0)
                {
                    _sortedCombinedChecker = false;
                }
                _combineds.Add(new Combined(mask, order));
            }

            public void Except(EcsStaticMask mask, int order = 0)
            {
                _excepteds.Add(new Excepted(mask, order));
            }
            #endregion

            #region Build
            public EcsStaticMask Build()
            {
                HashSet<EcsTypeCode> combinedIncs = _inc;
                HashSet<EcsTypeCode> combinedExcs = _exc;

                if (_combineds.Count > 0)
                {
                    combinedIncs = new HashSet<EcsTypeCode>();
                    combinedExcs = new HashSet<EcsTypeCode>();
                    if (_sortedCombinedChecker == false)
                    {
                        _combineds.Sort((a, b) => a.order - b.order);
                    }
                    foreach (var item in _combineds)
                    {
                        EcsStaticMask submask = item.mask;
                        combinedIncs.ExceptWith(submask._exc);//удаляю конфликтующие ограничения
                        combinedExcs.ExceptWith(submask._inc);//удаляю конфликтующие ограничения
                        combinedIncs.UnionWith(submask._inc);
                        combinedExcs.UnionWith(submask._exc);
                    }
                    combinedIncs.ExceptWith(_exc);//удаляю конфликтующие ограничения
                    combinedExcs.ExceptWith(_inc);//удаляю конфликтующие ограничения
                    combinedIncs.UnionWith(_inc);
                    combinedExcs.UnionWith(_exc);
                    _combineds.Clear();
                }
                else
                {
                    combinedIncs = _inc;
                    combinedExcs = _exc;
                }

                if (_excepteds.Count > 0)
                {
                    foreach (var item in _excepteds)
                    {
                        //if (combinedIncs.Overlaps(item.mask._exc) || combinedExcs.Overlaps(item.mask._inc))
                        //{
                        //    return _world.Get<WorldMaskComponent>().BrokenMask;
                        //}
                        combinedIncs.ExceptWith(item.mask._inc);
                        combinedExcs.ExceptWith(item.mask._exc);
                    }
                    _excepteds.Clear();
                }


                var inc = combinedIncs.ToArray();
                Array.Sort(inc);
                var exc = combinedExcs.ToArray();
                Array.Sort(exc);

                var key = new Key(inc, exc);
                EcsStaticMask result = CreateMask(key);

                _inc.Clear();
                _exc.Clear();

                _version++;
                return result;
            }
            #endregion

            #region Utils
            private readonly struct Combined
            {
                public readonly EcsStaticMask mask;
                public readonly int order;
                public Combined(EcsStaticMask mask, int order) { this.mask = mask; this.order = order; }
            }
            private readonly struct Excepted
            {
                public readonly EcsStaticMask mask;
                public readonly int order;
                public Excepted(EcsStaticMask mask, int order) { this.mask = mask; this.order = order; }
            }
            #endregion
        }
        #endregion

        #region Debug utils
        private static string CreateLogString(EcsTypeCode[] inc, EcsTypeCode[] exc)
        {
#if (DEBUG && !DISABLE_DEBUG)
            string converter(EcsTypeCode o) { return EcsDebugUtility.GetGenericTypeName(EcsTypeCodeManager.FindTypeOfCode(o), 1); }
            return $"Inc({string.Join(", ", inc.Select(converter))}) Exc({string.Join(", ", exc.Select(converter))})";
#else
            return $"Inc({string.Join(", ", inc)}) Exc({string.Join(", ", exc)})"; // Release optimization
#endif
        }

        internal class DebuggerProxy
        {
            private EcsStaticMask _source;

            public readonly int ID;
            public readonly EcsTypeCode[] included;
            public readonly EcsTypeCode[] excluded;
            public readonly Type[] includedTypes;
            public readonly Type[] excludedTypes;

            public bool IsEmpty { get { return _source.IsEmpty; } }
            public bool IsBroken { get { return _source.IsBroken; } }

            public DebuggerProxy(EcsStaticMask mask)
            {
                _source = mask;

                ID = mask._id;
                included = mask._inc;
                excluded = mask._exc;
                Type converter(EcsTypeCode o) { return EcsTypeCodeManager.FindTypeOfCode(o); }
                includedTypes = included.Select(converter).ToArray();
                excludedTypes = excluded.Select(converter).ToArray();
            }
            public override string ToString()
            {
                return CreateLogString(included, excluded);
            }
        }

#if DEBUG
        //TODO оптимизировать, так как списки сортированны, наверняка есть способ без хешсета пройтись и не локать треды
        private static HashSet<int> _dummyHashSet = new HashSet<int>();
        private static void CheckConstraints(int[] inc, int[] exc)
        {
            lock (_dummyHashSet)
            {
                if (CheckRepeats(inc)) { throw new EcsFrameworkException("The values in the Include constraints are repeated."); }
                if (CheckRepeats(exc)) { throw new EcsFrameworkException("The values in the Exclude constraints are repeated."); }
                _dummyHashSet.Clear();
                _dummyHashSet.UnionWith(inc);
                if (_dummyHashSet.Overlaps(exc)) { throw new EcsFrameworkException("Conflicting Include and Exclude constraints."); }
            }
        }
        private static bool CheckRepeats(int[] array)
        {
            _dummyHashSet.Clear();
            foreach (var item in array)
            {
                if (_dummyHashSet.Contains(item))
                {
                    return true;
                }
                _dummyHashSet.Add(item);
            }
            return false;
        }
#endif
        #endregion
    }
}