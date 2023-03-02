using System;

namespace DCFApixels.DragonECS
{
    public class BitMask
    {
        private int[] data;

        public int Length { get; private set; }

        public BitMask(int length)
        {
            Length = length;
            data = new int[(length + 31) / 32];
        }

        public void Resize(int newLength)
        {
            Length = newLength;
            Array.Resize(ref data, (newLength + 31) / 32);
        }

        public void Set1(int index)
        {
            data[index / 32] |= 1 << (index % 32);
        }

        public void Set0(int index)
        {
            data[index / 32] &= ~(1 << (index % 32));
        }

        public bool Get(int index)
        {
            return (data[index / 32] & (1 << (index % 32))) != 0;
        }
    }
}