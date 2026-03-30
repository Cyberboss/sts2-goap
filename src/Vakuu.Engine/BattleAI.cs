using System;
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
            Ascension ascension,
            byte startingEnergy)
        {
            ArgumentNullException.ThrowIfNull(encounter);
            ArgumentNullException.ThrowIfNull(config);

            var state = new Dictionary<string, object?>
            {
                { character.HealthState, playerHealth.Current },
                { character.MaxHealthState, playerHealth.Max },
                { State.CardDraw, Constants.BaseCardDraw },
                { State.TurnNumber, (byte)1 },
                { State.DeckSize, (byte)character.Deck.Count },
                { State.DiscardSize, 0 },
                { State.Energy, startingEnergy },
                { State.EnergyGainBase, Constants.DefaultEnergy },
                { State.EnergyGainTemp, (sbyte)0 },
            };

            StatusRepository.Apply(status => state.Add(character.StatusState(status), 0));
            var enemyAllocator = new IDAllocator();

            enemies = encounter.CreateOpponents(enemiesHPs.ToList(), state, enemyAllocator, ascension).ToList();
            state.Add(State.EnemiesAlive, enemies.Count);

            ushort movesetCycleTurns = 1;
            var initialEnemyActionBuilder = new ActionBuilder(enemies, character, () => { }, "Enemy Initial Move", 0.0f);
            foreach (var enemy in enemies)
            {
                movesetCycleTurns = (ushort)LeastCommonMultiple(movesetCycleTurns, enemy.Archetype.Moveset.Count);
                StatusRepository.Apply(status => state.Add(enemy.StatusState(status), (short)0));
                enemy.ApplyMovesAheadOfPlayerTurn(initialEnemyActionBuilder, character, ascension, 0);
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
                turnNumber => turnNumber + 1,
                State.TurnNumber);
            var resetCardDrawReducer = new Reducer(
                _ => Constants.BaseCardDraw,
                State.CardDraw);

            var resetEnemyStateVariablesReducers = enemies
                .SelectMany(GenerateEnemyResetReducers)
                .ToList();

            var resetEnergyGainReducer = new Reducer(
                _ => 0.0f,
                State.EnergyGainTemp);
            var applyEnergyGainReducer = new Reducer(
                (variables, _) => variables[State.EnergyGainBase] + variables[State.EnergyGainTemp],
                State.Energy);

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

            Dictionary<ulong, Func<IReadOnlyDictionary<string, object?>, bool>> cardCanBeInHandPreconditions = cards
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp =>
                    {
                        var card = kvp.Value;
                        return (Func<IReadOnlyDictionary<string, object?>, bool>)(
                            state =>
                            {
                                if ((bool)state[card.InDeckState]!)
                                    return true;

                                if ((bool)state[card.InHandState]! && card.Modifiers.Contains(CardModifier.Retain))
                                    return true;

                                var retainInHand = !card.Modifiers.Contains(CardModifier.Retain) && (bool)state[card.InHandState]!;
                                if (retainInHand || (bool)state[card.InDiscardState]!)
                                {
                                    var cardDraw = (byte)state[State.CardDraw]!;
                                    var potentialToDraw = cardDraw > (byte)state[State.DeckSize]!;
                                    if (potentialToDraw)
                                        return true;
                                }

                                return false;
                            });
                    });

            var drawnNCardsReducers = new Reducer[Constants.MaxHandSize];
            for (var i = 0; i < drawnNCardsReducers.Length; ++i)
                drawnNCardsReducers[i] = new Reducer(
                    input => input - (i + 1),
                    State.DeckSize);

            ActionBuilder BuildEndTurnAction(IReadOnlyList<ulong> drawnCards, int nextMovesetIndex)
            {
                var nameBuilder = new StringBuilder();
                var thisMovesetIndex = nextMovesetIndex > 0
                    ? nextMovesetIndex - 1
                    : movesetCycleTurns - 1;
                nameBuilder.Append("End Turn (Cycle: ");
                nameBuilder.Append(thisMovesetIndex + 1);
                nameBuilder.Append('/');
                nameBuilder.Append(movesetCycleTurns);
                nameBuilder.Append("). Draw");

                if (drawnCards.Count == 0)
                    nameBuilder.Append(" none");
                else
                {
                    nameBuilder.Append(": ");

                    var first = true;
                    foreach (var cardID in drawnCards)
                    {
                        if (first)
                            first = false;
                        else
                            nameBuilder.Append(", ");

                        nameBuilder.Append(cards[cardID].ToString());
                    }
                }

                nameBuilder.Append(". ");
                bool first2 = true;
                foreach (var enemy in enemies)
                {
                    if (!first2)
                        nameBuilder.AppendLine(", ");
                    else
                        first2 = false;
                    nameBuilder.Append(enemy);
                    nameBuilder.Append(": ");
                    nameBuilder.Append(enemy.Archetype.Moveset[thisMovesetIndex].Name);
                }

                var name = nameBuilder.ToString();

                /*
                if (drawnCards.Count == 5 && !name.Contains("Strike (#1)") && !name.Contains("Strike (#2)") && !name.Contains("Strike (#3)") && !name.Contains("Defend (#5)") && !name.Contains("Defend (#6)"))
                    name += " WINNABLE";
                */

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
                    (byte)drawnCards.Count);

                endTurnActionBuilder.AddDynamicPrecondition(
                    state =>
                    {
                        var turnNumber = (byte)state[State.TurnNumber]!;
                        var modulo = turnNumber % movesetCycleTurns;
                        var equivalent = modulo == nextMovesetIndex;
                        /*
                        if (equivalent && movesetIndex == 2 && turnNumber < 3)
                        {
                            Console.WriteLine($"T: {turnNumber}, M: {modulo}");
                            Debugger.Launch();
                            Console.WriteLine($"T: {turnNumber}, M: {modulo}");
                        }*/

                        return equivalent;
                    });

                endTurnActionBuilder.AddVariableFromState(State.TurnNumber, result => (byte)result);

                foreach (var cardID in drawnCards)
                    endTurnActionBuilder.AddDynamicPrecondition(cardCanBeInHandPreconditions[cardID]);

                foreach (var reducer in basicCardMoveReducers)
                    endTurnActionBuilder.Reduce(reducer);

                StatusRepository.Apply(status => status.OnTurnEnd(endTurnActionBuilder, character));
                foreach (var relic in character.Relics)
                    relic.OnTurnEnd(endTurnActionBuilder);

                foreach (var enemy in enemies)
                    StatusRepository.Apply(status => status.OnTurnStart(endTurnActionBuilder, enemy));

                foreach (var enemy in enemies)
                    enemy.ApplyOnTurn(endTurnActionBuilder, character);

                endTurnActionBuilder.ApplyCombatBuffers(false);

                foreach (var enemy in enemies)
                    StatusRepository.Apply(status => status.OnTurnEnd(endTurnActionBuilder, enemy));

                endTurnActionBuilder.Reduce(advanceTurnReducer);
                endTurnActionBuilder.Reduce(resetCardDrawReducer);
                foreach (var reducer in resetEnemyStateVariablesReducers)
                    endTurnActionBuilder.Reduce(reducer);

                foreach (var enemy in enemies)
                    enemy.ApplyMovesAheadOfPlayerTurn(endTurnActionBuilder, character, ascension, nextMovesetIndex);

                foreach (var relic in character.Relics)
                    relic.OnTurnStart(endTurnActionBuilder);

                StatusRepository.Apply(status => status.OnTurnStart(endTurnActionBuilder, character));

                endTurnActionBuilder.Reduce(applyEnergyGainReducer);
                endTurnActionBuilder.Reduce(resetEnergyGainReducer);

                endTurnActionBuilder.Reduce(emptyDiscardsGroup);

                foreach (var cardID in drawnCards)
                    endTurnActionBuilder.Reduce(cardMoveFromDeckToHandReducers[cardID]);

                if (drawnCards.Count > 0)
                    endTurnActionBuilder.Reduce(drawnNCardsReducers[drawnCards.Count - 1]);

                return endTurnActionBuilder;
            }

            var endTurnActionBuilders = new List<ActionBuilder>();
            for (byte i = 0; i <= Constants.MaxHandSize; ++i)
                for (ushort j = 0; j < movesetCycleTurns; ++j)
                    foreach (var combination in new Combinations<ulong>(cards.Keys, i))
                        endTurnActionBuilders.Add(BuildEndTurnAction(combination, j));

            foreach (var builder in endTurnActionBuilders)
                builder.SetCost(endTurnActionBuilders.Count);

            actions.AddRange(endTurnActionBuilders.Select(builder => builder.Build()));

            initialEnemyActionBuilder.Build().ApplyEffects(state);

            agent = new Agent(
                $"Encounter {encounter.EncounterType}: {encounter.Name}",
                state,
                null,
                new List<BaseGoal>
                {
                    new WinAndSurvive(config.WinAndSurvive),
                    //new DontOverheal(config.DontOverheal, playerHealth.Max),
                    //new MaximizeHP(config.MaximizeHP),
                    //new DontLoseMaxHP(config.PreserveMaxHP),
                    //new MaximizeDosh(config.MaximizeDosh),
                    //new MaintainPotions(config.MaintainPotions),
                },
                actions,
                stepMaximum: 100);
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
