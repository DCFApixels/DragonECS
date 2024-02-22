using System;

namespace DCFApixels.DragonECS.DI.Internal
{
    internal class Throw
    {
        public static void ArgumentNull()
        {
            throw new ArgumentNullException();
        }
    }
}
