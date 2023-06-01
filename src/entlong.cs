#pragma warning disable IDE1006
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static DCFApixels.DragonECS.entlong.ThrowHalper;

namespace DCFApixels.DragonECS
{
    // [        id 32        |  gen 16  | world 16 ]
    /// <summary>Strong identifier/Permanent entity identifier</summary>
    [StructLayout(LayoutKind.Explicit, Pack = 2, Size = 8)]
    [DebuggerTypeProxy(typeof(DebuggerProxy))]
    public readonly struct entlong : IEquatable<long>, IEquatable<entlong>
    {
        public static readonly entlong NULL = default;
        [FieldOffset(0)]
        internal readonly long full; //Union
        [FieldOffset(0)]
        internal readonly int id;
        [FieldOffset(4)]
        internal readonly short gen;
        [FieldOffset(6)]
        internal readonly short world;

        #region Properties
        public bool IsAlive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => EcsWorld.Worlds[world].IsAlive(id, gen);
        }
        public bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this == NULL;
        }
        public int ID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (!IsAlive) ThrowIsNotAlive(this);
#endif
                return id;
            }
        }
        public short Gen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (!IsAlive) ThrowIsNotAlive(this);
#endif
                return gen;
            }
        }
        public EcsWorld World
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (!IsAlive) ThrowIsNotAlive(this);
#endif
                return EcsWorld.Worlds[world];
            }
        }
        public short WorldID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if (DEBUG && !DISABLE_DEBUG) || ENABLE_DRAGONECS_ASSERT_CHEKS
                if (!IsAlive) ThrowIsNotAlive(this);
#endif
                return world;
            }
        }
        #endregion

        #region Constructors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public entlong(int id, short gen, short world) : this()
        {
            this.id = id;
            this.gen = gen;
            this.world = world;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal entlong(long full) : this()
        {
            this.full = full;
        }
        #endregion

        #region TryGetters
        public bool TryGetID(out int id)
        {
            id = this.id;
            return IsAlive;
        }
        public bool TryGetWorld(out EcsWorld world)
        {
            world = EcsWorld.Worlds[this.world];
            return IsAlive;
        }
        public bool TryUnpack(out EcsWorld world, out int id)
        {
            world = EcsWorld.Worlds[this.world];
            id = this.id;
            return IsAlive;
        }
        #endregion

        #region Equals
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(entlong other) => full == other.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(long other) => full == other;
        #endregion

        #region Object
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => unchecked((int)full) ^ (int)(full >> 32);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => $"entity(id:{id} g:{gen} w:{world} {(IsNull ? "null" : IsAlive ? "alive" : "not alive")})";
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) => obj is entlong other && full == other.full;
        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in entlong a, in entlong b) => a.full == b.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in entlong a, in entlong b) => a.full != b.full;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator long(in entlong a) => a.full;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator entlong(in long a) => new entlong(a);
        #endregion

        #region ThrowHalper
        internal static class ThrowHalper
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void ThrowIsNotAlive(entlong entity)
            {
                if (entity.IsNull)
                    throw new EcsFrameworkException($"The {entity} is null.");
                else
                    throw new EcsFrameworkException($"The {entity} is not alive.");
            }
        }
        #endregion

        #region DebuggerProxy
        internal class DebuggerProxy
        {
            private List<object> _componentsList;
            private entlong _value;
            public long full => _value.full;
            public int id => _value.id;
            public int gen => _value.gen;
            public int world => _value.world;
            public EntState State => _value.IsNull ? EntState.Null : _value.IsAlive ? EntState.Alive : EntState.Dead;
            public EcsWorld EcsWorld => _value.World;
            public IEnumerable<object> components
            {
                get
                {
                    _value.World.GetComponents(_value.ID, _componentsList);
                    return _componentsList;
                }
            }
            public DebuggerProxy(entlong value)
            {
                _value = value;
                _componentsList = new List<object>();
            }
            public enum EntState { Null, Dead, Alive, }
        }
        #endregion
    }
}
