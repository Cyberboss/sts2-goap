using System.Collections.Generic;
using System.Linq;

using Vakuu.Engine.Statuses;

namespace Vakuu.Engine.Cards.Ironclad
{
    public sealed class Bash : IroncladCard<Bash>
    {
        public override string Name => "Bash";

        public override CardType Type => CardType.Attack;

        public override byte EnergyCost => 1;

        public override IEnumerable<IReadOnlyCollection<IReadOnlyCollection<Enemy>>> SelectTargetPermutations(IEnumerable<Enemy> potentialTargets, bool upgraded)
            => SingleTargeted(potentialTargets);

        protected override void BuildActionImpl(IReadOnlyCollection<Enemy> targets, IActionBuilder builder, bool upgraded)
        {
            var target = targets.Single();
            builder.Reduce(
                new Reducer(
                    _ => upgraded ? 10 : 8,
                    Variables.PlayerAttackDamage,
                "Bash Base Damage"));
            builder.Reduce(
                new Reducer(
                    input => input + (upgraded ? 3 : 2),
                    target.StatusState<Vulnerable>(),
                "Bash Base Vuln"));
        }
    }
}
