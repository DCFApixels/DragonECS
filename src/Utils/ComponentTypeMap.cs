using System;
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS
{
    internal abstract class ComponentType
    {
        internal static int increment = 1;
        internal static int capacity = 512;
    }
    internal sealed class ComponentType<T> : ComponentType
    {
        internal static int globalID;

        static ComponentType()
        {
            globalID = increment++;
            if (increment > capacity)
            {
                capacity <<= 1;
            }
        }
    }

    public class ComponentTypeMap
    {
        private int[] _dense;
        private int[] _sparse;

        private int _count;

        #region Properties
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _count;
        }
        #endregion

        #region Constrcutors
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ComponentTypeMap(int denseCapacity = 64)
        {
            _dense = new int[denseCapacity];
            _sparse = new int[ComponentType.capacity];

            _count = 0;
        }
        #endregion

        #region Contains
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains<T>() => Contains(ComponentType<T>.globalID);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Contains(int globalID)
        {
            return globalID > 0 && globalID < _sparse.Length && _sparse[globalID] > 0;
        }
        #endregion

        #region GetID
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetID<T>()
        {
            int globalID = ComponentType<T>.globalID;

            if (!Contains(globalID))
            {
                Add(globalID);
            }

            return _dense[globalID];
        }
        #endregion

        #region Add
        private void Add(int entityID)
        {
            if (Contains(entityID))
                return;

            if (++_count >= _dense.Length)
                Array.Resize(ref _dense, _dense.Length << 1);

            if (entityID > _sparse.Length)
            {
                int neadedSpace = _sparse.Length;
                while (entityID >= neadedSpace)
                    neadedSpace <<= 1;
                Array.Resize(ref _sparse, neadedSpace);
            }

            _dense[_count] = entityID;
            _sparse[entityID] = _count;
        }
        #endregion
    }
}
