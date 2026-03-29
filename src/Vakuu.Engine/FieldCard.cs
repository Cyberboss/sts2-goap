using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vakuu.Engine
{
    public sealed class FieldCard : ICard
    {
        public delegate void PlayDelegate(Play play);

        readonly PlayDelegate playSink;
        readonly DeckCard deckCard;
        readonly HashSet<CardModifier> additionalModifiers;

        internal FieldCard(PlayDelegate playSink, DeckCard deckCard, IDAllocator idAllocator)
            : this(playSink, deckCard, null, idAllocator, deckCard.Upgraded)
        {
        }

        FieldCard(PlayDelegate playSink, DeckCard deckCard, HashSet<CardModifier>? additionalModifiers, IDAllocator idAllocator, bool upgraded)
        {
            this.playSink = playSink ?? throw new ArgumentNullException(nameof(playSink));
            this.deckCard = deckCard ?? throw new ArgumentNullException(nameof(deckCard));
            this.additionalModifiers = additionalModifiers != null
                ? new HashSet<CardModifier>(additionalModifiers)
                : new HashSet<CardModifier>();
            ID = idAllocator.Allocate();
            Upgraded = upgraded;

            InHandState = $"{State.CardInHandPrefix} {ToString()} (#{ID})";
            InDiscardState = $"{State.CardInDiscardPrefix} {ToString()} (#{ID})"; ;
            InDeckState = $"{State.CardInDeckPrefix} {ToString()} (#{ID})";
            InExhaustState = $"{State.CardInExhaustPrefix} {ToString()} (#{ID})";
            RemovedState = $"{State.CardIsPlayedPowerPrefix} {ToString()} (#{ID})";
        }

        public ulong ID { get; init; }

        public ICardArchetype Archetype => deckCard.Archetype;

        public string InHandState { get; }

        public string InDiscardState { get; }

        public string InDeckState { get; }

        public string InExhaustState { get; }

        public string RemovedState { get; }

        public bool Upgraded { get; private set; }

        public void Upgrade() => deckCard.Upgrade();

        public HashSet<CardModifier> GetModifiers()
        {
            var baseResult = deckCard.GetModifiers();
            baseResult.UnionWith(additionalModifiers);
            return baseResult;
        }

        internal FieldCard Duplicate(IDAllocator idAllocator)
        {
            throw new NotImplementedException("TODO: State update!");
            var result = new FieldCard(playSink, deckCard, additionalModifiers, idAllocator, Upgraded);
            return result;
        }

        public bool EquivalentTo(FieldCard other)
        {
            // TODO
            return Archetype == other.Archetype
                && Upgraded == other.Upgraded;
        }

        public override string ToString() => Archetype.ToString(Upgraded);

        public IEnumerable<MountainGoap.Action> GenerateActions(IEnumerable<Enemy> enemies, PlayerCharacter playerCharacter)
        {
            var userSelectionPermutations = Archetype.SelectTargetPermutations(enemies, Upgraded);
            foreach (var userSelectionRandomPermutations in userSelectionPermutations)
            {
                var cost = userSelectionRandomPermutations.Count;
                foreach (var targetList in userSelectionRandomPermutations)
                {
                    var playName = new StringBuilder("Play ");
                    playName.Append(ToString());
                    playName.Append(" (");
                    playName.Append(ID);
                    playName.Append(")");

                    if (targetList.Count > 0)
                    {
                        playName.Append(" against: ");
                        bool first = true;
                        foreach (var target in targetList)
                        {
                            if (first)
                                playName.Append(", ");
                            else
                                first = false;

                            playName.Append(target);
                        }
                    }

                    var playNameStr = playName.ToString();
                    var builder = new ActionBuilder(
                        enemies,
                        playerCharacter,
                        () => playSink(
                            new Play
                            {
                                CardID = ID,
                                Name = playNameStr,
                                TargetIDs = targetList.Select(enemy => enemy.ID).ToList(),
                            }),
                        playNameStr,
                        cost);

                    builder.AddStaticPrecondition(InHandState, true);
                    builder.AddVariable(InHandState, 0.0f, result => result > 0.0f);

                    Archetype.BuildAction(targetList, builder, Upgraded);

                    if (targetList.Count > 0)
                        foreach (var target in targetList)
                            StatusRepository.Apply(status => status.OnActionTaken(builder, playerCharacter, target));
                    else
                        StatusRepository.Apply(status => status.OnActionTaken(builder, playerCharacter, null));

                    yield return builder.Build();
                }
            }
        }

    }
}
