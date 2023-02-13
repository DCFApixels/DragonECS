namespace DCFApixels.DragonECS
{
    public interface IEcsComponentReset<T>
    {
        public void Reset(ref T component);
    }
}
