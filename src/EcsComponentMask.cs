using System;

namespace DCFApixels.DragonECS
{
    public class EcsComponentMask
    {
        internal Type WorldArchetype;
        internal int[] Inc;
        internal int[] Exc;
        public override string ToString()
        {
            return $"Inc({string.Join(", ", Inc)}) Exc({string.Join(", ", Exc)})";
        }
    }

}
