using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Vakuu.Engine
{
    public sealed class FieldCard : ICard
    {
        public delegate void PlayDelegate(Play play);

        readonly PlayDelegate playSink;
        readonly DeckCard deckCard;

        internal FieldCard(PlayDelegate playSink, DeckCard deckCard, IDAllocator idAllocator)
            : this(playSink, deckCard, null, idAllocator, deckCard.Upgraded)
        {
        }

        FieldCard(PlayDelegate playSink, DeckCard deckCard, IEnumerable<CardModifier>? additionalModifiers, IDAllocator idAllocator, bool upgraded)
        {
            this.playSink = playSink ?? throw new ArgumentNullException(nameof(playSink));
            this.deckCard = deckCard ?? throw new ArgumentNullException(nameof(deckCard));

            ID = idAllocator.Allocate();
            Upgraded = upgraded;

            RecalculateModifiers(additionalModifiers);

            var name = ToString();
            InHandState = State.CardInHandPrefix + name;
            InDiscardState = State.CardInDiscardPrefix + name;
            InDeckState = State.CardInDeckPrefix + name;
            InExhaustState = State.CardInExhaustPrefix + name;
            RemovedState = State.CardIsPlayedPowerPrefix + name;
        }

        public ulong ID { get; init; }

        public ICardArchetype Archetype => deckCard.Archetype;

        public string InHandState { get; }

        public string InDiscardState { get; }

        public string InDeckState { get; }

        public string InExhaustState { get; }

        public string RemovedState { get; }

        public bool Upgraded { get; private set; }

        public void Upgrade()
        {
            Upgraded = true;
            RecalculateModifiers(null);
        }

        public IReadOnlySet<CardModifier> Modifiers { get; private set; }

        internal FieldCard Duplicate(IDAllocator idAllocator)
        {
            throw new NotImplementedException();
        }

        public static void FinalizePreviousCardAction(IReadOnlyCollection<Enemy> targets, IActionBuilder builder, PlayerCharacter character)
        {
            foreach (var target in targets)
                builder.Reduce(
                    new Reducer(
                        (variables, input) => input - Math.Min(input, variables[Variables.PlayerAttackDamage]),
                        target.IncomingDamageVariable,
                        $"Apply Attack Damage from Card Play to Enemy incoming damage: {target}"));

            builder.ApplyCombatBuffers(true);
        }

        public bool EquivalentTo(FieldCard other)
        {
            // TODO
            return Archetype == other.Archetype
                && Upgraded == other.Upgraded;
        }

        public override string ToString() => $"{Archetype.ToString(deckCard.Upgraded)} (#{ID})"; // using the constant name from the deck here intentionally

        public IEnumerable<MountainGoap.Action> GenerateActions(IEnumerable<Enemy> enemies, PlayerCharacter character)
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
                            if (!first)
                                playName.Append(", ");
                            else
                                first = false;

                            playName.Append(target);
                        }
                    }

                    var playNameStr = playName.ToString();
                    var builder = new ActionBuilder(
                        enemies,
                        character,
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
                    builder.AddDynamicPrecondition(state => Archetype.EvaluatePreconditions(state, Upgraded));

                    if (Modifiers.Contains(CardModifier.Exhaust))
                    {
                        builder.AddStaticPostCondition(InExhaustState, true);
                        builder.AddStaticPostCondition(InHandState, false);
                    }
                    else if (Archetype.Type == CardType.Power)
                    {
                        builder.AddStaticPostCondition(RemovedState, true);
                        builder.AddStaticPostCondition(InHandState, false);
                    }
                    else
                    {
                        builder.AddVariableFromState(InDiscardState, result => result > 0.0f);
                        builder.Reduce(
                            new Reducer(
                                _ => 1.0f,
                                InDiscardState));

                        builder.AddVariableFromState(InHandState, result => result > 0.0f);
                        builder.Reduce(
                            new Reducer(
                                _ => 0.0f,
                                InHandState));
                    }

                    builder.AddVariableFromState(State.CardDraw, result => (byte)result);

                    builder.AddVariable<ushort>(Variables.PlayerAttackDamage, 0, null);
                    builder.AddVariable<byte>(Variables.PlayerCardActionNumber, 0, null);

                    Archetype.BuildAction(targetList, builder, character, Upgraded);

                    FinalizePreviousCardAction(targetList, builder, character);

                    if (targetList.Count > 0)
                        foreach (var target in targetList)
                            StatusRepository.Apply(status => status.OnActionTaken(builder, character, target));
                    else
                        StatusRepository.Apply(status => status.OnActionTaken(builder, character, null));

                    yield return builder.Build();
                }
            }
        }

        [MemberNotNull(nameof(Modifiers))]
        void RecalculateModifiers(IEnumerable<CardModifier>? additionalModifiers)
        {
            var modifiers = new HashSet<CardModifier>(deckCard.Modifiers);
            if (additionalModifiers != null)
                modifiers.UnionWith(additionalModifiers);
            if (Upgraded && !deckCard.Upgraded)
                modifiers.UnionWith(deckCard.Archetype.Modifiers(true));

            Modifiers = modifiers;
        }
    }
}
