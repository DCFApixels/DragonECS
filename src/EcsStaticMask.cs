#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Core.Internal;
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
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public sealed class EcsStaticMask : IEquatable<EcsStaticMask>, IComponentMask
    {
        public static readonly EcsStaticMask Empty;
        public static readonly EcsStaticMask Broken;

        private static readonly Stack<BuilderInstance> _buildersPool = new Stack<BuilderInstance>();
        private static readonly ConcurrentDictionary<Key, EcsStaticMask> _ids = new ConcurrentDictionary<Key, EcsStaticMask>();
        private static readonly IdDispenser _idDIspenser = new IdDispenser(nullID: 0);
        private static readonly object _lock = new object();

        static EcsStaticMask()
        {
            EcsStaticMask createMask(int id, Key key)
            {
                EcsStaticMask result = new EcsStaticMask(id, key);
                _ids[key] = result;
                return result;
            }
            Empty = createMask(0, new Key(new EcsTypeCode[0], new EcsTypeCode[0], new EcsTypeCode[0]));
            Broken = createMask(_idDIspenser.UseFree(), new Key(new EcsTypeCode[1] { (EcsTypeCode)1 }, new EcsTypeCode[1] { (EcsTypeCode)1 }, new EcsTypeCode[0]));
        }

        public readonly int ID;
        /// <summary> Sorted </summary>
        private readonly EcsTypeCode[] _incs;
        /// <summary> Sorted </summary>
        private readonly EcsTypeCode[] _excs;
        /// <summary> Sorted </summary>
        private readonly EcsTypeCode[] _anys;

        private readonly EcsMaskFlags _flags;

        #region Properties
        /// <summary> Sorted set including constraints presented as global type codes. </summary>
        public ReadOnlySpan<EcsTypeCode> IncTypeCodes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _incs; }
        }
        /// <summary> Sorted set excluding constraints presented as global type codes. </summary>
        public ReadOnlySpan<EcsTypeCode> ExcTypeCodes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _excs; }
        }
        /// <summary> Sorted set excluding constraints presented as global type codes. </summary>
        public ReadOnlySpan<EcsTypeCode> AnyTypeCodes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _anys; }
        }
        public EcsMaskFlags Flags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _flags; }
        }
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _flags == EcsMaskFlags.Empty; }
        }
        public bool IsBroken
        {
            get { return (_flags & EcsMaskFlags.Broken) != 0; }
        }
        #endregion

        #region Constrcutors
        private EcsStaticMask(int id, Key key)
        {
            ID = id;
            _incs = key.Incs;
            _excs = key.Excs;
            _anys = key.Anys;
            if (_incs.Length > 0) { _flags |= EcsMaskFlags.Inc; }
            if (_excs.Length > 0) { _flags |= EcsMaskFlags.Exc; }
            if (_anys.Length > 0) { _flags |= EcsMaskFlags.Any; }
            if ((_incs.Length & _excs.Length) == 1 && _incs[0] == _excs[0])
            {
                _flags = EcsMaskFlags.Broken;
            }
        }
        public static Builder New() { return Builder.New(); }
        public static Builder Inc<T>() { return Builder.New().Inc<T>(); }
        public static Builder Exc<T>() { return Builder.New().Exc<T>(); }
        public static Builder Any<T>() { return Builder.New().Any<T>(); }
        public static Builder Inc(Type type) { return Builder.New().Inc(type); }
        public static Builder Exc(Type type) { return Builder.New().Exc(type); }
        public static Builder Any(Type type) { return Builder.New().Any(type); }
        public static Builder Inc(EcsTypeCode typeCode) { return Builder.New().Inc(typeCode); }
        public static Builder Exc(EcsTypeCode typeCode) { return Builder.New().Exc(typeCode); }
        public static Builder Any(EcsTypeCode typeCode) { return Builder.New().Any(typeCode); }
        private static EcsStaticMask CreateMask(Key key)
        {
            if (_ids.TryGetValue(key, out EcsStaticMask result) == false)
            {
                lock (_lock)
                {
                    if (_ids.TryGetValue(key, out result) == false)
                    {
#if DEBUG
                        CheckConstraints(key.Incs, key.Excs, key.Anys);
#endif
                        result = new EcsStaticMask(_idDIspenser.UseFree(), key);
                        _ids[key] = result;
                    }
                }
            }
            return result;
        }
        #endregion

        #region Checks
        public bool IsSubmaskOf(EcsStaticMask otherMask)
        {
            return IsSubmask(otherMask, this);
        }
        public bool IsSupermaskOf(EcsStaticMask otherMask)
        {
            return IsSubmask(this, otherMask);
        }
        public bool IsConflictWith(EcsStaticMask otherMask)
        {
            return OverlapsArray(_incs, otherMask._excs) || OverlapsArray(_excs, otherMask._incs) || OverlapsArray(_anys, otherMask._excs) || OverlapsArray(_anys, otherMask._incs);
        }
        private static bool IsSubmask(EcsStaticMask super, EcsStaticMask sub)
        {
            return IsSubarray(sub._incs, super._incs) && IsSuperarray(sub._excs, super._excs) && IsSubarray(sub._anys, super._anys);
        }

        private static bool OverlapsArray(EcsTypeCode[] l, EcsTypeCode[] r)
        {
            int li = 0;
            int ri = 0;
            while (li < l.Length && ri < r.Length)
            {
                if (l[li] == r[ri])
                {
                    return true;
                }
                else if (l[li] < r[ri])
                {
                    li++;
                }
                else
                {
                    ri++;
                }
            }
            return false;
        }
        private static bool IsSubarray(EcsTypeCode[] super, EcsTypeCode[] sub)
        {
            if (super.Length < sub.Length)
            {
                return false;
            }
            int superI = 0;
            int subI = 0;

            while (superI < super.Length && subI < sub.Length)
            {
                if (super[superI] == sub[subI])
                {
                    superI++;
                }
                subI++;
            }
            return subI == sub.Length;
        }
        private static bool IsSuperarray(EcsTypeCode[] super, EcsTypeCode[] sub)
        {
            if (super.Length < sub.Length)
            {
                return false;
            }
            int superI = 0;
            int subI = 0;

            while (superI < super.Length && subI < sub.Length)
            {
                if (super[superI] == sub[subI])
                {
                    subI++;
                }
                superI++;
            }
            return subI == sub.Length;
        }
        #endregion

        #region Methods
        public EcsMask ToMask(EcsWorld world) { return EcsMask.FromStatic(world, this); }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(EcsStaticMask other) { return ID == other.ID; }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return ID; }
        public override bool Equals(object obj) { return Equals((EcsStaticMask)obj); }
        public override string ToString() { return CreateLogString(_incs, _excs); }
        #endregion

        #region Builder
        private readonly struct Key : IEquatable<Key>
        {
            public readonly EcsTypeCode[] Incs;
            public readonly EcsTypeCode[] Excs;
            public readonly EcsTypeCode[] Anys;
            public readonly int Hash;

            #region Constructors
            public Key(EcsTypeCode[] incs, EcsTypeCode[] excs, EcsTypeCode[] anys)
            {
                this.Incs = incs;
                this.Excs = excs;
                this.Anys = anys;
                unchecked
                {
                    Hash = incs.Length + excs.Length;
                    for (int i = 0, iMax = incs.Length; i < iMax; i++)
                    {
                        Hash = Hash * EcsConsts.MAGIC_PRIME + (int)incs[i];
                    }
                    for (int i = 0, iMax = excs.Length; i < iMax; i++)
                    {
                        Hash = Hash * EcsConsts.MAGIC_PRIME - (int)excs[i];
                    }
                    for (int i = 0, iMax = anys.Length; i < iMax; i++)
                    {
                        Hash = Hash * EcsConsts.MAGIC_PRIME + (int)anys[i];
                    }
                }
            }
            #endregion

            #region Object
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(Key other)
            {
                if (Incs.Length != other.Incs.Length) { return false; }
                if (Excs.Length != other.Excs.Length) { return false; }
                if (Anys.Length != other.Anys.Length) { return false; }
                for (int i = 0; i < Incs.Length; i++)
                {
                    if (Incs[i] != other.Incs[i]) { return false; }
                }
                for (int i = 0; i < Excs.Length; i++)
                {
                    if (Excs[i] != other.Excs[i]) { return false; }
                }
                for (int i = 0; i < Anys.Length; i++)
                {
                    if (Anys[i] != other.Anys[i]) { return false; }
                }
                return true;
            }
            public override bool Equals(object obj) { return Equals((Key)obj); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() { return Hash; }
            #endregion
        }
        public readonly struct Builder
        {
            private readonly BuilderInstance _builder;
            private readonly int _version;

            #region Properties
            public bool IsNull
            {
                get { return _builder == null || _builder._version != _version; }
            }
            #endregion

            #region Constrcutors
            private Builder(BuilderInstance builder)
            {
                _builder = builder;
                _version = builder._version;
            }
            public static Builder New()
            {
                return new Builder(BuilderInstance.TakeFromPool());
            }
            #endregion

            #region Inc/Exc/Combine/Except
            public Builder Inc<T>() { return Inc(EcsTypeCodeManager.Get<T>()); }
            public Builder Exc<T>() { return Exc(EcsTypeCodeManager.Get<T>()); }
            public Builder Any<T>() { return Any(EcsTypeCodeManager.Get<T>()); }
            public Builder Inc(Type type) { return Inc(EcsTypeCodeManager.Get(type)); }
            public Builder Exc(Type type) { return Exc(EcsTypeCodeManager.Get(type)); }
            public Builder Any(Type type) { return Any(EcsTypeCodeManager.Get(type)); }
            public Builder Inc(EcsTypeCode typeCode)
            {
                if (_version != _builder._version) { Throw.CantReuseBuilder(); }
                _builder.Inc(typeCode);
                return this;
            }
            public Builder Exc(EcsTypeCode typeCode)
            {
                if (_version != _builder._version) { Throw.CantReuseBuilder(); }
                _builder.Exc(typeCode);
                return this;
            }
            public Builder Any(EcsTypeCode typeCode)
            {
                if (_version != _builder._version) { Throw.CantReuseBuilder(); }
                _builder.Any(typeCode);
                return this;
            }
            public Builder Combine(EcsStaticMask mask)
            {
                if (_version != _builder._version) { Throw.CantReuseBuilder(); }
                _builder.Combine(mask);
                return this;
            }
            public Builder Except(EcsStaticMask mask)
            {
                if (_version != _builder._version) { Throw.CantReuseBuilder(); }
                _builder.Except(mask);
                return this;
            }
            #endregion

            #region Build/Cancel
            public EcsStaticMask Build()
            {
                if (_version != _builder._version) { Throw.CantReuseBuilder(); }
                lock (_lock)
                {
                    var result = _builder.Build();
                    BuilderInstance.ReturnToPool(_builder);
                    return result;
                }
            }
            public void Cancel()
            {
                if (_version != _builder._version) { Throw.CantReuseBuilder(); }
                BuilderInstance.ReturnToPool(_builder);
            }
            #endregion
        }
        private class BuilderInstance
        {
            private readonly HashSet<EcsTypeCode> _incsSet = new HashSet<EcsTypeCode>();
            private readonly HashSet<EcsTypeCode> _excsSet = new HashSet<EcsTypeCode>();
            private readonly HashSet<EcsTypeCode> _anysSet = new HashSet<EcsTypeCode>();
            private readonly List<Combined> _combineds = new List<Combined>();
            private bool _sortedCombinedChecker = true;
            private readonly List<Excepted> _excepteds = new List<Excepted>();

            internal int _version;

            #region Constrcutors/Take/Return
            internal BuilderInstance() { }
            internal static BuilderInstance TakeFromPool()
            {
                lock (_lock)
                {
                    if (_buildersPool.TryPop(out BuilderInstance builderInstance) == false)
                    {
                        builderInstance = new BuilderInstance();
                    }
                    return builderInstance;
                }
            }
            internal static void ReturnToPool(BuilderInstance instance)
            {
                lock (_lock)
                {
                    instance.Clear();
                    _buildersPool.Push(instance);
                }
            }
            private void Clear()
            {
                _incsSet.Clear();
                _excsSet.Clear();
                _anysSet.Clear();
            }
            #endregion

            #region Inc/Exc/Combine/Except
            public void Inc(EcsTypeCode typeCode)
            {
#if DEBUG
                if (_incsSet.Contains(typeCode) || _excsSet.Contains(typeCode) || _anysSet.Contains(typeCode)) { Throw.ConstraintIsAlreadyContainedInMask(typeCode); }
#elif DRAGONECS_STABILITY_MODE
                if (_incsSet.Contains(typeCode) || _excsSet.Contains(typeCode) || _anysSet.Contains(typeCode)) { return; }
#endif
                _incsSet.Add(typeCode);
            }
            public void Exc(EcsTypeCode typeCode)
            {
#if DEBUG
                if (_incsSet.Contains(typeCode) || _excsSet.Contains(typeCode) || _anysSet.Contains(typeCode)) { Throw.ConstraintIsAlreadyContainedInMask(typeCode); }
#elif DRAGONECS_STABILITY_MODE
                if (_incsSet.Contains(typeCode) || _excsSet.Contains(typeCode) || _anysSet.Contains(typeCode)) { return; }
#endif
                _excsSet.Add(typeCode);
            }
            public void Any(EcsTypeCode typeCode)
            {
#if DEBUG
                if (_incsSet.Contains(typeCode) || _excsSet.Contains(typeCode) || _anysSet.Contains(typeCode)) { Throw.ConstraintIsAlreadyContainedInMask(typeCode); }
#elif DRAGONECS_STABILITY_MODE
                if (_incsSet.Contains(typeCode) || _excsSet.Contains(typeCode) || _anysSet.Contains(typeCode)) { return; }
#endif
                _anysSet.Add(typeCode);
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
                HashSet<EcsTypeCode> combinedIncs;
                HashSet<EcsTypeCode> combinedExcs;
                HashSet<EcsTypeCode> combinedAnys;

                if (_combineds.Count > 0)
                {
                    var combinerBuilder = TakeFromPool();
                    combinedIncs = combinerBuilder._incsSet;
                    combinedExcs = combinerBuilder._excsSet;
                    combinedAnys = combinerBuilder._anysSet;
                    combinedIncs.UnionWith(_incsSet);
                    combinedExcs.UnionWith(_excsSet);
                    combinedAnys.UnionWith(_anysSet);

                    if (_sortedCombinedChecker == false)
                    {
                        _combineds.Sort((a, b) => a.order - b.order);
                    }
                    foreach (var item in _combineds)
                    {
                        EcsStaticMask submask = item.mask;
                        _incsSet.ExceptWith(submask._excs);//удаляю конфликтующие ограничения
                        _excsSet.ExceptWith(submask._incs);//удаляю конфликтующие ограничения
                        _anysSet.ExceptWith(submask._excs);//удаляю конфликтующие ограничения
                        _anysSet.ExceptWith(submask._incs);//удаляю конфликтующие ограничения
                        _incsSet.UnionWith(submask._incs);
                        _excsSet.UnionWith(submask._excs);
                        _anysSet.UnionWith(submask._anys);
                    }
                    _incsSet.ExceptWith(combinedExcs);//удаляю конфликтующие ограничения
                    _excsSet.ExceptWith(combinedIncs);//удаляю конфликтующие ограничения
                    _anysSet.ExceptWith(combinedExcs);//удаляю конфликтующие ограничения
                    _anysSet.ExceptWith(combinedIncs);//удаляю конфликтующие ограничения
                    _incsSet.UnionWith(combinedIncs);
                    _excsSet.UnionWith(combinedExcs);
                    _anysSet.UnionWith(combinedAnys);
                    _combineds.Clear();
                    ReturnToPool(combinerBuilder);
                }

                combinedIncs = _incsSet;
                combinedExcs = _excsSet;
                combinedAnys = _anysSet;

                if (_excepteds.Count > 0)
                {
                    foreach (var item in _excepteds)
                    {
                        //if (combinedIncs.Overlaps(item.mask._exc) || combinedExcs.Overlaps(item.mask._inc))
                        //{
                        //    return _world.Get<WorldMaskComponent>().BrokenMask;
                        //}
                        combinedIncs.ExceptWith(item.mask._incs);
                        combinedExcs.ExceptWith(item.mask._excs);
                        combinedAnys.ExceptWith(item.mask._anys);
                    }
                    _excepteds.Clear();
                }


                var inc = combinedIncs.ToArray();
                Array.Sort(inc);
                var exc = combinedExcs.ToArray();
                Array.Sort(exc);
                var any = combinedAnys.ToArray();
                Array.Sort(any);

                var key = new Key(inc, exc, any);
                EcsStaticMask result = CreateMask(key);

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
#if DEBUG
            string converter(EcsTypeCode o) { return EcsTypeCodeManager.FindTypeOfCode(o).ToString(); }
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

                ID = mask.ID;
                included = mask._incs;
                excluded = mask._excs;
                Type converter(EcsTypeCode o) { return EcsTypeCodeManager.FindTypeOfCode(o).Type; }
                includedTypes = included.Select(converter).ToArray();
                excludedTypes = excluded.Select(converter).ToArray();
            }
            public override string ToString()
            {
                return CreateLogString(included, excluded);
            }
        }

#if DEBUG
        private static void CheckConstraints(EcsTypeCode[] incs, EcsTypeCode[] excs, EcsTypeCode[] anys)
        {
            if (CheckRepeats(incs)) { throw new ArgumentException("The values in the Include constraints are repeated."); }
            if (CheckRepeats(excs)) { throw new ArgumentException("The values in the Exclude constraints are repeated."); }
            if (CheckRepeats(anys)) { throw new ArgumentException("The values in the Any constraints are repeated."); }
            if (OverlapsArray(incs, excs)) { throw new ArgumentException("Conflicting Include and Exclude constraints."); }
            if (OverlapsArray(incs, anys)) { throw new ArgumentException("Conflicting Include and Any constraints."); }
            if (OverlapsArray(anys, excs)) { throw new ArgumentException("Conflicting Any and Exclude constraints."); }
        }
        private static bool CheckRepeats(EcsTypeCode[] array)
        {
            if (array.Length <= 1)
            {
                return false;
            }
            EcsTypeCode lastValue = array[0];
            for (int i = 1; i < array.Length; i++)
            {
                EcsTypeCode value = array[i];
                if (value == lastValue)
                {
                    return true;
                }
                lastValue = value;
            }
            return false;
        }
#endif
        #endregion
    }
}