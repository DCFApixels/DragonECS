namespace DCFApixels.DragonECS
{
    public readonly ref struct TableBuilder
    {
        private readonly EcsWorld _world;
        private readonly EcsWorld.Mask _mask;

        public TableBuilder(EcsWorld world, EcsWorld.Mask mask)
        {
            _world = world;
            _mask = mask;
        }

        public EcsPool<T> Cache<T>(mem<T> member)
            where T : struct
        {
            return _world.GetPool(member);
        }
        public EcsPool<T> Inc<T>(mem<T> member)
            where T : struct
        {
            _mask.Inc(member);
            return _world.GetPool(member);
        }
        public EcsPool<T> Exc<T>(mem<T> member)
            where T : struct
        {
            _mask.Exc(member);
            return _world.GetPool(member);
        }
    }
}
