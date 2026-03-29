using System.Collections.Generic;

namespace Vakuu.Engine.Cards
{
    public abstract class Strike<TStrike> : CardArchetype<TStrike>
        where TStrike : Strike<TStrike>, new()
    {
        public override string Name => "Strike";

        public override CardType Type => CardType.Attack;

        public override IEnumerable<IReadOnlyCollection<IReadOnlyCollection<Enemy>>> SelectTargetPermutations(IEnumerable<Enemy> potentialTargets, bool upgraded)
            => SingleTargeted(potentialTargets);

        public override void BuildAction(IReadOnlyCollection<Enemy> targets, IActionBuilder builder, bool upgraded)
        {
        }
    }
}

