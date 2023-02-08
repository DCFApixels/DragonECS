using System;

namespace DCFApixels.Assets.DragonECS.src.React
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class WorldFilterAttribute : Attribute //TODO
    {
        public readonly string[] worlds;

        public WorldFilterAttribute(params string[] worlds)
        {
            this.worlds = worlds;
        }
    }
}
