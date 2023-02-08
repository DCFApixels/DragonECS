using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class EcsWorldMap
    {
        private EcsWorld[] _worlds = new EcsWorld[8];
        private SparseSet _sparceSet = new SparseSet(8);

        public EcsWorld this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _worlds[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InsertWorld(EcsWorld world)
        {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveWorld(EcsWorld world)
        {

        }
    }
}
