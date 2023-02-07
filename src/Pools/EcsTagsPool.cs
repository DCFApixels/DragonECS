using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class EcsTagsPool : IEcsPool
    {
        private int _id;
        private readonly EcsWorld _source;
        private readonly EcsType _type;
        private readonly SparseSet _sparseSet;

        #region Properites
        public EcsWorld World => _source;
        public int ID => _id;
        public EcsType Type => _type;
        #endregion

        #region Constructors
        public EcsTagsPool(EcsWorld source, EcsType type, int capacity)
        {
            _source = source;
            _type = type;
            _sparseSet = new SparseSet(capacity);
        }
        #endregion

        #region Add/Has/Del
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int index)
        {
            _sparseSet.Add(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Del(int index)
        {
            _sparseSet.Add(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(int index)
        {
            return _sparseSet.Contains(index);
        }
        #endregion
    }
}
