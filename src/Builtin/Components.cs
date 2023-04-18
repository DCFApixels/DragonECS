using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DCFApixels.DragonECS
{
    /// <summary>
    /// Используется для реализации отношений. traget - это сущьность к которой крепится эта сущьность. other - это сущьность с которой traget образует связь
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Pack = 8, Size = 16)]
    public readonly struct Edge
    {
        [FieldOffset(0), MarshalAs(UnmanagedType.U8)]
        public readonly EcsEntity origin;
        [FieldOffset(1), MarshalAs(UnmanagedType.U8)]
        public readonly EcsEntity other;

        /// <summary>alias for "origin"</summary>
        public EcsEntity left
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => origin;
        }
        /// <summary>alias for "other"</summary>
        public EcsEntity right
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => other;
        }
    }
}
