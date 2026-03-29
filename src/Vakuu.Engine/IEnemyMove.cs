namespace Vakuu.Engine
{
    public interface IEnemyMove
    {
        public string Name { get; }
        public EnemyMoveType Type { get; }

        bool Apply(IActionBuilder actionBuilder, Enemy enemy, Ascension ascension);
    }
}
