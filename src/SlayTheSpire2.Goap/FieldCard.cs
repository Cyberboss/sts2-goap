using System;
using System.Collections.Generic;

using MountainGoap;

namespace SlayTheSpire2.Goap
{
    public sealed class FieldCard : ICard
    {
        readonly Action<Guid> playSink;
        readonly Card deckCard;
        readonly HashSet<CardModifier> additionalModifiers;

        public FieldCard(Action<Guid> playSink, Card deckCard)
            : this(playSink, deckCard, null, deckCard.Upgraded)
        {
        }

        private FieldCard(Action<Guid> playSink, Card deckCard, HashSet<CardModifier>? additionalModifiers, bool upgraded)
        {
            this.playSink = playSink ?? throw new ArgumentNullException(nameof(playSink));
            this.deckCard = deckCard ?? throw new ArgumentNullException(nameof(deckCard));
            this.additionalModifiers = additionalModifiers != null
                ? new HashSet<CardModifier>(additionalModifiers)
                : new HashSet<CardModifier>();
            ID = Guid.NewGuid();
            Upgraded = upgraded;
        }

        public Guid ID { get; init; }

        public ICardArchetype Archetype => deckCard.Archetype;

        public string InHandState => $"Card \"{ToString()}\" In-Hand ({ID})";

        public bool Upgraded { get; private set; }

        public void Upgrade() => deckCard.Upgrade();

        public HashSet<CardModifier> GetModifiers()
        {
            var baseResult = deckCard.GetModifiers();
            baseResult.UnionWith(additionalModifiers);
            return baseResult;
        }

        public FieldCard Duplicate()
        {
            var result = new FieldCard(playSink, deckCard, additionalModifiers, Upgraded);
            return result;
        }

        public IEnumerable<MountainGoap.Action> GenerateActions()
        {
            var (preConditions, comparativePreConditions) = BuildPreConditions();
            yield return new MountainGoap.Action(
                $"Play {ToString()} ({ID})",
                executor: (agent, action) =>
                {
                    playSink(ID);
                    return ExecutionStatus.Succeeded;
                },
                cost: 0.0f,
                preconditions: preConditions,
                postconditions: BuildPostConditions(),
                comparativePreconditions: comparativePreConditions);
        }


        private (Dictionary<string, object> PreConditions, Dictionary<string, ComparisonValuePair> ComparativePreConditions) BuildPreConditions()
        {
            var preConditions = new Dictionary<string, object>
            {
                { InHandState, true }
            };

            var comparativePreConditions = new Dictionary<string, ComparisonValuePair>();

            Archetype.ApplyPreConditions(preConditions.Add, comparativePreConditions.Add);

            return (preConditions, comparativePreConditions);
        }
    }
}
