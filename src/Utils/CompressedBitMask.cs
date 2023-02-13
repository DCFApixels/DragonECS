using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCFApixels.DragonECS
{
    public class CompressedBitMask
    {
        private const int CHUNK = 32; //int bits

        private int[] _fullChunkIndexes; // индексы чанков с полным заполнением
        private int[] _sparseIndexes;
        private int[] _denseMasks;

        private int _highBit = 0;

        public int HightBit => _highBit;

        public void Set(int[] indexes, int count)
        {

        }

        public ref struct EqualsRequest
        {
            private CompressedBitMask _source;
            private CompressedBitMask _other;
            public void GetEnumerator() => 
        }
        public ref struct Enumerator
        {
            private readonly int[] _indexes;
            private readonly int[] _masks;
            private int _index;

            public Enumerator(int[] indexes, int[] masks)

            {
                _indexes = indexes;
                _masks = masks;
                _index = -1;
            }

            public int Current
            {
                get => 0;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
            }

            public void Reset()
            {
                _index = -1;
            }
        }
    }
}
