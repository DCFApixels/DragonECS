namespace DCFApixels.DragonECS
{
    public readonly struct EcsMaskBit
    {
        public readonly int chankIndex;
        public readonly int mask;
        public EcsMaskBit(int chankIndex, int mask)
        {
            this.chankIndex = chankIndex;
            this.mask = mask;
        }
        public static EcsMaskBit FromPoolID(int id)
        {
            return new EcsMaskBit(id / 32, 1 << (id % 32));
        }

        public override string ToString()
        {
            return $"bit({chankIndex}, {mask})";
        }
    }
}
