using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public sealed class EcsMask : IEquatable<EcsMask>
    {
        internal readonly int id;
        internal readonly short worldID;
        internal readonly EcsMaskChunck[] incChunckMasks;
        internal readonly EcsMaskChunck[] excChunckMasks;
        internal readonly int[] inc; //Sorted
        internal readonly int[] exc; //Sorted

        #region Properties
        public int ID
        {
            get { return id; }
        }
        public short WorldID
        {
            get { return worldID; }
        }
        public EcsWorld World
        {
            get { return EcsWorld.GetWorld(worldID); }
        }
        /// <summary>Including constraints</summary>
        public ReadOnlySpan<int> Inc
        {
            get { return inc; }
        }
        /// <summary>Excluding constraints</summary>
        public ReadOnlySpan<int> Exc
        {
            get { return exc; }
        }
        public bool IsEmpty
        {
            get { return inc.Length == 0 && exc.Length == 0; }
        }
        public bool IsBroken
        {
            get { return (inc.Length & exc.Length) == 1 && inc[0] == exc[0]; }
        }
        #endregion

        #region Constructors
        public static Builder New(EcsWorld world)
        {
            return new Builder(world);
        }

        internal static EcsMask New(int id, short worldID, int[] inc, int[] exc)
        {
#if DEBUG
            CheckConstraints(inc, exc);
#endif
            return new EcsMask(id, worldID, inc, exc);
        }
        internal static EcsMask NewEmpty(int id, short worldID)
        {
            return new EcsMask(id, worldID, new int[0], new int[0]);
        }
        internal static EcsMask NewBroken(int id, short worldID)
        {
            return new EcsMask(id, worldID, new int[1] { 1 }, new int[1] { 1 });
        }
        private EcsMask(int id, short worldID, int[] inc, int[] exc)
        {
            this.id = id;
            this.inc = inc;
            this.exc = exc;
            this.worldID = worldID;

            incChunckMasks = MakeMaskChuncsArray(inc);
            excChunckMasks = MakeMaskChuncsArray(exc);
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
                    if (bitJ.chankIndex != chankIndexX)
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
            return OverlapsArray(inc, otherMask.exc) || OverlapsArray(exc, otherMask.inc);
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
            return IsSubarray(sub.inc, super.inc) && IsSuperarray(sub.exc, super.exc);
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
            return CreateLogString(worldID, inc, exc);
        }
        public bool Equals(EcsMask mask)
        {
            return id == mask.id && worldID == mask.worldID;
        }
        public override bool Equals(object obj)
        {
            return obj is EcsMask mask && id == mask.id && Equals(mask);
        }
        public override int GetHashCode()
        {
            return unchecked(id ^ (worldID * EcsConsts.MAGIC_PRIME));
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

                ID = mask.id;
                world = EcsWorld.GetWorld(mask.worldID);
                _worldID = mask.worldID;
                includedChunkMasks = mask.incChunckMasks;
                excludedChunkMasks = mask.excChunckMasks;
                included = mask.inc;
                excluded = mask.exc;
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
        #endregion

        #region OpMaskKey
        private readonly struct OpMaskKey : IEquatable<OpMaskKey>
        {
            public readonly int leftMaskID;
            public readonly int rightMaskID;
            public readonly int operation;

            public const int UNION_OP = 7;
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

        #region Builder
        private readonly struct WorldMaskComponent : IEcsWorldComponent<WorldMaskComponent>
        {
            private readonly EcsWorld _world;
            private readonly Dictionary<Key, EcsMask> _masks;
            private readonly Dictionary<OpMaskKey, EcsMask> _opMasks;

            public readonly EcsMask EmptyMask;
            public readonly EcsMask BrokenMask;

            #region Constructor/Destructor
            public WorldMaskComponent(EcsWorld world, Dictionary<Key, EcsMask> masks, Dictionary<OpMaskKey, EcsMask> opMasks, EcsMask emptyMask, EcsMask brokenMask)
            {
                _world = world;
                _masks = masks;
                _opMasks = opMasks;
                EmptyMask = emptyMask;
                BrokenMask = brokenMask;
            }
            public void Init(ref WorldMaskComponent component, EcsWorld world)
            {
                var masks = new Dictionary<Key, EcsMask>(256);
                EcsMask emptyMask = NewEmpty(0, world.id);
                EcsMask brokenMask = NewBroken(1, world.id);
                masks.Add(new Key(emptyMask.inc, emptyMask.exc), emptyMask);
                masks.Add(new Key(brokenMask.inc, brokenMask.exc), brokenMask);
                component = new WorldMaskComponent(world, masks, new Dictionary<OpMaskKey, EcsMask>(256), emptyMask, brokenMask);
            }
            public void OnDestroy(ref WorldMaskComponent component, EcsWorld world)
            {
                component._masks.Clear();
                component._opMasks.Clear();
                component = default;
            }
            #endregion

            #region GetMask
            internal EcsMask ExceptMask(EcsMask a, EcsMask b)
            {
                int operation = OpMaskKey.EXCEPT_OP;
                if (_opMasks.TryGetValue(new OpMaskKey(a.id, b.id, operation), out EcsMask result) == false)
                {
                    if (a.IsConflictWith(b))
                    {
                        return a.World.Get<WorldMaskComponent>().BrokenMask;
                    }
                    result = New(a.World).Combine(a).Except(b).Build();
                    _opMasks.Add(new OpMaskKey(a.id, b.id, operation), result);
                }
                return result;
            }
            //internal EcsMask ExceptMask(EcsMask a, EcsMask b)
            //{
            //    int operation = OpMaskKey.EXCEPT_OP;
            //    if (_opMasks.TryGetValue(new OpMaskKey(a.id, b.id, operation), out EcsMask result) == false)
            //    {
            //        var builder = New(a.World);
            //        if (a.IsConflictWith(b))
            //        {
            //            return a.World.Get<WorldMaskComponent>().BrokenMask;
            //        }
            //        ExceptMaskConstraint(builder, a.inc, b.inc, true);
            //        ExceptMaskConstraint(builder, a.exc, b.exc, false);
            //        result = builder.Build();
            //        _opMasks.Add(new OpMaskKey(a.id, b.id, operation), result);
            //    }
            //    return result;
            //}
            //private void ExceptMaskConstraint(Builder b, int[] acnstrs, int[] bcnstrs, bool isInc)
            //{
            //    for (int i = 0, ii = 0; i < acnstrs.Length; i++)
            //    {
            //        int acnst = acnstrs[i];
            //        while (ii < bcnstrs.Length && acnst > bcnstrs[ii])
            //        {
            //            ii++;
            //        }
            //        if (ii >= bcnstrs.Length)
            //        {
            //            break;
            //        }
            //        int binc = bcnstrs[ii];
            //        if (acnst == binc)
            //        {
            //            if (isInc)
            //            {
            //                b.Include(acnst);
            //            }
            //            else
            //            {
            //                b.Exclude(acnst);
            //            }
            //        }
            //    }
            //}
            internal EcsMask GetMask(Key maskKey)
            {
                if (!_masks.TryGetValue(maskKey, out EcsMask result))
                {
                    result = New(_masks.Count, _world.id, maskKey.inc, maskKey.exc);
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
                if (inc.Length != other.inc.Length)
                {
                    return false;
                }
                if (exc.Length != other.exc.Length)
                {
                    return false;
                }
                for (int i = 0; i < inc.Length; i++)
                {
                    if (inc[i] != other.inc[i])
                    {
                        return false;
                    }
                }
                for (int i = 0; i < exc.Length; i++)
                {
                    if (exc[i] != other.exc[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() => hash;
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

            #region Include/Exclude/Combine
            public Builder Include<T>()
            {
                return Include(_world.GetComponentTypeID<T>());
            }
            public Builder Exclude<T>()
            {
                return Exclude(_world.GetComponentTypeID<T>());
            }
            public Builder Include(Type type)
            {
                return Include(_world.GetComponentTypeID(type));
            }
            public Builder Exclude(Type type)
            {
                return Exclude(_world.GetComponentTypeID(type));
            }

            public Builder Include(int compponentTypeID)
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(compponentTypeID) || _exc.Contains(compponentTypeID)) Throw.ConstraintIsAlreadyContainedInMask(_world.GetComponentType(compponentTypeID));
#endif
                _inc.Add(compponentTypeID);
                return this;
            }
            public Builder Exclude(int compponentTypeID)
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(compponentTypeID) || _exc.Contains(compponentTypeID)) Throw.ConstraintIsAlreadyContainedInMask(_world.GetComponentType(compponentTypeID));
#endif
                _exc.Add(compponentTypeID);
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
                        combinedInc.ExceptWith(submask.exc);//удаляю конфликтующие ограничения
                        combinedExc.ExceptWith(submask.inc);//удаляю конфликтующие ограничения
                        combinedInc.UnionWith(submask.inc);
                        combinedExc.UnionWith(submask.exc);
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
                        if(combinedInc.Overlaps(item.mask.exc) || combinedExc.Overlaps(item.mask.inc))
                        {
                            _combineds.Clear();
                            _excepteds.Clear();
                            return _world.Get<WorldMaskComponent>().BrokenMask;
                        }
                        combinedInc.ExceptWith(item.mask.inc);
                        combinedExc.ExceptWith(item.mask.exc);
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

        public readonly int chankIndex;
        public readonly int mask;
        public EcsMaskChunck(int chankIndex, int mask)
        {
            this.chankIndex = chankIndex;
            this.mask = mask;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsMaskChunck FromID(int id)
        {
            return new EcsMaskChunck(id >> DIV_SHIFT, 1 << (id & MOD_MASK));
        }
        public override string ToString()
        {
            return $"mask({chankIndex}, {mask}, {BitsUtility.CountBits(mask)})";
        }
        internal class DebuggerProxy
        {
            public int chunk;
            public uint mask;
            public int[] values = Array.Empty<int>();
            public string bits;
            public DebuggerProxy(EcsMaskChunck maskbits)
            {
                chunk = maskbits.chankIndex;
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
}
