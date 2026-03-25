namespace SlayTheSpire2.Goap.Statuses
{
    internal sealed class Strength : Status<Strength>
    {
        public Strength(short amount)
            : base(amount)
        {
        }

        public override string Name => "Strength";
    }
}
