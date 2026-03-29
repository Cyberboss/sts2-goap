using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Combinatorics.Collections;

using MountainGoap;

using Vakuu.Engine.Goals;

namespace Vakuu.Engine
{
    public sealed class BattleAI
    {
        readonly List<Enemy> enemies;

        readonly Dictionary<ulong, FieldCard> cards;
        readonly List<List<FieldCard>> equivalenceGroups;

        readonly Agent agent;

        Play? nextPlay;

        public BattleAI(
            IEnumerable<ushort> enemiesHPs,
            IEncounter encounter,
            Config config,
            PlayerCharacter character,
            Health playerHealth,
            Ascension ascension)
        {
            ArgumentNullException.ThrowIfNull(encounter);
            ArgumentNullException.ThrowIfNull(config);

            var state = new ConcurrentDictionary<string, object?>
            {
                { character.HealthState, playerHealth.Current },
                { character.MaxHealthState, playerHealth.Max },
                { State.CardDraw, Constants.BaseCardDraw },
                { State.TurnNumber, 1 },
                { State.DeckSize, character.Deck.Count },
                { State.DiscardSize, 0 },
            };

            StatusRepository.Apply(status => state.Add(character.StatusState(status), 0));
            var enemyAllocator = new IDAllocator();

            enemies = encounter.CreateOpponents(enemiesHPs.ToList(), state, enemyAllocator, ascension).ToList();
            state.Add(State.EnemiesAlive, enemies.Count);

            var movesetCycleTurns = 1;
            foreach (var enemy in enemies)
            {
                movesetCycleTurns = LeastCommonMultiple(movesetCycleTurns, enemy.Archetype.Moveset.Count);
                StatusRepository.Apply(status => state.Add(enemy.StatusState(status), 0));
                state.Add(enemy.AttackCountState, 0);
                state.Add(enemy.AttackAmountVariable, 0);
            }

            void SetPlay(Play play) => nextPlay = play;

            var cardAllocator = new IDAllocator();

            equivalenceGroups = new List<List<FieldCard>>();
            var actions = new List<MountainGoap.Action>();
            cards = character
                .Deck
                .Select(card =>
                {
                    var fieldCard = new FieldCard(SetPlay, card, cardAllocator);
                    state.Add(fieldCard.InDeckState, true);
                    state.Add(fieldCard.InHandState, false);
                    state.Add(fieldCard.InDiscardState, false);
                    state.Add(fieldCard.InExhaustState, false);
                    state.Add(fieldCard.RemovedState, false);
                    actions.AddRange(fieldCard.GenerateActions(enemies, character));

                    foreach (var group in equivalenceGroups)
                        if (fieldCard.EquivalentTo(group[0]))
                        {
                            group.Add(fieldCard);
                            return fieldCard;
                        }

                    equivalenceGroups.Add(new List<FieldCard>
                    {
                        fieldCard
                    });

                    return fieldCard;
                })
                .ToDictionary(fieldCard => fieldCard.ID);

            IEnumerable<Reducer> GenerateCardMoveReducers(KeyValuePair<ulong, FieldCard> kvp)
            {
                var card = kvp.Value;

                // in discard
                yield return new Reducer(
                    (variables, input) => variables[State.CardDraw] >= variables[State.DeckSize]
                        ? 0.0f
                        : (variables[card.InHandState] > 0.0f
                            ? 1.0f
                            : input),
                    card.InDiscardState);
                yield return new Reducer(
                    (variables, input) => variables[State.CardDraw] >= variables[State.DeckSize]
                        ? 0.0f
                        : (variables[card.InHandState] > 0.0f
                            ? 1.0f
                            : input),
                    card.InDiscardState);

                // exhaust
                if (card.Modifiers.Contains(CardModifier.Ethereal))
                    yield return new Reducer(
                        (variables, input) => input > 0 || variables[card.InExhaustState] > 0 ? 1.0f : 0.0f,
                        card.InExhaustState);

                // in hand
                if (!card.Modifiers.Contains(CardModifier.Retain))
                {
                    yield return new Reducer(
                        (variables, input) => variables[card.InHandState] > 0 ? input + 1 : input,
                        State.DiscardSize);
                    yield return new Reducer(
                        input => input > 0 ? 0.0f : 1.0f,
                        card.InHandState);
                }
            }

            IEnumerable<Reducer> GenerateEnemyResetReducers(Enemy enemy)
            {
                yield return new Reducer(
                    _ => 0.0f,
                    enemy.AttackCountState);
                yield return new Reducer(
                    _ => 0.0f,
                    enemy.AttackAmountVariable);
            }

            var advanceTurnReducer = new Reducer(
                turnNumber => turnNumber + 1U,
                State.TurnNumber);
            var resetCardDrawReducer = new Reducer(
                _ => Constants.BaseCardDraw,
                State.CardDraw);

            var resetEnemyStateVariablesReducers = enemies
                .SelectMany(GenerateEnemyResetReducers)
                .ToList();

            var basicCardMoveReducers = cards
                .Where(
                    kvp => !kvp.Value.Modifiers.Contains(CardModifier.Retain))
                .SelectMany(GenerateCardMoveReducers)
                .ToList();

            Dictionary<ulong, ReducerGroup> cardMoveFromDeckToHandReducers = cards
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp =>
                    {
                        var card = kvp.Value;
                        var group = new ReducerGroup();
                        group.Add(
                            new Reducer(
                                _ => 0.0f,
                               card.InDeckState));
                        group.Add(
                            new Reducer(
                                _ => 1.0f,
                               card.InHandState));
                        return group;
                    });

            ReducerGroup emptyDiscardsGroup = new ReducerGroup();
            foreach (var kvp in cards)
            {
                var card = kvp.Value;
                emptyDiscardsGroup.Add(
                    new Reducer(
                        (variables, input) => variables[card.InDiscardState] > 0 && variables[State.CardDraw] >= variables[State.DeckSize]
                            ? 1.0f
                            : input,
                        card.InDeckState));
            }

            Dictionary<ulong, Func<IReadOnlyDictionary<string, object?>, bool>> cardCanBeDrawnPreconditions = cards
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp =>
                    {
                        var card = kvp.Value;
                        return (Func<IReadOnlyDictionary<string, object?>, bool>)(
                            state => (int)state[card.InDeckState]! > 0
                                || ((!card.Modifiers.Contains(CardModifier.Retain) && (int)state[card.InHandState]! > 0
                                || (int)state[card.InDiscardState]! > 0)
                                && (int)state[State.CardDraw]! >= (int)state[State.DeckSize]!));
                    });

            var drawnNCardsReducers = new Reducer[Constants.MaxHandSize];
            for (var i = 0; i < drawnNCardsReducers.Length; ++i)
                drawnNCardsReducers[i] = new Reducer(
                    input => input - (i + 1),
                    State.DeckSize);

            ActionBuilder BuildEndTurnAction(IReadOnlyList<ulong> drawnCards, int movesetIndex)
            {
                var nameBuilder = new StringBuilder();
                nameBuilder.Append("End Turn (Cycle ");
                nameBuilder.Append(movesetIndex + 1);
                nameBuilder.Append('/');
                nameBuilder.Append(movesetCycleTurns);
                nameBuilder.Append("). Draw");

                if (drawnCards.Count == 0)
                    nameBuilder.Append(" none");
                else
                {
                    nameBuilder.Append(": ");

                    var first = true;
                    foreach (var card in drawnCards)
                    {
                        if (first)
                            first = false;
                        else
                            nameBuilder.Append(", ");

                        nameBuilder.Append(card.ToString());
                    }
                }

                var name = nameBuilder.ToString();

                var endTurnActionBuilder = new ActionBuilder(
                    enemies,
                    character,
                    () => nextPlay = new Play
                    {
                        Name = name,
                        CardID = null,
                        TargetIDs = Array.Empty<ulong>(),
                    },
                    name,
                    null);

                endTurnActionBuilder.AddStaticPrecondition(
                    State.CardDraw,
                    drawnCards.Count);

                foreach (var cardID in drawnCards)
                    endTurnActionBuilder.AddDynamicPrecondition(cardCanBeDrawnPreconditions[cardID]);

                foreach (var reducer in basicCardMoveReducers.Concat(resetEnemyStateVariablesReducers))
                    endTurnActionBuilder.Reduce(reducer);

                StatusRepository.Apply(status => status.OnTurnEnd(endTurnActionBuilder, character));
                foreach (var relic in character.Relics)
                    relic.OnTurnEnd(endTurnActionBuilder);

                var aliveEnemies = enemies.Where(enemy => enemy.Alive);
                foreach (var enemy in aliveEnemies)
                    StatusRepository.Apply(status => status.OnTurnStart(endTurnActionBuilder, enemy));

                foreach (var enemy in aliveEnemies)
                    enemy.ApplyMoves(endTurnActionBuilder, character, ascension, movesetIndex);

                foreach (var enemy in aliveEnemies)
                    StatusRepository.Apply(status => status.OnTurnEnd(endTurnActionBuilder, enemy));

                endTurnActionBuilder.Reduce(advanceTurnReducer);
                endTurnActionBuilder.Reduce(resetCardDrawReducer);

                foreach (var relic in character.Relics)
                    relic.OnTurnStart(endTurnActionBuilder);

                StatusRepository.Apply(status => status.OnTurnStart(endTurnActionBuilder, character));

                endTurnActionBuilder.Reduce(emptyDiscardsGroup);

                foreach (var cardID in drawnCards)
                    endTurnActionBuilder.Reduce(cardMoveFromDeckToHandReducers[cardID]);

                if (drawnCards.Count > 0)
                    endTurnActionBuilder.Reduce(drawnNCardsReducers[drawnCards.Count - 1]);

                return endTurnActionBuilder;
            }

            var endTurnActionBuilders = new List<ActionBuilder>();
            for (var i = 0; i <= Constants.MaxHandSize; ++i)
                for (var j = 0; j < movesetCycleTurns; ++j)
                    foreach (var combination in new Combinations<ulong>(cards.Keys, i))
                        endTurnActionBuilders.Add(BuildEndTurnAction(combination, j));

            foreach (var builder in endTurnActionBuilders)
                builder.SetCost(endTurnActionBuilders.Count);

            actions.AddRange(endTurnActionBuilders.Select(builder => builder.Build()));

            agent = new Agent(
                $"Encounter {encounter.EncounterType}: {encounter.Name}",
                state,
                null,
                new List<BaseGoal>
                {
                    new WinAndSurvive(config.WinAndSurvive),
                    new DontOverheal(config.DontOverheal, playerHealth.Max),
                    new MaximizeHP(config.MaximizeHP),
                    new DontLoseMaxHP(config.PreserveMaxHP),
                    new MaximizeDosh(config.MaximizeDosh),
                    new MaintainPotions(config.MaintainPotions),
                },
                actions);
        }

        public IReadOnlyDictionary<ulong, FieldCard> Cards => cards;

        public IEnumerable<FieldCard> DeckCards => EnumerateCards(card => card.InDeckState);

        public IEnumerable<FieldCard> HandCards => EnumerateCards(card => card.InHandState);

        public IEnumerable<FieldCard> ExhaustedCards => EnumerateCards(card => card.InExhaustState);

        public IEnumerable<FieldCard> RemovedCards => EnumerateCards(card => card.RemovedState);

        public float EvaluateFightCost()
        {
            throw new NotImplementedException();
        }

        public void UpdateHand(IReadOnlyCollection<ulong> hand)
        {
            foreach (var kvp in cards)
            {
                var inhand = hand.Contains(kvp.Key);
                agent.State[kvp.Value.InHandState] = inhand;
                agent.State[kvp.Value.InDeckState] = !inhand && (bool)agent.State[kvp.Value.InDeckState]!;
            }
        }

        public Play? GetBestMove()
        {
            agent.Step(StepMode.OneAction);
            if (!agent.IsBusy)
                return null;

            return nextPlay!;
        }

        static int GreatestCommonDenominator(int a, int b)
        {
            // Absolute values are used to handle negative inputs
            a = Math.Abs(a);
            b = Math.Abs(b);

            while (b != 0)
            {
                var temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        static int LeastCommonMultiple(int a, int b)
        {
            if (a == 0 || b == 0)
            {
                return 0; // LCM is 0 if either number is 0
            }

            var lcm = Math.Abs(a) / GreatestCommonDenominator(a, b) * Math.Abs(b);
            return lcm;
        }

        IEnumerable<FieldCard> EnumerateCards(Func<FieldCard, string> booleanStateSelector)
        {
            foreach (var kvp in cards)
                if ((bool)agent.State[booleanStateSelector(kvp.Value)]!)
                    yield return kvp.Value;
        }
    }
}
