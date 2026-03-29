using System.Collections.Generic;

namespace Vakuu.Engine.Cards.Ironclad
{
    public sealed class Bash : IroncladCard<Bash>
    {
        public override string Name => "Bash";

        public override CardType Type => CardType.Attack;

        public override IEnumerable<IReadOnlyCollection<IReadOnlyCollection<Enemy>>> SelectTargetPermutations(IEnumerable<Enemy> potentialTargets, bool upgraded)
            => SingleTargeted(potentialTargets);

        public override void BuildAction(IReadOnlyCollection<Enemy> targets, IActionBuilder builder, bool upgraded) => throw new System.NotImplementedException();
    }
}
