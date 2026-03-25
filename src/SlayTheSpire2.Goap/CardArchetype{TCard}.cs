using System;

using MountainGoap;

namespace SlayTheSpire2.Goap
{
    public abstract class CardArchetype<TCard> : Singleton<TCard>, ICardArchetype
        where TCard : CardArchetype<TCard>, new()
    {
        public abstract string Name { get; }

        public abstract CardType Type { get; }
        public abstract CardPool Pool { get; }
        public abstract bool EnemyTargeted { get; }

        protected abstract byte EnergyCost { get; }

        public void ApplyPreConditions(Action<string, object> preCondition, Action<string, ComparisonValuePair> comparativePreCondition)
        {
            comparativePreCondition(
                State.Energy,
                new ComparisonValuePair
                {
                    Operator = ComparisonOperator.GreaterThanOrEquals,
                    Value = EnergyCost,
                });
        }
    }
}
