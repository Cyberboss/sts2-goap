using Vakuu.Engine.Statuses;

namespace Vakuu.Engine.Enemies.Moves
{
    internal sealed class Hiss : IEnemyMove
    {
        public string Name => "Hiss";

        public EnemyMoveType Type => EnemyMoveType.Buff;

        public bool Apply(IActionBuilder actionBuilder, Enemy enemy, Ascension ascension)
        {
            actionBuilder.Reduce(
                new Reducer(
                    input => input + 2,
                    enemy.StatusState<Strength>()));
            return false;
        }
    }
}
