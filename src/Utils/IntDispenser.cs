﻿using System.Collections.Concurrent;
using System.Threading;

namespace DCFApixels.DragonECS.Utils
{
    internal sealed class IntDispenser
    {
        private readonly ConcurrentQueue<int> _freeInts;
        private int _increment;

        #region Properties
        public int LastInt => _increment;
        public int FreeConut => _freeInts.Count;
        #endregion

        #region Constructor
        public IntDispenser()
        {
            _freeInts = new ConcurrentQueue<int>();
            _increment = 0;
        }
        public IntDispenser(int startIncrement)
        {
            _freeInts = new ConcurrentQueue<int>();
            _increment = startIncrement;
        }
        #endregion

        #region GetFree/Release
        public int GetFree()
        {
            if (!_freeInts.TryDequeue(out int result))
            {
                result = Interlocked.Increment(ref _increment);
            }
            return result;
        }

        public void Release(int released)
        {
            _freeInts.Enqueue(released);
        }
        #endregion
    }
}
