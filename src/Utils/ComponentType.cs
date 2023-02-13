using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    internal abstract class ComponentType
    {
        protected static int _increment = 0; 
    }
    internal sealed class ComponentType<T> : ComponentType
    {
        internal static int uniqueID = _increment++;
    }
}
