using System;

namespace DCFApixels.DragonECS.Core.Internal
{
    internal static class AllocatorUtility
    {
        public static unsafe void ClearAllocatedMemory(IntPtr ptr, int startByte, int lengthInBytes)
        {
            ClearAllocatedMemory((byte*)ptr, startByte, lengthInBytes);
        }
        public static unsafe void ClearAllocatedMemory(byte* ptr, int startByte, int lengthInBytes)
        {
#if ENABLE_DUMMY_SPAN
            lengthInBytes += startByte;
            for (int i = startByte; i < lengthInBytes; i++)
            {
                ptr[i] = 0;
            }
#else
            Span<byte> memorySpan = new Span<byte>(ptr + startByte, lengthInBytes);
            memorySpan.Clear();
#endif
        }
    }
}
