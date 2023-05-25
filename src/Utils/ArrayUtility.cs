namespace DCFApixels.DragonECS.Utils
{
    internal static class ArrayUtility
    {
        public static void Fill<T>(T[] array, T value, int startIndex = 0, int length = -1)
        {
            if (length < 0)
                length = array.Length;
            else
                length = startIndex + length;
            for (int i = startIndex; i < length; i++)
                array[i] = value;
        }
    }
}
