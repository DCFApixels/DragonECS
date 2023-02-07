using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class EcsEntityTableManager
    {
        private int _count;
        private IEcsPool[] _fieldPools;

        private int _idIncrement;
        private Dictionary<IDKey, int> _ids;

        public EcsEntityTableManager(int capacity)
        {
            _fieldPools = new IEcsPool[capacity];
            _ids = new Dictionary<IDKey, int>(capacity);
            _count = 0;
        }

        public EcsPool<T> GetFieldPool<T>(int id)
        {
            if(id < _count)
                return (EcsPool<T>)_fieldPools[id];
        
            _count++;
            if(_fieldPools.Length < _count)
            {
                Array.Resize(ref _fieldPools, _fieldPools.Length << 1);
            }
            EcsPool<T> newPool = null;// new EcsFieldPool<T>(7);
            _fieldPools[id] = newPool;
            return newPool;
        }

        public void ResizeFieldPool(int id)
        {
        }


        public int GetFieldID(string name, int index)
        {
            IDKey key = new IDKey(name, index);
            if (_ids.TryGetValue(key, out int id))
                return id;

            id = _idIncrement++;
            _ids.Add(key, id);
            return id;
        }

        private struct IDKey
        {
            public string name;
            public int index;

            public IDKey(string name, int index)
            {
                this.name = name;
                this.index = index;
            }

            public override bool Equals(object obj)
            {
                return obj is IDKey key &&
                       name == key.name &&
                       index == key.index;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(name, index);
            }

            public override string ToString()
            {
                return name + "_" + index;
            }
        }
    }
}
