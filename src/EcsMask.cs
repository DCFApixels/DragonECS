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
    using static EcsMaskIteratorUtility;
    public interface IEcsComponentMask
    {
        EcsMask ToMask(EcsWorld world);
    }
#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    public sealed class EcsStaticMask : IEquatable<EcsStaticMask>, IEcsComponentMask
    {
        private static ConcurrentDictionary<Key, EcsStaticMask> _ids = new ConcurrentDictionary<Key, EcsStaticMask>();
        private static IdDispenser _idDIspenser = new IdDispenser(nullID: 0);
        private static object _lock = new object();

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
        #endregion

        #region Constrcutors
        public static Builder New() { return new Builder(); }
        public static Builder Inc<T>() { return new Builder().Inc<T>(); }
        public static Builder Exc<T>() { return new Builder().Exc<T>(); }
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
        public override string ToString() { return $"s_mask({_id})"; }
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
        public class Builder
        {
            private readonly HashSet<EcsTypeCode> _inc = new HashSet<EcsTypeCode>();
            private readonly HashSet<EcsTypeCode> _exc = new HashSet<EcsTypeCode>();

            #region Constrcutors
            internal Builder() { }
            #endregion

            #region Include/Exclude/Combine
            public Builder Inc<T>() { return Inc(EcsTypeCodeManager.Get<T>()); }
            public Builder Exc<T>() { return Exc(EcsTypeCodeManager.Get<T>()); }
            public Builder Inc(Type type) { return Inc(EcsTypeCodeManager.Get(type)); }
            public Builder Exc(Type type) { return Exc(EcsTypeCodeManager.Get(type)); }
            public Builder Inc(EcsTypeCode typeCode)
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(typeCode) || _exc.Contains(typeCode)) { Throw.ConstraintIsAlreadyContainedInMask(); }
#endif
                _inc.Add(typeCode);
                return this;
            }
            public Builder Exc(EcsTypeCode typeCode)
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(typeCode) || _exc.Contains(typeCode)) { Throw.ConstraintIsAlreadyContainedInMask(); }
#endif
                _exc.Add(typeCode);
                return this;
            }
            #endregion

            #region Build
            public EcsStaticMask Build()
            {
                HashSet<EcsTypeCode> combinedIncs = _inc;
                HashSet<EcsTypeCode> combinedExcs = _exc;

                var inc = combinedIncs.ToArray();
                Array.Sort(inc);
                var exc = combinedExcs.ToArray();
                Array.Sort(exc);

                var key = new Key(inc, exc);
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
            #endregion
        }
        #endregion
    }

#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public sealed class EcsMask : IEquatable<EcsMask>, IEcsComponentMask
    {
        internal readonly int _id;
        internal readonly short _worldID;
        internal readonly EcsMaskChunck[] _incChunckMasks;
        internal readonly EcsMaskChunck[] _excChunckMasks;
        /// <summary> Sorted </summary>
        internal readonly int[] _inc;
        /// <summary> Sorted </summary>
        internal readonly int[] _exc;

        private EcsMaskIterator _iterator;

        #region Properties
        public int ID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _id; }
        }
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _worldID; }
        }
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return EcsWorld.GetWorld(_worldID); }
        }
        /// <summary> Sorted set including constraints. </summary>
        public ReadOnlySpan<int> Inc
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _inc; }
        }
        /// <summary> Sorted set excluding constraints. </summary>
        public ReadOnlySpan<int> Exc
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

        #region Constructors
        public static Builder New(EcsWorld world) { return new Builder(world); }
        internal static EcsMask Create(int id, short worldID, int[] inc, int[] exc)
        {
#if DEBUG
            CheckConstraints(inc, exc);
#endif
            return new EcsMask(id, worldID, inc, exc);
        }
        internal static EcsMask CreateEmpty(int id, short worldID)
        {
            return new EcsMask(id, worldID, new int[0], new int[0]);
        }
        internal static EcsMask CreateBroken(int id, short worldID)
        {
            return new EcsMask(id, worldID, new int[1] { 1 }, new int[1] { 1 });
        }
        private EcsMask(int id, short worldID, int[] inc, int[] exc)
        {
            _id = id;
            _inc = inc;
            _exc = exc;
            _worldID = worldID;

            _incChunckMasks = MakeMaskChuncsArray(inc);
            _excChunckMasks = MakeMaskChuncsArray(exc);
        }

        private unsafe EcsMaskChunck[] MakeMaskChuncsArray(int[] sortedArray)
        {
            EcsMaskChunck* buffer = stackalloc EcsMaskChunck[sortedArray.Length];

            int resultLength = 0;
            for (int i = 0; i < sortedArray.Length;)
            {
                int chankIndexX = sortedArray[i] >> EcsMaskChunck.DIV_SHIFT;
                int maskX = 0;
                do
                {
                    EcsMaskChunck bitJ = EcsMaskChunck.FromID(sortedArray[i]);
                    if (bitJ.chunkIndex != chankIndexX)
                    {
                        break;
                    }
                    maskX |= bitJ.mask;
                    i++;
                } while (i < sortedArray.Length);
                buffer[resultLength++] = new EcsMaskChunck(chankIndexX, maskX);
            }

            EcsMaskChunck[] result = new EcsMaskChunck[resultLength];
            for (int i = 0; i < resultLength; i++)
            {
                result[i] = buffer[i];
            }
            return result;
        }
        #endregion

        #region Checks
        public bool IsSubmaskOf(EcsMask otherMask)
        {
            return IsSubmask(otherMask, this);
        }
        public bool IsSupermaskOf(EcsMask otherMask)
        {
            return IsSubmask(this, otherMask);
        }
        public bool IsConflictWith(EcsMask otherMask)
        {
            return OverlapsArray(_inc, otherMask._exc) || OverlapsArray(_exc, otherMask._inc);
        }

        private static bool OverlapsArray(int[] l, int[] r)
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
        private static bool IsSubmask(EcsMask super, EcsMask sub)
        {
            return IsSubarray(sub._inc, super._inc) && IsSuperarray(sub._exc, super._exc);
        }
        private static bool IsSubarray(int[] super, int[] sub)
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

        private static bool IsSuperarray(int[] super, int[] sub)
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

        #region Object
        public override string ToString()
        {
            return CreateLogString(_worldID, _inc, _exc);
        }
        public bool Equals(EcsMask mask)
        {
            return _id == mask._id && _worldID == mask._worldID;
        }
        public override bool Equals(object obj)
        {
            return obj is EcsMask mask && _id == mask._id && Equals(mask);
        }
        public override int GetHashCode()
        {
            return unchecked(_id ^ (_worldID * EcsConsts.MAGIC_PRIME));
        }
        #endregion

        #region Other
        EcsMask IEcsComponentMask.ToMask(EcsWorld world) { return this; }
        public EcsMaskIterator GetIterator()
        {
            if (_iterator == null)
            {
                _iterator = new EcsMaskIterator(EcsWorld.GetWorld(_worldID), this);
            }
            return _iterator;
        }
        #endregion

        #region Debug utils
#if DEBUG
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
        private static string CreateLogString(short worldID, int[] inc, int[] exc)
        {
#if (DEBUG && !DISABLE_DEBUG)
            string converter(int o) { return EcsDebugUtility.GetGenericTypeName(EcsWorld.GetWorld(worldID).AllPools[o].ComponentType, 1); }
            return $"Inc({string.Join(", ", inc.Select(converter))}) Exc({string.Join(", ", exc.Select(converter))})";
#else
            return $"Inc({string.Join(", ", inc)}) Exc({string.Join(", ", exc)})"; // Release optimization
#endif
        }

        internal class DebuggerProxy
        {
            private EcsMask _source;

            public readonly int ID;
            public readonly EcsWorld world;
            private readonly short _worldID;
            public readonly EcsMaskChunck[] includedChunkMasks;
            public readonly EcsMaskChunck[] excludedChunkMasks;
            public readonly int[] included;
            public readonly int[] excluded;
            public readonly Type[] includedTypes;
            public readonly Type[] excludedTypes;

            public bool IsEmpty { get { return _source.IsEmpty; } }
            public bool IsBroken { get { return _source.IsBroken; } }

            public DebuggerProxy(EcsMask mask)
            {
                _source = mask;

                ID = mask._id;
                world = EcsWorld.GetWorld(mask._worldID);
                _worldID = mask._worldID;
                includedChunkMasks = mask._incChunckMasks;
                excludedChunkMasks = mask._excChunckMasks;
                included = mask._inc;
                excluded = mask._exc;
                Type converter(int o) { return world.GetComponentType(o); }
                includedTypes = included.Select(converter).ToArray();
                excludedTypes = excluded.Select(converter).ToArray();
            }
            public override string ToString()
            {
                return CreateLogString(_worldID, included, excluded);
            }
        }
        #endregion

        #region Operators
        public static EcsMask operator -(EcsMask a, EcsMask b)
        {
            return a.World.Get<WorldMaskComponent>().ExceptMask(a, b);
        }
        public static EcsMask operator -(EcsMask a, IEcsComponentMask b)
        {
            return a.World.Get<WorldMaskComponent>().ExceptMask(a, b.ToMask(a.World));
        }
        public static EcsMask operator -(IEcsComponentMask b, EcsMask a)
        {
            return a.World.Get<WorldMaskComponent>().ExceptMask(b.ToMask(a.World), a);
        }
        public static EcsMask operator +(EcsMask a, EcsMask b)
        {
            return a.World.Get<WorldMaskComponent>().CombineMask(a, b);
        }
        public static EcsMask operator +(EcsMask a, IEcsComponentMask b)
        {
            return a.World.Get<WorldMaskComponent>().CombineMask(a, b.ToMask(a.World));
        }
        public static EcsMask operator +(IEcsComponentMask b, EcsMask a)
        {
            return a.World.Get<WorldMaskComponent>().CombineMask(b.ToMask(a.World), a);
        }
        public static implicit operator EcsMask((IEcsComponentMask mask, EcsWorld world) a)
        {
            return a.mask.ToMask(a.world);
        }
        public static implicit operator EcsMask((EcsWorld world, IEcsComponentMask mask) a)
        {
            return a.mask.ToMask(a.world);
        }
        #endregion

        #region OpMaskKey
        private readonly struct OpMaskKey : IEquatable<OpMaskKey>
        {
            public readonly int leftMaskID;
            public readonly int rightMaskID;
            public readonly int operation;

            public const int COMBINE_OP = 7;
            public const int EXCEPT_OP = 32;
            public OpMaskKey(int leftMaskID, int rightMaskID, int operation)
            {
                this.leftMaskID = leftMaskID;
                this.rightMaskID = rightMaskID;
                this.operation = operation;
            }
            public bool Equals(OpMaskKey other)
            {
                return leftMaskID == other.leftMaskID &&
                    rightMaskID == other.rightMaskID &&
                    operation == other.operation;
            }
            public override int GetHashCode()
            {
                return leftMaskID ^ (rightMaskID * operation);
            }
        }

        #endregion

        #region StaticMask
        public static EcsMask FromStatic(EcsWorld world, EcsStaticMask abstractMask)
        {
            return world.Get<WorldMaskComponent>().ConvertFromAbstract(abstractMask);
        }
        #endregion

        #region Builder
        private readonly struct WorldMaskComponent : IEcsWorldComponent<WorldMaskComponent>
        {
            private readonly EcsWorld _world;
            private readonly Dictionary<Key, EcsMask> _masks;
            private readonly Dictionary<OpMaskKey, EcsMask> _opMasks;
            private readonly SparseArray<EcsMask> _abstractMasks;

            public readonly EcsMask EmptyMask;
            public readonly EcsMask BrokenMask;

            #region Constructor/Destructor
            public WorldMaskComponent(EcsWorld world, Dictionary<Key, EcsMask> masks, EcsMask emptyMask, EcsMask brokenMask)
            {
                _world = world;
                _masks = masks;
                _opMasks = new Dictionary<OpMaskKey, EcsMask>(256);
                _abstractMasks = new SparseArray<EcsMask>(256);
                EmptyMask = emptyMask;
                BrokenMask = brokenMask;
            }
            public void Init(ref WorldMaskComponent component, EcsWorld world)
            {
                var masks = new Dictionary<Key, EcsMask>(256);
                EcsMask emptyMask = CreateEmpty(0, world.id);
                EcsMask brokenMask = CreateBroken(1, world.id);
                masks.Add(new Key(emptyMask._inc, emptyMask._exc), emptyMask);
                masks.Add(new Key(brokenMask._inc, brokenMask._exc), brokenMask);
                component = new WorldMaskComponent(world, masks, emptyMask, brokenMask);
            }
            public void OnDestroy(ref WorldMaskComponent component, EcsWorld world)
            {
                component._masks.Clear();
                component._opMasks.Clear();
                component._abstractMasks.Clear();
                component = default;
            }
            #endregion

            #region GetMask
            internal EcsMask CombineMask(EcsMask a, EcsMask b)
            {
                int operation = OpMaskKey.COMBINE_OP;
                if (_opMasks.TryGetValue(new OpMaskKey(a._id, b._id, operation), out EcsMask result) == false)
                {
                    if (a.IsConflictWith(b))
                    {
                        return a.World.Get<WorldMaskComponent>().BrokenMask;
                    }
                    result = New(a.World).Combine(a).Combine(b).Build();
                    _opMasks.Add(new OpMaskKey(a._id, b._id, operation), result);
                }
                return result;
            }
            internal EcsMask ExceptMask(EcsMask a, EcsMask b)
            {
                int operation = OpMaskKey.EXCEPT_OP;
                if (_opMasks.TryGetValue(new OpMaskKey(a._id, b._id, operation), out EcsMask result) == false)
                {
                    if (a.IsConflictWith(b))
                    {
                        return a.World.Get<WorldMaskComponent>().BrokenMask;
                    }
                    result = New(a.World).Combine(a).Except(b).Build();
                    _opMasks.Add(new OpMaskKey(a._id, b._id, operation), result);
                }
                return result;
            }
            internal EcsMask ConvertFromAbstract(EcsStaticMask abstractMask)
            {
                if (_abstractMasks.TryGetValue(abstractMask.ID, out EcsMask result) == false)
                {
                    var b = New(_world);
                    foreach (var typeCode in abstractMask.IncTypeCodes)
                    {
                        b.Inc(_world.DeclareOrGetComponentTypeID(typeCode));
                    }
                    foreach (var typeCode in abstractMask.ExcTypeCodes)
                    {
                        b.Exc(_world.DeclareOrGetComponentTypeID(typeCode));
                    }
                    result = b.Build();
                    _abstractMasks.Add(abstractMask.ID, result);
                }
                return result;
            }
            internal EcsMask GetMask(Key maskKey)
            {
                if (_masks.TryGetValue(maskKey, out EcsMask result) == false)
                {
                    result = Create(_masks.Count, _world.id, maskKey.inc, maskKey.exc);
                    _masks.Add(maskKey, result);
                }
                return result;
            }
            #endregion
        }
        private readonly struct Key : IEquatable<Key>
        {
            public readonly int[] inc;
            public readonly int[] exc;
            public readonly int hash;

            #region Constructors
            public Key(int[] inc, int[] exc)
            {
                this.inc = inc;
                this.exc = exc;
                unchecked
                {
                    hash = inc.Length + exc.Length;
                    for (int i = 0, iMax = inc.Length; i < iMax; i++)
                    {
                        hash = hash * EcsConsts.MAGIC_PRIME + inc[i];
                    }
                    for (int i = 0, iMax = exc.Length; i < iMax; i++)
                    {
                        hash = hash * EcsConsts.MAGIC_PRIME - exc[i];
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

        public class Builder
        {
            private readonly EcsWorld _world;
            private readonly HashSet<int> _inc = new HashSet<int>();
            private readonly HashSet<int> _exc = new HashSet<int>();
            private readonly List<Combined> _combineds = new List<Combined>();
            private readonly List<Excepted> _excepteds = new List<Excepted>();

            #region Constrcutors
            internal Builder(EcsWorld world)
            {
                _world = world;
            }
            #endregion

            #region Inc/Exc/Combine
            [Obsolete("Use Inc<T>()")] public Builder Include<T>() { return Inc<T>(); }
            [Obsolete("Use Exc<T>()")] public Builder Exclude<T>() { return Exc<T>(); }
            [Obsolete("Use Inc(type)")] public Builder Include(Type type) { return Inc(type); }
            [Obsolete("Use Exc(type)")] public Builder Exclude(Type type) { return Exc(type); }

            public Builder Inc<T>() { return Inc(_world.GetComponentTypeID<T>()); }
            public Builder Exc<T>() { return Exc(_world.GetComponentTypeID<T>()); }
            public Builder Inc(Type type) { return Inc(_world.GetComponentTypeID(type)); }
            public Builder Exc(Type type) { return Exc(_world.GetComponentTypeID(type)); }

            public Builder Inc(int componentTypeID)
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(componentTypeID) || _exc.Contains(componentTypeID)) { Throw.ConstraintIsAlreadyContainedInMask(_world.GetComponentType(componentTypeID)); }
#endif
                _inc.Add(componentTypeID);
                return this;
            }
            public Builder Exc(int componentTypeID)
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(componentTypeID) || _exc.Contains(componentTypeID)) { Throw.ConstraintIsAlreadyContainedInMask(_world.GetComponentType(componentTypeID)); }
#endif
                _exc.Add(componentTypeID);
                return this;
            }

            public Builder Combine(EcsMask mask, int order = 0)
            {
                _combineds.Add(new Combined(mask, order));
                return this;
            }

            public Builder Except(EcsMask mask, int order = 0)
            {
                _excepteds.Add(new Excepted(mask, order));
                return this;
            }
            #endregion

            #region Build
            public EcsMask Build()
            {
                HashSet<int> combinedInc;
                HashSet<int> combinedExc;
                if (_combineds.Count > 0)
                {
                    combinedInc = new HashSet<int>();
                    combinedExc = new HashSet<int>();
                    _combineds.Sort((a, b) => a.order - b.order);
                    foreach (var item in _combineds)
                    {
                        EcsMask submask = item.mask;
                        combinedInc.ExceptWith(submask._exc);//удаляю конфликтующие ограничения
                        combinedExc.ExceptWith(submask._inc);//удаляю конфликтующие ограничения
                        combinedInc.UnionWith(submask._inc);
                        combinedExc.UnionWith(submask._exc);
                    }
                    combinedInc.ExceptWith(_exc);//удаляю конфликтующие ограничения
                    combinedExc.ExceptWith(_inc);//удаляю конфликтующие ограничения
                    combinedInc.UnionWith(_inc);
                    combinedExc.UnionWith(_exc);
                }
                else
                {
                    combinedInc = _inc;
                    combinedExc = _exc;
                }
                if (_excepteds.Count > 0)
                {
                    foreach (var item in _excepteds)
                    {
                        if (combinedInc.Overlaps(item.mask._exc) || combinedExc.Overlaps(item.mask._inc))
                        {
                            _combineds.Clear();
                            _excepteds.Clear();
                            return _world.Get<WorldMaskComponent>().BrokenMask;
                        }
                        combinedInc.ExceptWith(item.mask._inc);
                        combinedExc.ExceptWith(item.mask._exc);
                    }
                }

                var inc = combinedInc.ToArray();
                Array.Sort(inc);
                var exc = combinedExc.ToArray();
                Array.Sort(exc);

                _combineds.Clear();
                _excepteds.Clear();

                return _world.Get<WorldMaskComponent>().GetMask(new Key(inc, exc));
            }
            #endregion
        }

        private readonly struct Combined
        {
            public readonly EcsMask mask;
            public readonly int order;
            public Combined(EcsMask mask, int order)
            {
                this.mask = mask;
                this.order = order;
            }
        }
        private readonly struct Excepted
        {
            public readonly EcsMask mask;
            public readonly int order;
            public Excepted(EcsMask mask, int order)
            {
                this.mask = mask;
                this.order = order;
            }
        }
        #endregion
    }

    #region EcsMaskChunck
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public readonly struct EcsMaskChunck
    {
        internal const int BITS = 32;
        internal const int DIV_SHIFT = 5;
        internal const int MOD_MASK = BITS - 1;

        public readonly int chunkIndex;
        public readonly int mask;
        public EcsMaskChunck(int chankIndex, int mask)
        {
            this.chunkIndex = chankIndex;
            this.mask = mask;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsMaskChunck FromID(int id)
        {
            return new EcsMaskChunck(id >> DIV_SHIFT, 1 << (id & MOD_MASK));
        }
        public override string ToString()
        {
            return $"mask({chunkIndex}, {mask}, {BitsUtility.CountBits(mask)})";
        }
        internal class DebuggerProxy
        {
            public int chunk;
            public uint mask;
            public int[] values = Array.Empty<int>();
            public string bits;
            public DebuggerProxy(EcsMaskChunck maskbits)
            {
                chunk = maskbits.chunkIndex;
                mask = (uint)maskbits.mask;
                BitsUtility.GetBitNumbersNoAlloc(mask, ref values);
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] += (chunk) << 5;
                }
                bits = BitsUtility.ToBitsString(mask, '_', 8);
            }
        }
    }
    #endregion

    #region EcsMaskIterator
#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    public class EcsMaskIterator
    {
        public readonly EcsWorld World;
        public readonly EcsMask Mask;

        private readonly UnsafeArray<int> _sortIncBuffer;
        private readonly UnsafeArray<int> _sortExcBuffer;
        private readonly UnsafeArray<EcsMaskChunck> _sortIncChunckBuffer;
        private readonly UnsafeArray<EcsMaskChunck> _sortExcChunckBuffer;

        private readonly bool _isOnlyInc;

        #region Constructors/Finalizator
        public unsafe EcsMaskIterator(EcsWorld source, EcsMask mask)
        {
            World = source;
            Mask = mask;
            _sortIncBuffer = UnsafeArray<int>.FromArray(mask._inc);
            _sortExcBuffer = UnsafeArray<int>.FromArray(mask._exc);
            _sortIncChunckBuffer = UnsafeArray<EcsMaskChunck>.FromArray(mask._incChunckMasks);
            _sortExcChunckBuffer = UnsafeArray<EcsMaskChunck>.FromArray(mask._excChunckMasks);
            _isOnlyInc = _sortExcBuffer.Length <= 0;
        }
        unsafe ~EcsMaskIterator()
        {
            _sortIncBuffer.ReadonlyDispose();
            _sortExcBuffer.ReadonlyDispose();
            _sortIncChunckBuffer.ReadonlyDispose();
            _sortExcChunckBuffer.ReadonlyDispose();
        }
        #endregion


        #region IterateTo
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void IterateTo(EcsSpan source, EcsGroup group)
        {
            if (_isOnlyInc)
            {
                IterateOnlyInc(source).CopyTo(group);
            }
            else
            {
                Iterate(source).CopyTo(group);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IterateTo(EcsSpan source, ref int[] array)
        {
            if (_isOnlyInc)
            {
                return IterateOnlyInc(source).CopyTo(ref array);
            }
            else
            {
                return Iterate(source).CopyTo(ref array);
            }
        }
        #endregion

        #region Iterate/Enumerable
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerable Iterate(EcsSpan span) { return new Enumerable(this, span); }
#if ENABLE_IL2CPP
        [Il2CppSetOption (Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
        public readonly ref struct Enumerable
        {
            private readonly EcsMaskIterator _iterator;
            private readonly EcsSpan _span;
            public Enumerable(EcsMaskIterator iterator, EcsSpan span)
            {
                _iterator = iterator;
                _span = span;
            }

            #region CopyTo
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CopyTo(EcsGroup group)
            {
                group.Clear();
                var enumerator = GetEnumerator();
                while (enumerator.MoveNext())
                {
                    group.AddUnchecked(enumerator.Current);
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int CopyTo(ref int[] array)
            {
                int count = 0;
                var enumerator = GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (array.Length <= count)
                    {
                        Array.Resize(ref array, array.Length << 1);
                    }
                    array[count++] = enumerator.Current;
                }
                return count;
            }
            #endregion

            #region Other
            public List<int> ToList()
            {
                List<int> ints = new List<int>();
                foreach (var e in this) { ints.Add(e); }
                return ints;
            }
            public override string ToString() { return CollectionUtility.EntitiesToString(ToList(), "it"); }
            #endregion

            #region Enumerator
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator() { return new Enumerator(_span, _iterator); }
#if ENABLE_IL2CPP
            [Il2CppSetOption (Option.NullChecks, false)]
            [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
            public unsafe ref struct Enumerator
            {
                private ReadOnlySpan<int>.Enumerator _span;

                private readonly UnsafeArray<EcsMaskChunck> _sortIncChunckBuffer;
                private readonly UnsafeArray<EcsMaskChunck> _sortExcChunckBuffer;

                private readonly int[] _entityComponentMasks;
                private readonly int _entityComponentMaskLengthBitShift;

                public unsafe Enumerator(EcsSpan span, EcsMaskIterator iterator)
                {
                    _sortIncChunckBuffer = iterator._sortIncChunckBuffer;
                    _sortExcChunckBuffer = iterator._sortExcChunckBuffer;

                    _entityComponentMasks = iterator.World._entityComponentMasks;
                    _entityComponentMaskLengthBitShift = iterator.World._entityComponentMaskLengthBitShift;

                    if (iterator.Mask.IsBroken)
                    {
                        _span = span.Slice(0, 0).GetEnumerator();
                        return;
                    }

                    #region Sort
                    UnsafeArray<int> _sortIncBuffer = iterator._sortIncBuffer;
                    UnsafeArray<int> _sortExcBuffer = iterator._sortExcBuffer;
                    EcsWorld.PoolSlot[] counts = iterator.World._poolSlots;
                    int max = _sortIncBuffer.Length > _sortExcBuffer.Length ? _sortIncBuffer.Length : _sortExcBuffer.Length;

                    EcsMaskChunck* preSortingBuffer;
                    if (max > STACK_BUFFER_THRESHOLD)
                    {
                        preSortingBuffer = TempBuffer<EcsMaskChunck>.Get(max);
                    }
                    else
                    {
                        EcsMaskChunck* ptr = stackalloc EcsMaskChunck[max];
                        preSortingBuffer = ptr;
                    }

                    if (_sortIncChunckBuffer.Length > 1)
                    {
                        var comparer = new IncCountComparer(counts);
                        UnsafeArraySortHalperX<int>.InsertionSort(_sortIncBuffer.ptr, _sortIncBuffer.Length, ref comparer);
                        ConvertToChuncks(preSortingBuffer, _sortIncBuffer, _sortIncChunckBuffer);
                    }
                    if (_sortIncChunckBuffer.Length > 0 && counts[_sortIncBuffer.ptr[0]].count <= 0)
                    {
                        _span = span.Slice(0, 0).GetEnumerator();
                        return;
                    }

                    if (_sortExcChunckBuffer.Length > 1)
                    {
                        ExcCountComparer comparer = new ExcCountComparer(counts);
                        UnsafeArraySortHalperX<int>.InsertionSort(_sortExcBuffer.ptr, _sortExcBuffer.Length, ref comparer);
                        ConvertToChuncks(preSortingBuffer, _sortExcBuffer, _sortExcChunckBuffer);
                    }
                    #endregion

                    _span = span.GetEnumerator();
                }
                public int Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get { return _span.Current; }
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    while (_span.MoveNext())
                    {
                        int chunck = _span.Current << _entityComponentMaskLengthBitShift;
                        for (int i = 0; i < _sortIncChunckBuffer.Length; i++)
                        {
                            var bit = _sortIncChunckBuffer.ptr[i];
                            if ((_entityComponentMasks[chunck + bit.chunkIndex] & bit.mask) != bit.mask)
                            {
                                goto skip;
                            }
                        }
                        for (int i = 0; i < _sortExcChunckBuffer.Length; i++)
                        {
                            var bit = _sortExcChunckBuffer.ptr[i];
                            if ((_entityComponentMasks[chunck + bit.chunkIndex] & bit.mask) != 0)
                            {
                                goto skip;
                            }
                        }
                        return true;
                        skip: continue;
                    }
                    return false;
                }
            }
            #endregion
        }
        #endregion

        #region Iterate/Enumerable OnlyInc
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OnlyIncEnumerable IterateOnlyInc(EcsSpan span) { return new OnlyIncEnumerable(this, span); }
#if ENABLE_IL2CPP
        [Il2CppSetOption (Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
        public readonly ref struct OnlyIncEnumerable
        {
            private readonly EcsMaskIterator _iterator;
            private readonly EcsSpan _span;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public OnlyIncEnumerable(EcsMaskIterator iterator, EcsSpan span)
            {
                _iterator = iterator;
                _span = span;
            }

            #region CopyTo
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CopyTo(EcsGroup group)
            {
                group.Clear();
                var enumerator = GetEnumerator();
                while (enumerator.MoveNext())
                {
                    group.AddUnchecked(enumerator.Current);
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int CopyTo(ref int[] array)
            {
                int count = 0;
                var enumerator = GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (array.Length <= count)
                    {
                        Array.Resize(ref array, array.Length << 1);
                    }
                    array[count++] = enumerator.Current;
                }
                return count;
            }
            #endregion

            #region Other
            public List<int> ToList()
            {
                List<int> ints = new List<int>();
                foreach (var e in this) { ints.Add(e); }
                return ints;
            }
            public override string ToString() { return CollectionUtility.EntitiesToString(ToList(), "inc_it"); }
            #endregion

            #region Enumerator
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator GetEnumerator() { return new Enumerator(_span, _iterator); }
#if ENABLE_IL2CPP
            [Il2CppSetOption (Option.NullChecks, false)]
            [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
            public unsafe ref struct Enumerator
            {
                private ReadOnlySpan<int>.Enumerator _span;

                private readonly UnsafeArray<EcsMaskChunck> _sortIncChunckBuffer;

                private readonly int[] _entityComponentMasks;
                private readonly int _entityComponentMaskLengthBitShift;

                public unsafe Enumerator(EcsSpan span, EcsMaskIterator iterator)
                {
                    _sortIncChunckBuffer = iterator._sortIncChunckBuffer;

                    _entityComponentMasks = iterator.World._entityComponentMasks;
                    _entityComponentMaskLengthBitShift = iterator.World._entityComponentMaskLengthBitShift;

                    if (iterator.Mask.IsBroken)
                    {
                        _span = span.Slice(0, 0).GetEnumerator();
                        return;
                    }

                    #region Sort
                    UnsafeArray<int> _sortIncBuffer = iterator._sortIncBuffer;
                    EcsWorld.PoolSlot[] counts = iterator.World._poolSlots;
                    int max = _sortIncBuffer.Length;

                    EcsMaskChunck* preSortingBuffer;
                    if (max > STACK_BUFFER_THRESHOLD)
                    {
                        preSortingBuffer = TempBuffer<EcsMaskChunck>.Get(max);
                    }
                    else
                    {
                        EcsMaskChunck* ptr = stackalloc EcsMaskChunck[max];
                        preSortingBuffer = ptr;
                    }

                    if (_sortIncChunckBuffer.Length > 1)
                    {
                        var comparer = new IncCountComparer(counts);
                        UnsafeArraySortHalperX<int>.InsertionSort(_sortIncBuffer.ptr, _sortIncBuffer.Length, ref comparer);
                        ConvertToChuncks(preSortingBuffer, _sortIncBuffer, _sortIncChunckBuffer);
                    }
                    if (_sortIncChunckBuffer.Length > 0 && counts[_sortIncBuffer.ptr[0]].count <= 0)
                    {
                        _span = span.Slice(0, 0).GetEnumerator();
                        return;
                    }
                    #endregion

                    _span = span.GetEnumerator();
                }
                public int Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get { return _span.Current; }
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public bool MoveNext()
                {
                    while (_span.MoveNext())
                    {
                        int chunck = _span.Current << _entityComponentMaskLengthBitShift;
                        for (int i = 0; i < _sortIncChunckBuffer.Length; i++)
                        {
                            var bit = _sortIncChunckBuffer.ptr[i];
                            if ((_entityComponentMasks[chunck + bit.chunkIndex] & bit.mask) != bit.mask)
                            {
                                goto skip;
                            }
                        }
                        return true;
                        skip: continue;
                    }
                    return false;
                }
            }
            #endregion
        }
        #endregion
    }
    #endregion
}

namespace DCFApixels.DragonECS.Internal
{
    #region EcsMaskIteratorUtility
#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal unsafe class EcsMaskIteratorUtility
    {
        internal const int STACK_BUFFER_THRESHOLD = 256;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ConvertToChuncks(EcsMaskChunck* ptr, UnsafeArray<int> input, UnsafeArray<EcsMaskChunck> output)
        {
            for (int i = 0; i < input.Length; i++)
            {
                ptr[i] = EcsMaskChunck.FromID(input.ptr[i]);
            }

            for (int inputI = 0, outputI = 0; outputI < output.Length; inputI++, ptr++)
            {
                int maskX = ptr->mask;
                if (maskX == 0) { continue; }
                int chunkIndexX = ptr->chunkIndex;

                EcsMaskChunck* subptr = ptr;
                for (int j = 1; j < input.Length - inputI; j++, subptr++)
                {
                    if (subptr->chunkIndex == chunkIndexX)
                    {
                        maskX |= subptr->mask;
                        *subptr = default;
                    }
                }
                output.ptr[outputI] = new EcsMaskChunck(chunkIndexX, maskX);
                outputI++;
            }
        }

#if ENABLE_IL2CPP
        [Il2CppSetOption (Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
        internal readonly struct IncCountComparer : IStructComparer<int>
        {
            public readonly EcsWorld.PoolSlot[] counts;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IncCountComparer(EcsWorld.PoolSlot[] counts)
            {
                this.counts = counts;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(int a, int b)
            {
                return counts[a].count - counts[b].count;
            }
        }

#if ENABLE_IL2CPP
        [Il2CppSetOption (Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
        internal readonly struct ExcCountComparer : IStructComparer<int>
        {
            public readonly EcsWorld.PoolSlot[] counts;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ExcCountComparer(EcsWorld.PoolSlot[] counts)
            {
                this.counts = counts;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(int a, int b)
            {
                return counts[b].count - counts[a].count;
            }
        }
    }
    #endregion
}