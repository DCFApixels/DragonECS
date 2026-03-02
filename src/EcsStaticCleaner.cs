using DCFApixels.DragonECS.Core.Internal;

namespace DCFApixels.DragonECS
{
    public static class EcsStaticCleaner
    {
        public static void ResetAll()
        {
            TypeMeta.Clear();
            Injector.InjectionList.Clear();
            // MemoryAllocator.Clear();
            EcsTypeCodeManager.Clear();
            ConfigContainer.Clear();
            EcsAspect.Clear();
            EcsWorld.Clear();
        }
    }
}