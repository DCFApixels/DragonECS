namespace DCFApixels.DragonECS
{
    public class ComponentTypeID
    {
        protected static int _incerement = 0;
    }
    public class TypeID<T> : ComponentTypeID
        where T : struct
    {
        public static readonly int id = _incerement++;
    }
}