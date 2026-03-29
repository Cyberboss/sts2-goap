using Vakuu.Engine.Statuses;

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
                    enemy.StatusState<Block>()));
            actionBuilder.Reduce(
                new Reducer(
                    _ => 6,
                    enemy.AttackAmountState));
            return true;
        }
    }
}
