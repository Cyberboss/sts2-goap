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
                    enemy.AttackAmountVariable,
                    "Apply Butt Damage"));
            actionBuilder.Reduce(
                new Reducer(
                    _ => 1,
                    enemy.AttackCountState,
                    "Apply Butt Attack Count"));
            return true;
        }
    }
}
