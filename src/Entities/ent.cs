using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace DCFApixels.DragonECS
{
#pragma warning disable CS0660, CS0661, IDE1006
    /// <summary>Weak identifier/Single frame entity identifier</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 4)]
    public readonly ref struct ent
    {
        [MarshalAs(UnmanagedType.I4)]
        internal readonly int id;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ent(int id) => this.id = id;

        public static implicit operator int(ent entityID) => entityID.id;

        public static bool operator ==(ent a, ent b) => a.id == b.id;
        public static bool operator !=(ent a, ent b) => a.id != b.id;

        public static bool operator ==(ent a, Null? _) => a.id == 0;
        public static bool operator ==(Null? _, ent b) => b.id == 0;
        public static bool operator !=(ent a, Null? _) => a.id != 0;
        public static bool operator !=(Null? _, ent b) => b.id != 0;

        public struct Null { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EcsEntity ToStrongID(IEcsWorld world) => world.GetEcsEntity(id);
    }
}
