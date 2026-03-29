namespace Vakuu.Engine.Enemies.Moves
{
    internal sealed class Butt : IEnemyMove
    {
        public string Name => "Butt";

        public EnemyMoveType Type => EnemyMoveType.Attack;

        public bool Apply(IActionBuilder actionBuilder, Enemy enemy, Ascension ascension)
        {
            actionBuilder.Reduce(
                new Reducer(
                    _ => ascension >= Ascension.DeadlyEnemies
                        ? 13
                        : 12,
                    enemy.AttackAmountState));
            return true;
        }
    }
}
