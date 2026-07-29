#if DISABLE_DEBUG
#undef DEBUG
#endif
using System.Runtime.CompilerServices;

namespace DCFApixels.DragonECS.Core.Internal
{
    internal unsafe static class BitsUtility
    {
        private const char DEFAULT_SEPARATOR = '_';
        private const int BYTE_BITS = 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountBits(int bits)
        {
            return CountBits((uint)bits);
        }
        public static int CountBits(uint bits)
        {
            bits = bits - ((bits >> 1) & 0x55555555);
            bits = (bits & 0x33333333) + ((bits >> 2) & 0x33333333);
            return (int)(((bits + (bits >> 4) & 0x0F0F0F0F) * 0x01010101) >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHighBitNumber(int bits)
        {
            return GetHighBitNumber((uint)bits);
        }
        public static int GetHighBitNumber(uint bits)
        {
            if (bits == 0)
            {
                return -1;
            }
            int bit = 0;
            if ((bits & 0xFFFF0000) != 0)
            {
                bits >>= 16;
                bit |= 16;
            }
            if ((bits & 0xFF00) != 0)
            {
                bits >>= 8;
                bit |= 8;
            }
            if ((bits & 0xF0) != 0)
            {
                bits >>= 4;
                bit |= 4;
            }
            if ((bits & 0xC) != 0)
            {
                bits >>= 2;
                bit |= 2;
            }
            if ((bits & 0x2) != 0)
            {
                bit |= 1;
            }
            return bit;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBitNumbersNoAlloc(int bits, ref int[] numbers)
        {
            return GetBitNumbersNoAlloc((uint)bits, ref numbers);
        }
        public static int GetBitNumbersNoAlloc(uint bits, ref int[] numbers)
        {
            int iMax = CountBits(bits);
            if (iMax >= numbers.Length)
            {
                System.Array.Resize(ref numbers, iMax);
            }
            for (int i = 0; i < iMax; i++)
            {
                int number = GetHighBitNumber(bits);
                numbers[i] = number;
                bits ^= 1u << number;
            }
            return iMax;
        }

        public static string ToBitsString<T>(T value, char separator = DEFAULT_SEPARATOR, int separateRange = BYTE_BITS) where T : unmanaged
        {
            int size = sizeof(T);
            int length = size * BYTE_BITS;
            byte* bytes = (byte*)&value;
            char* chars = stackalloc char[length + (separateRange > 0 ? length / separateRange : 0)];

            int writeIndex = 0;
            for (int i = length - 1; i >= 0; i--)
            {
                int bitIndex = length - i - 1;
                chars[writeIndex++] = (bytes[i / BYTE_BITS] & 1 << (i % BYTE_BITS)) > 0 ? '1' : '0';
                if (separateRange > 0 && (bitIndex + 1) % separateRange == 0)
                {
                    chars[writeIndex++] = separator;
                }
            }
            return new string(chars, 0, writeIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint NextXorShiftState(uint state)
        {
            unchecked
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }
        }
    }
}
