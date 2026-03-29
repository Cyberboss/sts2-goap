using System.Collections.Generic;

namespace Vakuu.Engine.Cards
{
    public abstract class Defend<TDefend> : CardArchetype<TDefend>
        where TDefend : Defend<TDefend>, new()
    {
        public override string Name => "Defend";

        public override CardType Type => CardType.Attack;

        public override IEnumerable<IReadOnlyCollection<IReadOnlyCollection<Enemy>>> SelectTargetPermutations(IEnumerable<Enemy> potentialTargets, bool upgraded)
            => SelfTargeted;

        protected override void BuildActionImpl(
            IReadOnlyCollection<Enemy> targets,
            IActionBuilder builder,
            bool upgraded)
            => builder.Reduce(
                new Reducer(
                    input => input + (upgraded ? 8 : 5),
                    Variables.PlayerBlockGain));
    }
}
