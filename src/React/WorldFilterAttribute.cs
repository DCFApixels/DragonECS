using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.Assets.DragonECS.src.React
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    sealed class WorldFilterAttribute : Attribute
    {
        public readonly string[] worlds;

        public WorldFilterAttribute(params string[] worlds)
        {
            this.worlds = worlds;
        }
    }
}
