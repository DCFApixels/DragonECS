using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    public abstract partial class EcsWorld
    {
        private Dictionary<EcsMask.BuilderMaskKey, EcsMask> _masks = new Dictionary<EcsMask.BuilderMaskKey, EcsMask>(256);
        internal EcsMask GetMask_Internal(EcsMask.BuilderMaskKey maskKey)
        {
            if (!_masks.TryGetValue(maskKey, out EcsMask result))
            {
                result = new EcsMask(_masks.Count, id, maskKey.inc, maskKey.exc);
                _masks.Add(maskKey, result);
            }
            return result;
        }
    }

    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public sealed class EcsMask : IEquatable<EcsMask>
    {
        internal readonly int id;
        internal readonly int worldID;
        internal readonly EcsMaskChunck[] incChunckMasks;
        internal readonly EcsMaskChunck[] excChunckMasks;
        internal readonly int[] inc;
        internal readonly int[] exc;
        public int WorldID => worldID;
        public EcsWorld World => EcsWorld.GetWorld(worldID);
        /// <summary>Including constraints</summary>
        public ReadOnlySpan<int> Inc => inc;
        /// <summary>Excluding constraints</summary>
        public ReadOnlySpan<int> Exc => exc;
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

        #region Object
        public override string ToString() => CreateLogString(worldID, inc, exc);
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
                if (CheckRepeats(inc)) throw new EcsFrameworkException("The values in the Include constraints are repeated.");
                if (CheckRepeats(exc)) throw new EcsFrameworkException("The values in the Exclude constraints are repeated.");
                _dummyHashSet.Clear();
                _dummyHashSet.UnionWith(inc);
                if (_dummyHashSet.Overlaps(exc)) throw new EcsFrameworkException("Conflicting Include and Exclude constraints.");
            }
        }
        private bool CheckRepeats(int[] array)
        {
            _dummyHashSet.Clear();
            foreach (var item in array)
            {
                if (_dummyHashSet.Contains(item)) return true;
                _dummyHashSet.Add(item);
            }
            return false;
        }
#endif
        private static string CreateLogString(int worldID, int[] inc, int[] exc)
        {
#if (DEBUG && !DISABLE_DEBUG)
            string converter(int o) => EcsDebugUtility.GetGenericTypeName(EcsWorld.GetWorld(worldID).AllPools[o].ComponentType, 1);
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
                Type converter(int o) => world.GetComponentType(o);
                includedTypes = included.Select(converter).ToArray();
                excludedTypes = excluded.Select(converter).ToArray();
            }
            public override string ToString() => CreateLogString(_worldID, included, excluded);
        }
        #endregion

        #region Builder
        public readonly struct BuilderMaskKey : IEquatable<BuilderMaskKey>
        {
            public readonly int[] inc;
            public readonly int[] exc;
            public readonly int hash;
            public BuilderMaskKey(int[] inc, int[] exc, int hash)
            {
                this.inc = inc;
                this.exc = exc;
                this.hash = hash;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(BuilderMaskKey other)
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
#if DEBUG
                if (other.hash != hash)
                {
                    throw new Exception("other.hash != hash");
                }
#endif
                return true;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode() => hash;
        }

        public class Builder
        {
            private readonly EcsWorld _world;
            private readonly HashSet<int> _inc = new HashSet<int>();
            private readonly HashSet<int> _exc = new HashSet<int>();
            private readonly List<Combined> _combined = new List<Combined>();

            internal Builder(EcsWorld world)
            {
                _world = world;
            }

            public void Include<T>()
            {
                int id = _world.GetComponentID<T>();
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(id) || _exc.Contains(id)) Throw.ConstraintIsAlreadyContainedInMask(typeof(T));
#endif
                _inc.Add(id);
            }
            public void Exclude<T>()
            {
                int id = _world.GetComponentID<T>();
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(id) || _exc.Contains(id)) Throw.ConstraintIsAlreadyContainedInMask(typeof(T));
#endif
                _exc.Add(id);
            }
            public void Include(Type type)
            {
                int id = _world.GetComponentID(type);
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(id) || _exc.Contains(id)) Throw.ConstraintIsAlreadyContainedInMask(type);
#endif
                _inc.Add(id);
            }
            public void Exclude(Type type)
            {
                int id = _world.GetComponentID(type);
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (_inc.Contains(id) || _exc.Contains(id)) Throw.ConstraintIsAlreadyContainedInMask(type);
#endif
                _exc.Add(id);
            }

            public void CombineWith(EcsMask mask, int order = 0)
            {
                _combined.Add(new Combined(mask, order));
            }

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
                var exc = combinedExc.ToArray();
                Array.Sort(exc);

                unchecked
                {
                    int keyHash = inc.Length + exc.Length;
                    for (int i = 0, iMax = inc.Length; i < iMax; i++)
                    {
                        keyHash = keyHash * EcsConsts.MAGIC_PRIME + inc[i];
                    }
                    for (int i = 0, iMax = exc.Length; i < iMax; i++)
                    {
                        keyHash = keyHash * EcsConsts.MAGIC_PRIME - exc[i];
                    }
                    BuilderMaskKey key = new BuilderMaskKey(inc, exc, keyHash);
                    return _world.GetMask_Internal(key);
                }
            }
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
}
