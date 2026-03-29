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

        protected override void BuildActionImpl(IReadOnlyCollection<Enemy> targets, IActionBuilder builder, bool upgraded)
            => builder.Reduce(
                new Reducer(
                    input => input + (upgraded ? 9 : 6),
                    Variables.PlayerAttackDamage));
    }
}

