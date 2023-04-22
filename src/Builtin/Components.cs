using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    public struct Parent : IEcsAttachComponent
    {
        public EcsEntity entity;

        public EcsEntity Target
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => entity;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => entity = value;
        }
    }
}
