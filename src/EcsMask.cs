#if DISABLE_DEBUG
#undef DEBUG
#endif
using DCFApixels.DragonECS.Core;
using DCFApixels.DragonECS.Core.Internal;
using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Defines a mask — a collection of component conditions used to filter entities in queries.
    /// The mask consists of three sets:
    /// <list type="bullet">
    ///   <item><description><b>Include (Inc)</b> – entity must have <i>all</i> of these components.</description></item>
    ///   <item><description><b>Exclude (Exc)</b> – entity must have <i>none</i> of these components.</description></item>
    ///   <item><description><b>Any (Any)</b> – entity must have <i>at least one</i> of these components.</description></item>
    /// </list>
    /// Conditions are evaluated per entity during queries or iteration.
    /// </summary>
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public sealed class EcsMask : IEquatable<EcsMask>, IComponentMask
    {
        public readonly int ID;
        public readonly short WorldID;
        public readonly EcsWorld World;

        internal readonly EcsStaticMask _staticMask;
        internal readonly EcsMaskChunck[] _incChunckMasks;
        internal readonly EcsMaskChunck[] _excChunckMasks;
        internal readonly EcsMaskChunck[] _anyChunckMasks;
        /// <summary> Sorted </summary>
        internal readonly int[] _incs;
        /// <summary> Sorted </summary>
        internal readonly int[] _excs;
        /// <summary> Sorted </summary>
        internal readonly int[] _anys;

        internal readonly EcsMaskFlags _flags;

        private EcsMaskIterator _iterator;

        #region Properties
        /// <summary>Gets the sorted set of component type IDs that are required (include conditions).</summary>
        public ReadOnlySpan<int> Incs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _incs; }
        }

        /// <summary>Gets the sorted set of component type IDs that are forbidden (exclude conditions).</summary>
        public ReadOnlySpan<int> Excs
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _excs; }
        }

        /// <summary>
        /// Gets the sorted set of component type IDs that form the "any" condition.
        /// An entity matches this mask only if it has at least one of these components.
        /// </summary>
        public ReadOnlySpan<int> Anys
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _anys; }
        }

        /// <summary>Gets the sorted set of required component type codes (global type codes).</summary>
        public ReadOnlySpan<EcsTypeCode> IncTypeCodes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ToStatic().IncTypeCodes; }
        }

        /// <summary>Gets the sorted set of forbidden component type codes (global type codes).</summary>
        public ReadOnlySpan<EcsTypeCode> ExcTypeCodes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ToStatic().ExcTypeCodes; }
        }

        /// <summary>
        /// Gets the sorted set of component type codes that form the "any" condition.
        /// An entity matches this mask only if it has at least one of the corresponding components.
        /// </summary>
        public ReadOnlySpan<EcsTypeCode> AnyTypeCodes
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ToStatic().AnyTypeCodes; }
        }

        /// <summary>Gets the flags indicating which condition groups are present.</summary>
        public EcsMaskFlags Flags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _flags; }
        }

        /// <summary>Indicates whether the mask has no conditions (empty mask).</summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _flags == EcsMaskFlags.Empty; }
        }

        /// <summary>Indicates whether the mask is in a broken state (e.g., conflicting conditions).</summary>
        public bool IsBroken
        {
            get { return (_flags & EcsMaskFlags.Broken) != 0; }
        }
        #endregion

        #region Constructors
        /// <summary>Creates a new fluent builder for constructing a mask in the specified world.</summary>
        /// <param name="world">The world to which the mask will belong.</param>
        /// <returns>A new <see cref="Builder"/> instance.</returns>
        public static Builder New(EcsWorld world) { return new Builder(world); }
        internal static EcsMask CreateEmpty(int id, short worldID)
        {
            return new EcsMask(EcsStaticMask.Empty, id, worldID);
        }
        internal static EcsMask CreateBroken(int id, short worldID)
        {
            return new EcsMask(EcsStaticMask.Broken, id, worldID);
        }
        private EcsMask(EcsStaticMask staticMask, int id, short worldID)
        {
            int[] ConvertTypeCodeToComponentTypeID(ReadOnlySpan<EcsTypeCode> from_, EcsWorld world_)
            {
                int[] to = new int[from_.Length];
                for (int i = 0; i < to.Length; i++)
                {
                    to[i] = world_.DeclareOrGetComponentTypeID(from_[i]);
                }
                Array.Sort(to);
                return to;
            }

            _staticMask = staticMask;
            ID = id;
            WorldID = worldID;
            World = EcsWorld.GetWorld(worldID);
            _flags = staticMask.Flags;

            EcsWorld world = EcsWorld.GetWorld(worldID);
            int[] incs = ConvertTypeCodeToComponentTypeID(staticMask.IncTypeCodes, world);
            int[] excs = ConvertTypeCodeToComponentTypeID(staticMask.ExcTypeCodes, world);
            int[] anys = ConvertTypeCodeToComponentTypeID(staticMask.AnyTypeCodes, world);

            _incs = incs;
            _excs = excs;
            _anys = anys;

            _incChunckMasks = MakeMaskChuncsArray(incs);
            _excChunckMasks = MakeMaskChuncsArray(excs);
            _anyChunckMasks = MakeMaskChuncsArray(anys);
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
        /// <summary>Returns a string representation of the mask, showing the condition sets.</summary>
        public override string ToString()
        {
            return CreateLogString(WorldID, _incs, _excs, _anys);
        }
        /// <summary>Determines whether this mask is equal to another mask (by ID and world).</summary>
        /// <param name="mask">The other mask.</param>
        /// <returns>True if equal; otherwise false.</returns>
        public bool Equals(EcsMask mask)
        {
            return ID == mask.ID && WorldID == mask.WorldID;
        }

        /// <summary>Determines whether this mask equals another object (must be an <see cref="EcsMask"/>).</summary>
        /// <returns>True if equal; otherwise false.</returns>
        public override bool Equals(object obj)
        {
            return obj is EcsMask mask && ID == mask.ID && Equals(mask);
        }

        /// <summary>Gets the hash code for this mask (based on ID and world ID).</summary>
        public override int GetHashCode()
        {
            return unchecked(ID ^ (WorldID * EcsConsts.MAGIC_PRIME));
        }
        #endregion

        #region Other
        /// <summary>Converts a static mask (<see cref="EcsStaticMask"/>) into a world‑specific <see cref="EcsMask"/> for the given world.</summary>
        /// <param name="world">The world to bind the mask to.</param>
        /// <param name="abstractMask">The static mask definition.</param>
        /// <returns>A world‑specific mask instance.</returns>
        public static EcsMask FromStatic(EcsWorld world, EcsStaticMask abstractMask)
        {
            return world.Get<WorldMaskComponent>().ConvertFromStatic(abstractMask);
        }

        /// <summary>Converts this world mask back to its static representation.</summary>
        /// <returns>The underlying <see cref="EcsStaticMask"/>.</returns>
        public EcsStaticMask ToStatic()
        {
            return _staticMask;
        }
        EcsMask IComponentMask.ToMask(EcsWorld world) { return this; }

        /// <summary>Returns an iterator that can be used to perform queries against entities using this mask.</summary>
        /// <returns>An <see cref="EcsMaskIterator"/> instance for this mask.</returns>
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
        internal readonly struct WorldMaskComponent : IEcsWorldComponent<WorldMaskComponent>
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
                if (_staticMasks.TryGetValue(staticMask.ID, out EcsMask result) == false)
                {
                    result = new EcsMask(staticMask, _staticMasks.Count, _world.ID);
                    _staticMasks.Add(staticMask.ID, result);
                }
                return result;
            }
            #endregion
        }

        public readonly partial struct Builder
        {
            private readonly EcsStaticMask.Builder _builder;
            private readonly EcsWorld _world;

            public Builder(EcsWorld world)
            {
                _world = world;
                _builder = EcsStaticMask.New();
            }
            public Builder Inc() { return this; }
            public Builder Exc() { return this; }
            public Builder Any() { return this; }

            /// <summary>Adds an include condition for the component type <typeparamref name="T"/>.</summary>
            /// <typeparam name="T">The component type.</typeparam>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Inc<T>() { _builder.Inc<T>(); return this; }

            /// <summary>Adds an exclude condition for the component type <typeparamref name="T"/>.</summary>
            /// <typeparam name="T">The component type.</typeparam>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Exc<T>() { _builder.Exc<T>(); return this; }

            /// <summary>Adds an any condition for the component type <typeparamref name="T"/>.</summary>
            /// <typeparam name="T">The component type.</typeparam>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Any<T>() { _builder.Any<T>(); return this; }

            /// <summary>Adds an include condition for the specified component type(s).</summary>
            /// <param name="type">The component type.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Inc(Type type) { _builder.Inc(type); return this; }
            /// <summary>Adds an exclude condition for the specified component type(s).</summary>
            /// <param name="type">The component type.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Exc(Type type) { _builder.Exc(type); return this; }
            /// <summary>Adds an any condition for the specified component type(s).</summary>
            /// <param name="type">The component type.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Any(Type type) { _builder.Any(type); return this; }

            /// <summary>Adds an include condition for the specified component type(s).</summary>
            /// <param name="types">Array of component types.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Inc(params Type[] types) { _builder.Inc(types); return this; }

            /// <summary>Adds an exclude condition for the specified component type(s).</summary>
            /// <param name="types">Array of component types.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Exc(params Type[] types) { _builder.Exc(types); return this; }

            /// <summary>Adds an any condition for the specified component type(s).</summary>
            /// <param name="types">Array of component types.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Any(params Type[] types) { _builder.Any(types); return this; }

            /// <summary>Adds an include condition for the specified component type(s).</summary>
            /// <param name="types">Span of component types.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Inc(ReadOnlySpan<Type> types) { _builder.Inc(types); return this; }

            /// <summary>Adds an exclude condition for the specified component type(s).</summary>
            /// <param name="types">Span of component types.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Exc(ReadOnlySpan<Type> types) { _builder.Exc(types); return this; }

            /// <summary>Adds an any condition for the specified component type(s).</summary>
            /// <param name="types">Span of component types.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Any(ReadOnlySpan<Type> types) { _builder.Any(types); return this; }

            /// <summary>Adds an include condition for the specified component type(s).</summary>
            /// <param name="types">Enumerable of component types.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Inc(IEnumerable<Type> types) { _builder.Inc(types); return this; }

            /// <summary>Adds an exclude condition for the specified component type(s).</summary>
            /// <param name="types">Enumerable of component types.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Exc(IEnumerable<Type> types) { _builder.Exc(types); return this; }

            /// <summary>Adds an any condition for the specified component type(s).</summary>
            /// <param name="types">Enumerable of component types.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Any(IEnumerable<Type> types) { _builder.Any(types); return this; }

            /// <summary>Adds an include condition using a global type code.</summary>
            /// <param name="typeCode">The type code.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Inc(EcsTypeCode typeCode) { _builder.Inc(typeCode); return this; }

            /// <summary>Adds an exclude condition using a global type code.</summary>
            /// <param name="typeCode">The type code.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Exc(EcsTypeCode typeCode) { _builder.Exc(typeCode); return this; }

            /// <summary>Adds an any condition using a global type code.</summary>
            /// <param name="typeCode">The type code.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Any(EcsTypeCode typeCode) { _builder.Any(typeCode); return this; }

            /// <summary>Adds an include condition using global type codes.</summary>
            /// <param name="typeCodes">Array of type codes.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Inc(params EcsTypeCode[] typeCodes) { _builder.Inc(typeCodes); return this; }

            /// <summary>Adds an exclude condition using global type codes.</summary>
            /// <param name="typeCodes">Array of type codes.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Exc(params EcsTypeCode[] typeCodes) { _builder.Exc(typeCodes); return this; }

            /// <summary>Adds an any condition using global type codes.</summary>
            /// <param name="typeCodes">Array of type codes.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Any(params EcsTypeCode[] typeCodes) { _builder.Any(typeCodes); return this; }

            /// <summary>Adds an include condition using global type codes.</summary>
            /// <param name="typeCodes">Array of type codes.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Inc(ReadOnlySpan<EcsTypeCode> typeCodes) { _builder.Inc(typeCodes); return this; }

            /// <summary>Adds an exclude condition using global type codes.</summary>
            /// <param name="typeCodes">Array of type codes.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Exc(ReadOnlySpan<EcsTypeCode> typeCodes) { _builder.Exc(typeCodes); return this; }

            /// <summary>Adds an any condition using global type codes.</summary>
            /// <param name="typeCodes">Array of type codes.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Any(ReadOnlySpan<EcsTypeCode> typeCodes) { _builder.Any(typeCodes); return this; }

            /// <summary>Adds an include condition using global type codes.</summary>
            /// <param name="typeCodes">Array of type codes.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Inc(IEnumerable<EcsTypeCode> typeCodes) { _builder.Inc(typeCodes); return this; }

            /// <summary>Adds an exclude condition using global type codes.</summary>
            /// <param name="typeCodes">Array of type codes.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Exc(IEnumerable<EcsTypeCode> typeCodes) { _builder.Exc(typeCodes); return this; }

            /// <summary>Adds an any condition using global type codes.</summary>
            /// <param name="typeCodes">Array of type codes.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Any(IEnumerable<EcsTypeCode> typeCodes) { _builder.Any(typeCodes); return this; }

            /// <summary>Combines the current builder with an existing mask (includes its conditions).</summary>
            /// <param name="mask">The mask to combine.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Combine(EcsMask mask) { _builder.Combine(mask._staticMask); return this; }

            /// <summary>Combines the current builder with an existing mask (includes its conditions).</summary>
            /// <param name="mask">The mask to combine.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Combine(EcsStaticMask mask) { _builder.Combine(mask); return this; }

            /// <summary>Subtracts (excludes) the conditions of an existing mask from the current builder.</summary>
            /// <param name="mask">The mask to subtract.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Except(EcsMask mask) { _builder.Except(mask._staticMask); return this; }

            /// <summary>Subtracts (excludes) the conditions of an existing mask from the current builder.</summary>
            /// <param name="mask">The mask to subtract.</param>
            /// <returns>The builder instance for chaining.</returns>
            public Builder Except(EcsStaticMask mask) { _builder.Except(mask); return this; }

            /// <summary>Builds the final <see cref="EcsMask"/> from the current builder state.</summary>
            /// <returns>A new or cached mask instance.</returns>
            public EcsMask Build() { return _world.Get<WorldMaskComponent>().ConvertFromStatic(_builder.Build()); }

            /// <summary>Implicitly converts a builder directly into an <see cref="EcsMask"/> (calls <see cref="Build"/>).</summary>
            /// <param name="a">The builder.</param>
            /// <returns>The built mask.</returns>
            public static implicit operator EcsMask(Builder a) { return a.Build(); }
        }
        #endregion

        #region Debug utils
        private Type[] _incTypes_Debug;
        private Type[] _excTypes_Debug;
        private Type[] _anyTypes_Debug;
        public ReadOnlySpan<Type> GetIncTypes_Debug()
        {
            if (_incTypes_Debug == null)
            {
                _incTypes_Debug = GetTypes(IncTypeCodes);
            }
            return _incTypes_Debug;
        }
        public ReadOnlySpan<Type> GetExcTypes_Debug()
        {
            if (_excTypes_Debug == null)
            {
                _excTypes_Debug = GetTypes(ExcTypeCodes);
            }
            return _excTypes_Debug;
        }
        public ReadOnlySpan<Type> GetAnyTypes_Debug()
        {
            if (_anyTypes_Debug == null)
            {
                _anyTypes_Debug = GetTypes(AnyTypeCodes);
            }
            return _anyTypes_Debug;
        }
        private Type[] GetTypes(ReadOnlySpan<EcsTypeCode> typeCodes)
        {
            Type[] result = new Type[typeCodes.Length];
            for (int i = 0; i < typeCodes.Length; i++)
            {
                result[i] = EcsTypeCodeManager.FindTypeOfCode(typeCodes[i]).Type;
            }
            return result;
        }
        private static string CreateLogString(short worldID, int[] incs, int[] excs, int[] anys)
        {
#if DEBUG
            string converter(int o) { return EcsDebugUtility.GetGenericTypeName(EcsWorld.GetWorld(worldID).AllPools[o].ComponentType, 1); }
            return $"Inc({string.Join(", ", incs.Select(converter))}); Exc({string.Join(", ", excs.Select(converter))}); Any({string.Join(", ", anys.Select(converter))})";
#else
            return $"Inc({string.Join(", ", incs)}); Exc({string.Join(", ", excs)}; Any({string.Join(", ", anys)})"; // Release optimization
#endif
        }

        internal class DebuggerProxy
        {
            private EcsMask _source;

            public readonly int ID;
            public readonly EcsWorld world;
            private readonly short _worldID;
            public readonly EcsMaskChunck[] incsChunkMasks;
            public readonly EcsMaskChunck[] excsChunkMasks;
            public readonly EcsMaskChunck[] anysChunkMasks;
            public readonly int[] incs;
            public readonly int[] excs;
            public readonly int[] anys;
            public readonly Type[] incsTypes;
            public readonly Type[] excsTypes;
            public readonly Type[] anysTypes;
            public readonly IEcsPool[] incsPools;
            public readonly IEcsPool[] excsPools;
            public readonly IEcsPool[] anysPools;

            public bool IsEmpty { get { return _source.IsEmpty; } }
            public bool IsBroken { get { return _source.IsBroken; } }

            public DebuggerProxy(EcsMask mask)
            {
                _source = mask;

                ID = mask.ID;
                world = EcsWorld.GetWorld(mask.WorldID);
                _worldID = mask.WorldID;
                incsChunkMasks = mask._incChunckMasks;
                excsChunkMasks = mask._excChunckMasks;
                anysChunkMasks = mask._anyChunckMasks;
                incs = mask._incs;
                excs = mask._excs;
                anys = mask._anys;
                IEcsPool converterPool(int o) { return world.FindPoolInstance(o); }
                incsTypes = mask.GetIncTypes_Debug().ToArray();
                excsTypes = mask.GetExcTypes_Debug().ToArray();
                anysTypes = mask.GetAnyTypes_Debug().ToArray();
                incsPools = incs.Select(converterPool).ToArray();
                excsPools = excs.Select(converterPool).ToArray();
                anysPools = anys.Select(converterPool).ToArray();
            }
            public override string ToString()
            {
                return CreateLogString(_worldID, incs, excs, anys);
            }
        }
        #endregion
    }

    /// <summary>Flags that indicate which condition groups are present in a mask.</summary>
    [Flags]
    public enum EcsMaskFlags : byte
    {
        /// <summary>No conditions.</summary>
        Empty = 0,
        /// <summary>Include conditions present.</summary>
        Inc = 1 << 0,
        /// <summary>Exclude conditions present.</summary>
        Exc = 1 << 1,
        /// <summary>Any conditions present.</summary>
        Any = 1 << 2,
        /// <summary>Both Include and Exclude.</summary>
        IncExc = Inc | Exc,
        /// <summary>Include and Any.</summary>
        IncAny = Inc | Any,
        /// <summary>Exclude and Any.</summary>
        ExcAny = Exc | Any,
        /// <summary>All three condition groups.</summary>
        IncExcAny = Inc | Exc | Any,
        /// <summary>Mask is broken (conflict or invalid).</summary>
        Broken = IncExcAny + 1,
    }

    #region EcsMaskChunck
    /// <summary>Represents a chunk (group) of component type IDs within a mask, used for efficient bitmask checks.</summary>
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public readonly struct EcsMaskChunck
    {
        internal const int BITS = 32;
        internal const int DIV_SHIFT = 5;
        internal const int MOD_MASK = BITS - 1;

        /// <summary>The index of the chunk (group).</summary>
        public readonly int chunkIndex;
        /// <summary>The bitmask of component type IDs within this chunk.</summary>
        public readonly int mask;

        /// <summary>Creates a chunk from the given index and mask.</summary>
        /// <param name="chunkIndex">The chunk index.</param>
        /// <param name="mask">The bitmask.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsMaskChunck(int chunkIndex, int mask)
        {
            this.chunkIndex = chunkIndex;
            this.mask = mask;
        }

        /// <summary>Creates a mask chunk from a component type ID (automatically determines chunk index and bit position).</summary>
        /// <param name="id">The component type ID.</param>
        /// <returns>The corresponding <see cref="EcsMaskChunck"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EcsMaskChunck FromID(int id)
        {
            return new EcsMaskChunck(id >> DIV_SHIFT, 1 << (id & MOD_MASK));
        }

        /// <summary>Returns a string representation of the chunk (index, mask, bit count).</summary>
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
    /// <summary>Provides iteration over entities that match a given mask. Used internally by query executors.</summary>
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    public class EcsMaskIterator : IDisposable
    {
        public readonly EcsWorld World;
        public readonly EcsMask Mask;

        private readonly UnsafeArray<int> _sortIncBuffer;
        private readonly UnsafeArray<int> _sortExcBuffer;
        private readonly UnsafeArray<int> _sortAnyBuffer;

        private readonly UnsafeArray<EcsMaskChunck> _sortIncChunckBuffer;
        private readonly UnsafeArray<EcsMaskChunck> _sortExcChunckBuffer;
        private readonly UnsafeArray<EcsMaskChunck> _sortAnyChunckBuffer;

        private MemoryAllocator.Handler _bufferHandler;
        private MemoryAllocator.Handler _chunckBufferHandler;

        private readonly bool _isSingleIncPoolWithEntityStorage;
        private readonly bool _isHasAnyEntityStorage;
        private readonly EcsMaskFlags _maskFlags;

        /// <summary>Gets the flags of the associated mask.</summary>
        public EcsMaskFlags MaskFlags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _maskFlags; }
        }

        #region Constructors/Finalizator
        /// <summary>Initializes a new iterator for the specified world and mask.</summary>
        /// <param name="source">The world.</param>
        /// <param name="mask">The mask to use.</param>
        public unsafe EcsMaskIterator(EcsWorld source, EcsMask mask)
        {
            World = source;
            Mask = mask;
            _maskFlags = mask.Flags;

            int bufferLength = mask._incs.Length + mask._excs.Length + mask._anys.Length;
            int chunckBufferLength = mask._incChunckMasks.Length + mask._excChunckMasks.Length + mask._anyChunckMasks.Length;
            _bufferHandler = MemoryAllocator.AllocAndInit<int>(bufferLength);
            _chunckBufferHandler = MemoryAllocator.AllocAndInit<EcsMaskChunck>(chunckBufferLength);
            var sortBuffer = UnsafeArray<int>.Manual(_bufferHandler.As<int>(), bufferLength);
            var sortChunckBuffer = UnsafeArray<EcsMaskChunck>.Manual(_chunckBufferHandler.As<EcsMaskChunck>(), chunckBufferLength);

            _sortIncBuffer = sortBuffer.Slice(0, mask._incs.Length);
            _sortIncBuffer.CopyFromArray_Unchecked(mask._incs);
            _sortExcBuffer = sortBuffer.Slice(mask._incs.Length, mask._excs.Length);
            _sortExcBuffer.CopyFromArray_Unchecked(mask._excs);
            _sortAnyBuffer = sortBuffer.Slice(mask._incs.Length + mask._excs.Length, mask._anys.Length);
            _sortAnyBuffer.CopyFromArray_Unchecked(mask._anys);

            _sortIncChunckBuffer = sortChunckBuffer.Slice(0, mask._incChunckMasks.Length);
            _sortIncChunckBuffer.CopyFromArray_Unchecked(mask._incChunckMasks);
            _sortExcChunckBuffer = sortChunckBuffer.Slice(mask._incChunckMasks.Length, mask._excChunckMasks.Length);
            _sortExcChunckBuffer.CopyFromArray_Unchecked(mask._excChunckMasks);
            _sortAnyChunckBuffer = sortChunckBuffer.Slice(mask._incChunckMasks.Length + mask._excChunckMasks.Length, mask._anyChunckMasks.Length);
            _sortAnyChunckBuffer.CopyFromArray_Unchecked(mask._anyChunckMasks);

            _isHasAnyEntityStorage = false;
            var pools = source.AllPools;
            for (int i = 0; i < _sortIncBuffer.Length; i++)
            {
                var pool = pools[_sortIncBuffer.ptr[i]];
                _isHasAnyEntityStorage |= pool is IEntityStorage;
                if (_isHasAnyEntityStorage) { break; }
            }

            bool isSignleInc = Mask.Excs.Length <= 0 && Mask.Anys.Length <= 0 && Mask.Incs.Length == 1;
            _isSingleIncPoolWithEntityStorage = isSignleInc && _isHasAnyEntityStorage;
        }
        ~EcsMaskIterator()
        {
            Cleanup(false);
        }

        /// <summary>Releases all unmanaged resources used by the iterator.</summary>
        public void Dispose()
        {
            Cleanup(true);
            GC.SuppressFinalize(this);
        }
        private void Cleanup(bool disposing)
        {
            _bufferHandler.DisposeAndReset();
            _chunckBufferHandler.DisposeAndReset();
        }
        #endregion

        #region SortConstraints/TryFindEntityStorage
        private unsafe int SortConstraints_Internal()
        {
            UnsafeArray<int> sortIncBuffer = _sortIncBuffer;
            UnsafeArray<int> sortExcBuffer = _sortExcBuffer;
            UnsafeArray<int> sortAnyBuffer = _sortAnyBuffer;

            EcsWorld.PoolSlot[] counts = World._poolSlots;
            int maxBufferSize = Math.Max(Math.Max(sortIncBuffer.Length, sortExcBuffer.Length), sortAnyBuffer.Length);
            int maxEntites = int.MaxValue;

            EcsMaskChunck* preSortingBuffer;
            if (maxBufferSize < STACK_BUFFER_THRESHOLD)
            {
                EcsMaskChunck* ptr = stackalloc EcsMaskChunck[maxBufferSize];
                preSortingBuffer = ptr;
            }
            else
            {
                preSortingBuffer = TempBuffer<EcsMaskIterator, EcsMaskChunck>.Get(maxBufferSize);
            }

            if (_sortIncChunckBuffer.Length > 1)
            {
                SortHalper.Sort(sortIncBuffer.AsSpan(), new IncCountComparer(counts));
                ConvertToChuncks(preSortingBuffer, sortIncBuffer, _sortIncChunckBuffer);
            }
            if (_sortIncChunckBuffer.Length > 0)
            {
                maxEntites = counts[_sortIncBuffer.ptr[0]].count;
                if (maxEntites <= 0)
                {
                    return 0;
                }
            }

            if (_sortExcChunckBuffer.Length > 1)
            {
                SortHalper.Sort(sortExcBuffer.AsSpan(), new ExcCountComparer(counts));
                ConvertToChuncks(preSortingBuffer, sortExcBuffer, _sortExcChunckBuffer);
            }
            // Выражение IncCount < (AllEntitesCount - ExcCount) мало вероятно будет истинным.
            // ExcCount = максимальное количество ентитей с исключеющим ограничением и IncCount = минимальоне количество ентитей с включающим ограничением
            // Поэтому исключающее ограничение игнорируется для maxEntites.


            if (_sortAnyChunckBuffer.Length > 1)
            {
                SortHalper.Sort(sortAnyBuffer.AsSpan(), new ExcCountComparer(counts));
                ConvertToChuncks(preSortingBuffer, sortAnyBuffer, _sortAnyChunckBuffer);
            }
            // Any не влияет на maxEntites если есть Inc и сложно высчитывается если нет Inc

            return maxEntites;
        }
        private unsafe bool TryGetEntityStorage(out IEntityStorage storage, out IEcsPool pool)
        {
            if (_isHasAnyEntityStorage)
            {
                var pools = World.AllPools;
                for (int i = 0; i < _sortIncBuffer.Length; i++)
                {
                    pool = pools[_sortIncBuffer.ptr[i]];
                    storage = pool as IEntityStorage;
                    if (storage != null)
                    {
                        return true;
                    }
                }
            }
            pool = null;
            storage = null;
            return false;
        }
        #endregion

        #region IterateTo
        /// <summary>Fills the given group with all entities from the source span that match the mask.</summary>
        /// <param name="source">The span of entity IDs to filter.</param>
        /// <param name="group">The group to populate with matching entities.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CacheTo(EcsSpan source, EcsGroup group)
        {
            switch (_maskFlags)
            {
                case EcsMaskFlags.Empty:
                    group.CopyFrom(source);
                    break;
                case EcsMaskFlags.Inc:
                    IterateOnlyInc(source).CacheTo(group);
                    break;
                case EcsMaskFlags.Exc:
                case EcsMaskFlags.Any:
                case EcsMaskFlags.IncExc:
                case EcsMaskFlags.IncAny:
                case EcsMaskFlags.ExcAny:
                case EcsMaskFlags.IncExcAny:
                    Iterate(source).CacheTo(group);
                    break;
                case EcsMaskFlags.Broken:
                    group.Clear();
                    break;
                default:
                    Throw.UndefinedException();
                    return;
            }
        }

        /// <summary>Fills the given array with matching entity IDs from the source span.</summary>
        /// <param name="source">The span of entity IDs.</param>
        /// <param name="array">The array to write into (will be resized if needed).</param>
        /// <returns>The number of entities written.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CacheTo(EcsSpan source, ref int[] array)
        {
            switch (_maskFlags)
            {
                case EcsMaskFlags.Empty:
                    return source.ToArray(ref array);
                case EcsMaskFlags.Inc:
                    return IterateOnlyInc(source).CacheTo(ref array);
                case EcsMaskFlags.Exc:
                case EcsMaskFlags.Any:
                case EcsMaskFlags.IncExc:
                case EcsMaskFlags.IncAny:
                case EcsMaskFlags.ExcAny:
                case EcsMaskFlags.IncExcAny:
                    return Iterate(source).CacheTo(ref array);
                case EcsMaskFlags.Broken:
                    return new EcsSpan(World.ID, Array.Empty<int>()).ToArray(ref array);
                default:
                    Throw.UndefinedException();
                    return 0;
            }
        }
        #endregion

        #region Iterate/Enumerable
        /// <summary>Returns an enumerable for iterating over matching entities (supports all condition types).</summary>
        /// <param name="span">The source span of entity IDs.</param>
        /// <returns>An <see cref="Enumerable"/> struct that can be enumerated.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerable Iterate(EcsSpan span) { return new Enumerable(this, span); }

        /// <summary>Provides enumeration over entities matching the mask (all conditions).</summary>
#if ENABLE_IL2CPP
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
        public readonly ref struct Enumerable
        {
            private readonly EcsMaskIterator _iterator;
            private readonly EcsSpan _span;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerable(EcsMaskIterator iterator, EcsSpan span)
            {
                _iterator = iterator;
                _span = span;
            }

            #region CacheTo
            /// <summary>Copies all matching entities into the specified group.</summary>
            /// <param name="group">The group to fill.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CacheTo(EcsGroup group)
            {
                group.Clear();
                var enumerator = GetEnumerator();
                while (enumerator.MoveNext())
                {
                    group.AddUnchecked(enumerator.Current);
                }
            }

            /// <summary>Copies matching entity IDs into the provided array (resizes if needed).</summary>
            /// <param name="array">The array to fill.</param>
            /// <returns>The number of entities copied.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int CacheTo(ref int[] array)
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
            /// <summary>Converts the result to a <see cref="List{int}"/>.</summary>
            /// <returns>A new list containing all matching entity IDs.</returns>
            public List<int> ToList()
            {
                List<int> ints = new List<int>();
                foreach (var e in this) { ints.Add(e); }
                return ints;
            }

            /// <summary>Returns a string representation of the enumeration.</summary>
            public override string ToString() { return CollectionUtility.EntitiesToString(ToList(), "it"); }
            #endregion

            #region Enumerator
            public Enumerator GetEnumerator()
            {
                if (_iterator.Mask.IsBroken)
                {
                    return new Enumerator(_span.Slice(0, 0), _iterator);
                }
                int maxEntities = _iterator.SortConstraints_Internal();
                if (maxEntities <= 0)
                {
                    return new Enumerator(_span.Slice(0, 0), _iterator);
                }
                if (_span.IsSourceEntities && _iterator.TryGetEntityStorage(out IEntityStorage storage, out _))
                {
                    return new Enumerator(storage.ToSpan(), _iterator);
                }
                else
                {
                    return new Enumerator(_span, _iterator);
                }
            }

#if ENABLE_IL2CPP
            [Il2CppSetOption(Option.NullChecks, false)]
            [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
            public ref struct Enumerator
            {
                private ReadOnlySpan<int>.Enumerator _span;

                private readonly UnsafeArray<EcsMaskChunck> _sortIncChunckBuffer;
                private readonly UnsafeArray<EcsMaskChunck> _sortExcChunckBuffer;
                private readonly UnsafeArray<EcsMaskChunck> _sortAnyChunckBuffer;

                private readonly int[] _entityComponentMasks;
                private readonly int _entityComponentMaskLengthBitShift;

                public Enumerator(EcsSpan span, EcsMaskIterator iterator)
                {
                    _sortIncChunckBuffer = iterator._sortIncChunckBuffer;
                    _sortExcChunckBuffer = iterator._sortExcChunckBuffer;
                    _sortAnyChunckBuffer = iterator._sortAnyChunckBuffer;

                    _entityComponentMasks = iterator.World._entityComponentMasks;
                    _entityComponentMaskLengthBitShift = iterator.World._entityComponentMaskLengthBitShift;

                    _span = span.GetEnumerator();
                }
                public int Current
                {
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    get { return _span.Current; }
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public unsafe bool MoveNext()
                {
                    while (_span.MoveNext())
                    {
                        int entityLineStartIndex = _span.Current << _entityComponentMaskLengthBitShift;
                        for (int i = 0; i < _sortIncChunckBuffer.Length; i++)
                        {
                            var bit = _sortIncChunckBuffer.ptr[i];
                            if ((_entityComponentMasks[entityLineStartIndex + bit.chunkIndex] & bit.mask) != bit.mask)
                            {
                                goto skip;
                            }
                        }
                        for (int i = 0; i < _sortExcChunckBuffer.Length; i++)
                        {
                            var bit = _sortExcChunckBuffer.ptr[i];
                            if ((_entityComponentMasks[entityLineStartIndex + bit.chunkIndex] & bit.mask) != 0)
                            {
                                goto skip;
                            }
                        }

                        if (_sortAnyChunckBuffer.Length != 0)
                        {
                            for (int i = 0; i < _sortAnyChunckBuffer.Length; i++)
                            {
                                var bit = _sortAnyChunckBuffer.ptr[i];
                                if ((_entityComponentMasks[entityLineStartIndex + bit.chunkIndex] & bit.mask) == bit.mask)
                                {
                                    return true;
                                }
                            }
                            goto skip;
                        }

                        return true;
                    skip: continue;
                    }
                    return false; //exit
                }
            }
            #endregion
        }
        #endregion

        #region Iterate/Enumerable OnlyInc
        /// <summary>Returns an enumerable optimized for masks that only have include conditions (no exclusions or any).</summary>
        /// <param name="span">The source span.</param>
        /// <returns>An <see cref="OnlyIncEnumerable"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OnlyIncEnumerable IterateOnlyInc(EcsSpan span) { return new OnlyIncEnumerable(this, span); }


        /// <summary>Optimized enumeration for masks that have only include conditions.</summary>
#if ENABLE_IL2CPP
        [Il2CppSetOption(Option.NullChecks, false)]
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

            #region CacheTo
            /// <summary>Copies all matching entities into the specified group.</summary>
            /// <param name="group">The group to fill.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CacheTo(EcsGroup group)
            {
                group.Clear();
                var enumerator = GetEnumerator();
                while (enumerator.MoveNext())
                {
                    group.AddUnchecked(enumerator.Current);
                }
            }

            /// <summary>Copies matching entity IDs into the provided array (resizes if needed).</summary>
            /// <param name="array">The array to fill.</param>
            /// <returns>The number of entities copied.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int CacheTo(ref int[] array)
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
            /// <summary>Converts the result to a <see cref="List{int}"/>.</summary>
            public List<int> ToList()
            {
                List<int> ints = new List<int>();
                foreach (var e in this) { ints.Add(e); }
                return ints;
            }

            /// <summary>Returns a string representation of the enumeration.</summary>
            public override string ToString() { return CollectionUtility.EntitiesToString(ToList(), "inc_it"); }
            #endregion

            #region Enumerator
            public Enumerator GetEnumerator()
            {
                if (_iterator.Mask.IsBroken)
                {
                    return new Enumerator(_span.Slice(0, 0), _iterator);
                }
                int maxEntities = _iterator.SortConstraints_Internal();
                if (maxEntities <= 0)
                {
                    return new Enumerator(_span.Slice(0, 0), _iterator);
                }

                if (_span.IsSourceEntities && _iterator.TryGetEntityStorage(out IEntityStorage storage, out IEcsPool rawPool))
                {
                    var span = storage.ToSpan();
                    return new Enumerator(span, _iterator, _iterator._isSingleIncPoolWithEntityStorage && span.Count == rawPool.Count);
                }
                else
                {
                    return new Enumerator(_span, _iterator);
                }
            }

#if ENABLE_IL2CPP
            [Il2CppSetOption(Option.NullChecks, false)]
            [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
            public unsafe ref struct Enumerator
            {
                private ReadOnlySpan<int>.Enumerator _span;

                private readonly UnsafeArray<EcsMaskChunck> _sortIncChunckBuffer;

                private readonly int[] _entityComponentMasks;
                private readonly int _entityComponentMaskLengthBitShift;

                public Enumerator(EcsSpan span, EcsMaskIterator iterator, bool isPureIteration = false)
                {
                    if (isPureIteration)
                    {
                        _sortIncChunckBuffer = UnsafeArray<EcsMaskChunck>.Empty;
                    }
                    else
                    {
                        _sortIncChunckBuffer = iterator._sortIncChunckBuffer;
                    }

                    _entityComponentMasks = iterator.World._entityComponentMasks;
                    _entityComponentMaskLengthBitShift = iterator.World._entityComponentMaskLengthBitShift;

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
                        int entityLineStartIndex = _span.Current << _entityComponentMaskLengthBitShift;
                        for (int i = 0; i < _sortIncChunckBuffer.Length; i++)
                        {
                            var bit = _sortIncChunckBuffer.ptr[i];
                            if ((_entityComponentMasks[entityLineStartIndex + bit.chunkIndex] & bit.mask) != bit.mask)
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

#region Utils
namespace DCFApixels.DragonECS.Core.Internal
{
    #region EcsMaskIteratorUtility
#if ENABLE_IL2CPP
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
    internal unsafe class EcsMaskIteratorUtility
    {
        internal const int STACK_BUFFER_THRESHOLD = 100;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ConvertToChuncks(EcsMaskChunck* bufferPtr, UnsafeArray<int> input, UnsafeArray<EcsMaskChunck> output)
        {
            for (int i = 0; i < input.Length; i++)
            {
                bufferPtr[i] = EcsMaskChunck.FromID(input.ptr[i]);
            }

            for (int inputI = 0, outputI = 0; outputI < output.Length; inputI++, bufferPtr++)
            {
                int stackingMask = bufferPtr->mask;
                if (stackingMask == 0) { continue; }
                int stackingChunkIndex = bufferPtr->chunkIndex;

                EcsMaskChunck* bufferSpanPtr = bufferPtr + 1;
                for (int j = 1; j < input.Length - inputI; j++, bufferSpanPtr++)
                {
                    if (bufferSpanPtr->chunkIndex == stackingChunkIndex)
                    {
                        stackingMask |= bufferSpanPtr->mask;
                        *bufferSpanPtr = default;
                    }
                }

                output.ptr[outputI] = new EcsMaskChunck(stackingChunkIndex, stackingMask);
                outputI++;
            }
        }

#if ENABLE_IL2CPP
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
        internal readonly struct IncCountComparer : IComparer<int>
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
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
#endif
        internal readonly struct ExcCountComparer : IComparer<int>
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
#endregion