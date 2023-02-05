using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public struct EcsField<T>
    {
        private EcsFieldPool<T> _pool;
        public ref T this[int index]
        {
            get => ref _pool[index];
        }
    }
}
