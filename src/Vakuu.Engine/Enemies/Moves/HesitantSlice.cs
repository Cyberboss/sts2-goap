namespace Vakuu.Engine.Enemies.Moves
{
    internal sealed class HesitantSlice : IEnemyMove
    {
        public string Name => "Hesitant Slice";

        public EnemyMoveType Type => EnemyMoveType.Attack | EnemyMoveType.Block;

        public bool Apply(IActionBuilder actionBuilder, Enemy enemy, Ascension ascension)
        {
            actionBuilder.Reduce(
                new Reducer(
                    input => input + 5,
                    enemy.BlockGainVariable));
            actionBuilder.Reduce(
                new Reducer(
                    _ => 6,
                    enemy.AttackAmountVariable));
            actionBuilder.Reduce(
                new Reducer(
                    _ => 1,
                    enemy.AttackCountState));
            return true;
        }
    }
}
