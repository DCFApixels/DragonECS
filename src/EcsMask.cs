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
        internal readonly int worldID;
        internal readonly EcsMaskChunck[] incChunckMasks;
        internal readonly EcsMaskChunck[] excChunckMasks;
        internal readonly int[] inc;
        internal readonly int[] exc;

        #region Properties
        public int ID
        {
            get { return id; }
        }
        public int WorldID
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
        #endregion

        #region Constructors
        public static Builder New(EcsWorld world)
        {
            return new Builder(world);
        }
        internal EcsMask(int id, int worldID, int[] inc, int[] exc)
        {
#if DEBUG
            CheckConstraints(inc, exc);
#endif
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
        public bool IsSubmaskOf(EcsMask otherMask) //TODO протестить
        {
            return IsSubmask(otherMask, this);
        }
        public bool IsSupermaskOf(EcsMask otherMask) //TODO протестить
        {
            return IsSubmask(this, otherMask);
        }
        public bool IsConflictWith(EcsMask otherMask) //TODO протестить
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
        private void CheckConstraints(int[] inc, int[] exc)
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
        private bool CheckRepeats(int[] array)
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
        private static string CreateLogString(int worldID, int[] inc, int[] exc)
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
            public readonly int ID;
            public readonly EcsWorld world;
            private readonly int _worldID;
            public readonly EcsMaskChunck[] includedChunkMasks;
            public readonly EcsMaskChunck[] excludedChunkMasks;
            public readonly int[] included;
            public readonly int[] excluded;
            public readonly Type[] includedTypes;
            public readonly Type[] excludedTypes;

            public DebuggerProxy(EcsMask mask)
            {
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

        #region Builder
        private readonly struct WorldMaskComponent : IEcsWorldComponent<WorldMaskComponent>
        {
            private readonly EcsWorld _world;
            private readonly Dictionary<Key, EcsMask> _masks;

            #region Constructor/Destructor
            public WorldMaskComponent(EcsWorld world, Dictionary<Key, EcsMask> masks)
            {
                _world = world;
                _masks = masks;
            }
            public void Init(ref WorldMaskComponent component, EcsWorld world)
            {
                component = new WorldMaskComponent(world, new Dictionary<Key, EcsMask>(256));
            }
            public void OnDestroy(ref WorldMaskComponent component, EcsWorld world)
            {
                component._masks.Clear();
                component = default;
            }
            #endregion

            #region GetMask
            internal EcsMask GetMask(Key maskKey)
            {
                if (!_masks.TryGetValue(maskKey, out EcsMask result))
                {
                    result = new EcsMask(_masks.Count, _world.id, maskKey.inc, maskKey.exc);
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
            private readonly List<Combined> _combined = new List<Combined>();

            #region Constrcutors
            internal Builder(EcsWorld world)
            {
                _world = world;
            }
            #endregion

            #region Include/Exclude/Combine
            public Builder Include<T>()
            {
                int id = _world.GetComponentTypeID<T>();
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(id) || _exc.Contains(id)) Throw.ConstraintIsAlreadyContainedInMask(typeof(T));
#endif
                _inc.Add(id);
                return this;
            }
            public Builder Exclude<T>()
            {
                int id = _world.GetComponentTypeID<T>();
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(id) || _exc.Contains(id)) Throw.ConstraintIsAlreadyContainedInMask(typeof(T));
#endif
                _exc.Add(id);
                return this;
            }
            public Builder Include(Type type)
            {
                int id = _world.GetComponentTypeID(type);
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(id) || _exc.Contains(id)) Throw.ConstraintIsAlreadyContainedInMask(type);
#endif
                _inc.Add(id);
                return this;
            }
            public Builder Exclude(Type type)
            {
                int id = _world.GetComponentTypeID(type);
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(id) || _exc.Contains(id)) Throw.ConstraintIsAlreadyContainedInMask(type);
#endif
                _exc.Add(id);
                return this;
            }
            public Builder Combine(EcsMask mask, int order = 0)
            {
                _combined.Add(new Combined(mask, order));
                return this;
            }
            #endregion

            #region Build
            public EcsMask Build()
            {
                HashSet<int> combinedInc;
                HashSet<int> combinedExc;
                if (_combined.Count > 0)
                {
                    combinedInc = new HashSet<int>();
                    combinedExc = new HashSet<int>();
                    _combined.Sort((a, b) => a.order - b.order);
                    foreach (var item in _combined)
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

                var inc = combinedInc.ToArray();
                Array.Sort(inc);
                _inc.Clear();
                var exc = combinedExc.ToArray();
                Array.Sort(exc);
                _exc.Clear();

                _combined.Clear();

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
