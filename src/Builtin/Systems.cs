namespace DCFApixels.DragonECS
{
    [DebugColor(DebugColor.Black)]
    public class SystemsBlockMarkerSystem : IEcsSystem
    {
        public readonly string name;

        public SystemsBlockMarkerSystem(string name)
        {
            this.name = name;
        }
    }
}
