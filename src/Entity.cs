using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    [StructLayout(LayoutKind.Sequential, Pack = 0, Size = 8)]
    public readonly struct ent : IEquatable<long>, IEquatable<ent>
    {
        public static readonly ent NULL = default;

        // id - 32 bits
        // gen - 16 bits
        // world - 16 bits
        public readonly long _full;

        #region Properties
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int id
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(_full >> 32);
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ushort gen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ushort)((_full << 32) >> 48);

        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public short world
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (short)((_full << 48) >> 48);

        }
        #endregion

        #region Constructors
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ent(int id, short gen, short world)
        {
            _full = ((long)id) << 32;
            _full += ((long)gen) << 16;
            _full += world;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ent(long value)
        {
            _full = value;
        }
        #endregion

        #region GetHashCode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return unchecked((int)(_full)) ^ (int)(_full >> 32);
        }
        #endregion

        #region Equals
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(in ent other)
        {
            return _full == other._full;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is ent other && Equals(in other);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ent other)
        {
            return _full == other._full;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(long other)
        {
            return _full == other;
        }
        #endregion

        #region operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in ent left, in ent right) => left.Equals(in right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in ent left, in ent right) => !left.Equals(in right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(in ent eent) => eent._full;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(in ent eent) => eent.id;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ent(in long value) => new ent(value);
        #endregion
    }

    public static partial class entExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull(this in ent self)
        {
            return self == ent.NULL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T Read<T>(this in ent self)
            where T : struct
        {
            return ref EcsWorld.Worlds[self.world].GetPool<T>().Read(self.id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Write<T>(this in ent self)
            where T : struct
        {
            return ref EcsWorld.Worlds[self.world].GetPool<T>().Write(self.id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has<T>(this in ent self)
            where T : struct
        {
            return EcsWorld.Worlds[self.world].GetPool<T>().Has(self.id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Del<T>(this in ent self)
            where T : struct
        {
            EcsWorld.Worlds[self.world].GetPool<T>().Del(self.id);
        }
    }

    public struct Entity
    {
        public IEcsWorld world;
        public int id;

        public Entity(IEcsWorld world, int id)
        {
            this.world = world;
            this.id = id;
        }
    }

    public static partial class EntityExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNull(this in Entity self)
        {
            return self.world == null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T Read<T>(this in Entity self)
            where T : struct
        {
            return ref self.world.GetPool<T>().Read(self.id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Write<T>(this in Entity self)
            where T : struct
        {
            return ref self.world.GetPool<T>().Write(self.id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Has<T>(this in Entity self)
            where T : struct
        {
            return self.world.GetPool<T>().Has(self.id);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Del<T>(this in Entity self)
            where T : struct
        {
            self.world.GetPool<T>().Del(self.id);
        }
    }
}
