namespace Vakuu.Engine
{
    public struct Health
    {
        public Health()
        {
        }

        public Health(ushort maxHP)
        {
            Max = maxHP;
            Current = maxHP;
        }

        public ushort Max { get; set; }

        public ushort Current { get; set; }
    }
}
