using System.Collections.Generic;

using SlayTheSpire2.Goap.Statuses;

namespace SlayTheSpire2.Goap.Cards
{
    public abstract class Defend<TDefend> : CardArchetype<TDefend>
        where TDefend : Defend<TDefend>, new()
    {
        public override string Name => "Defend";

        public override CardType Type => CardType.Attack;

        public override bool EnemyTargeted => false;

        protected override byte EnergyCost => 1;

        public IEnumerable<IStatus> InvokerStatuses(bool upgraded)
        {
            yield return new Block((short)(upgraded ? 8 : 5));
        }
    }
}
