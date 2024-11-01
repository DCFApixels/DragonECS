using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace DCFApixels.DragonECS.Core
{
    public interface IComponentMask
    {
        EcsMask ToMask(EcsWorld world);
    }
}

namespace DCFApixels.DragonECS
{
    using static EcsMaskIteratorUtility;


#if ENABLE_IL2CPP
    [Il2CppSetOption (Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public sealed class EcsMask : IEquatable<EcsMask>, IComponentMask
    {
        public readonly int ID;
        public readonly short WorldID;

        internal readonly EcsStaticMask _staticMask;
        internal readonly EcsMaskChunck[] _incChunckMasks;
        internal readonly EcsMaskChunck[] _excChunckMasks;
        /// <summary> Sorted </summary>
        internal readonly int[] _incs;
        /// <summary> Sorted </summary>
        internal readonly int[] _excs;

        private EcsMaskIterator _iterator;

        #region Properties
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return EcsWorld.GetWorld(WorldID); }
        }
        /// <summary> Sorted set excluding constraints. </summary>
        public ReadOnlySpan<int> Incs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _incs; }
        }
        /// <summary> Sorted set excluding constraints. </summary>
        public ReadOnlySpan<int> Excs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _excs; }
        }
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _incs.Length == 0 && _excs.Length == 0; }
        }
        public bool IsBroken
        {
            get { return (_incs.Length & _excs.Length) == 1 && _incs[0] == _excs[0]; }
        }
        #endregion

        #region Constructors
        public static Builder New(EcsWorld world) { return new Builder(world); }
        internal static EcsMask CreateEmpty(int id, short worldID)
        {
            return new EcsMask(EcsStaticMask.Empty, id, worldID, new int[0], new int[0]);
        }
        internal static EcsMask CreateBroken(int id, short worldID)
        {
            return new EcsMask(EcsStaticMask.Broken, id, worldID, new int[1] { 1 }, new int[1] { 1 });
        }
        private EcsMask(EcsStaticMask staticMask, int id, short worldID, int[] inc, int[] exc)
        {
            _staticMask = staticMask;
            ID = id;
            _incs = inc;
            _excs = exc;
            WorldID = worldID;

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
            return _staticMask.IsSubmaskOf(otherMask._staticMask);
        }
        public bool IsSupermaskOf(EcsMask otherMask)
        {
            return _staticMask.IsSupermaskOf(otherMask._staticMask);
        }
        public bool IsConflictWith(EcsMask otherMask)
        {
            return _staticMask.IsConflictWith(otherMask._staticMask);
        }
        #endregion

        #region Object
        public override string ToString()
        {
            return CreateLogString(WorldID, _incs, _excs);
        }
        public bool Equals(EcsMask mask)
        {
            return ID == mask.ID && WorldID == mask.WorldID;
        }
        public override bool Equals(object obj)
        {
            return obj is EcsMask mask && ID == mask.ID && Equals(mask);
        }
        public override int GetHashCode()
        {
            return unchecked(ID ^ (WorldID * EcsConsts.MAGIC_PRIME));
        }
        #endregion

        #region Other
        public static EcsMask FromStatic(EcsWorld world, EcsStaticMask abstractMask)
        {
            return world.Get<WorldMaskComponent>().ConvertFromStatic(abstractMask);
        }
        EcsMask IComponentMask.ToMask(EcsWorld world) { return this; }
        public EcsMaskIterator GetIterator()
        {
            if (_iterator == null)
            {
                _iterator = new EcsMaskIterator(EcsWorld.GetWorld(WorldID), this);
            }
            return _iterator;
        }
        #endregion

        #region Operators
        public static EcsMask operator -(EcsMask a, EcsMask b)
        {
            return a.World.Get<WorldMaskComponent>().ExceptMask(a, b);
        }
        public static EcsMask operator -(EcsMask a, IComponentMask b)
        {
            return a.World.Get<WorldMaskComponent>().ExceptMask(a, b.ToMask(a.World));
        }
        public static EcsMask operator -(IComponentMask b, EcsMask a)
        {
            return a.World.Get<WorldMaskComponent>().ExceptMask(b.ToMask(a.World), a);
        }
        public static EcsMask operator +(EcsMask a, EcsMask b)
        {
            return a.World.Get<WorldMaskComponent>().CombineMask(a, b);
        }
        public static EcsMask operator +(EcsMask a, IComponentMask b)
        {
            return a.World.Get<WorldMaskComponent>().CombineMask(a, b.ToMask(a.World));
        }
        public static EcsMask operator +(IComponentMask b, EcsMask a)
        {
            return a.World.Get<WorldMaskComponent>().CombineMask(b.ToMask(a.World), a);
        }
        public static implicit operator EcsMask((IComponentMask mask, EcsWorld world) a)
        {
            return a.mask.ToMask(a.world);
        }
        public static implicit operator EcsMask((EcsWorld world, IComponentMask mask) a)
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
                return leftMaskID == other.leftMaskID && rightMaskID == other.rightMaskID && operation == other.operation;
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
            private readonly Dictionary<OpMaskKey, EcsMask> _opMasks;
            private readonly SparseArray<EcsMask> _staticMasks;

            public readonly EcsMask EmptyMask;
            public readonly EcsMask BrokenMask;

            #region Constructor/Destructor
            public WorldMaskComponent(EcsWorld world)
            {
                _world = world;
                _opMasks = new Dictionary<OpMaskKey, EcsMask>(256);
                _staticMasks = new SparseArray<EcsMask>(256);


                EmptyMask = CreateEmpty(_staticMasks.Count, world.ID);
                _staticMasks.Add(EmptyMask._staticMask.ID, EmptyMask);
                BrokenMask = CreateBroken(_staticMasks.Count, world.ID);
                _staticMasks.Add(BrokenMask._staticMask.ID, BrokenMask);
            }
            public void Init(ref WorldMaskComponent component, EcsWorld world)
            {
                component = new WorldMaskComponent(world);
            }
            public void OnDestroy(ref WorldMaskComponent component, EcsWorld world)
            {
                component._opMasks.Clear();
                component._staticMasks.Clear();
                component = default;
            }
            #endregion

            #region GetMask
            internal EcsMask CombineMask(EcsMask a, EcsMask b)
            {
                int operation = OpMaskKey.COMBINE_OP;
                if (_opMasks.TryGetValue(new OpMaskKey(a.ID, b.ID, operation), out EcsMask result) == false)
                {
                    if (a.IsConflictWith(b))
                    {
                        return a.World.Get<WorldMaskComponent>().BrokenMask;
                    }
                    result = ConvertFromStatic(EcsStaticMask.New().Combine(a._staticMask).Combine(b._staticMask).Build());
                    _opMasks.Add(new OpMaskKey(a.ID, b.ID, operation), result);
                }
                return result;
            }
            internal EcsMask ExceptMask(EcsMask a, EcsMask b)
            {
                int operation = OpMaskKey.EXCEPT_OP;
                if (_opMasks.TryGetValue(new OpMaskKey(a.ID, b.ID, operation), out EcsMask result) == false)
                {
                    if (a.IsConflictWith(b))
                    {
                        return a.World.Get<WorldMaskComponent>().BrokenMask;
                    }
                    result = ConvertFromStatic(EcsStaticMask.New().Combine(a._staticMask).Except(b._staticMask).Build());
                    _opMasks.Add(new OpMaskKey(a.ID, b.ID, operation), result);
                }
                return result;
            }

            internal EcsMask ConvertFromStatic(EcsStaticMask staticMask)
            {
                int[] ConvertTypeCodeToComponentTypeID(ReadOnlySpan<EcsTypeCode> from, EcsWorld world)
                {
                    int[] to = new int[from.Length];
                    for (int i = 0; i < to.Length; i++)
                    {
                        to[i] = world.DeclareOrGetComponentTypeID(from[i]);
                    }
                    Array.Sort(to);
                    return to;
                }

                if (_staticMasks.TryGetValue(staticMask.ID, out EcsMask result) == false)
                {
                    int[] incs = ConvertTypeCodeToComponentTypeID(staticMask.IncTypeCodes, _world);
                    int[] excs = ConvertTypeCodeToComponentTypeID(staticMask.ExcTypeCodes, _world);

                    result = new EcsMask(staticMask, _staticMasks.Count, _world.ID, incs, excs);

                    _staticMasks.Add(staticMask.ID, result);
                }
                return result;
            }
            #endregion
        }

        public partial struct Builder
        {
            private readonly EcsStaticMask.Builder _builder;
            private readonly EcsWorld _world;

            public Builder(EcsWorld world)
            {
                _world = world;
                _builder = EcsStaticMask.New();
            }

            public Builder Inc<T>() { _builder.Inc<T>(); return this; }
            public Builder Exc<T>() { _builder.Exc<T>(); return this; }
            public Builder Inc(Type type) { _builder.Inc(type); return this; }
            public Builder Exc(Type type) { _builder.Exc(type); return this; }
            public Builder Inc(EcsTypeCode typeCode) { _builder.Inc(typeCode); return this; }
            public Builder Exc(EcsTypeCode typeCode) { _builder.Exc(typeCode); return this; }
            public Builder Combine(EcsMask mask) { _builder.Combine(mask._staticMask); return this; }
            public Builder Except(EcsMask mask) { _builder.Except(mask._staticMask); return this; }

            public EcsMask Build() { return _world.Get<WorldMaskComponent>().ConvertFromStatic(_builder.Build()); }
        }
        #endregion

        #region Debug utils
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

                ID = mask.ID;
                world = EcsWorld.GetWorld(mask.WorldID);
                _worldID = mask.WorldID;
                includedChunkMasks = mask._incChunckMasks;
                excludedChunkMasks = mask._excChunckMasks;
                included = mask._incs;
                excluded = mask._excs;
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

        #region Obsolete
        /// <summary> Sorted set including constraints. </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use Incs")]
        public ReadOnlySpan<int> Inc
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _incs; }
        }
        /// <summary> Sorted set excluding constraints. </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use Excs")]
        public ReadOnlySpan<int> Exc
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _excs; }
        }
        public partial struct Builder
        {
            [EditorBrowsable(EditorBrowsableState.Never)][Obsolete] public Builder Include<T>() { return Inc<T>(); }
            [EditorBrowsable(EditorBrowsableState.Never)][Obsolete] public Builder Exclude<T>() { return Exc<T>(); }
            [EditorBrowsable(EditorBrowsableState.Never)][Obsolete] public Builder Include(Type type) { return Inc(type); }
            [EditorBrowsable(EditorBrowsableState.Never)][Obsolete] public Builder Exclude(Type type) { return Exc(type); }
            [EditorBrowsable(EditorBrowsableState.Never)][Obsolete] public Builder Inc(int componentTypeID) { Inc(_world.GetComponentType(componentTypeID)); return this; }
            [EditorBrowsable(EditorBrowsableState.Never)][Obsolete] public Builder Exc(int componentTypeID) { Exc(_world.GetComponentType(componentTypeID)); return this; }
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
            _sortIncBuffer = UnsafeArray<int>.FromArray(mask._incs);
            _sortExcBuffer = UnsafeArray<int>.FromArray(mask._excs);
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