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
            };

            var enemyAllocator = new IDAllocator();

            enemies = encounter.CreateOpponents(enemiesHPs.ToList(), state, enemyAllocator, ascension).ToList();
            state.Add(State.EnemiesAlive, enemies.Count);

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
                    {
                        if (fieldCard.EquivalentTo(group[0]))
                        {
                            group.Add(fieldCard);
                            return fieldCard;
                        }
                    }

                    equivalenceGroups.Add(new List<FieldCard>
                    {
                        fieldCard
                    });
                    return fieldCard;
                })
                .ToDictionary(fieldCard => fieldCard.ID);

            var advanceTurnReducer = new Reducer(
                turnNumber => turnNumber + 1,
                State.TurnNumber);

            ActionBuilder BuildEndTurnAction(IReadOnlyList<ulong> drawnCards)
            {
                var nameBuilder = new StringBuilder();
                nameBuilder.Append("End Turn. Draw");

                if (drawnCards.Count == 0)
                {
                    nameBuilder.Append(" none");
                }
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
                    () =>
                    {
                        nextPlay = new Play
                        {
                            Name = name,
                            CardID = null,
                            TargetIDs = Array.Empty<ulong>(),
                        };

                        foreach (var enemy in enemies)
                        {
                            enemy.CycleMoveset();
                        }
                    },
                    name,
                    null);

                StatusRepository.Apply(status => status.OnTurnEnd(endTurnActionBuilder, character));
                foreach (var relic in character.Relics)
                {
                    relic.OnTurnEnd(endTurnActionBuilder);
                }

                var aliveEnemies = enemies.Where(enemy => enemy.Alive);
                foreach (var enemy in aliveEnemies)
                {
                    StatusRepository.Apply(status => status.OnTurnStart(endTurnActionBuilder, enemy));
                }

                foreach (var enemy in aliveEnemies)
                {
                    bool targetedPlayer = enemy.NextMove.Apply(endTurnActionBuilder, enemy, ascension);
                    StatusRepository.Apply(status => status.OnActionTaken(endTurnActionBuilder, enemy, targetedPlayer ? character : null));
                }

                foreach (var enemy in aliveEnemies)
                {
                    StatusRepository.Apply(status => status.OnTurnEnd(endTurnActionBuilder, enemy));
                }

                endTurnActionBuilder.Reduce(advanceTurnReducer);

                foreach (var relic in character.Relics)
                {
                    relic.OnTurnStart(endTurnActionBuilder);
                }

                StatusRepository.Apply(status => status.OnTurnStart(endTurnActionBuilder, character));

                return endTurnActionBuilder;
            }

            var endTurnActionBuilders = new List<ActionBuilder>();
            for (var i = 0; i <= Constants.MaxHandSize; ++i)
            {
                foreach (var combination in new Combinations<ulong>(cards.Keys, i))
                    endTurnActionBuilders.Add(BuildEndTurnAction(combination));
            }

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
                agent.State[kvp.Value.InHandState] = hand.Contains(kvp.Key);

            agent.State[State.HandSize] = hand.Count;

            agent.PlanAsync();
        }

        public Play GetBestMove()
        {
            agent.Step(StepMode.OneAction);
            return nextPlay!;
        }

        IEnumerable<FieldCard> EnumerateCards(Func<FieldCard, string> booleanStateSelector)
        {
            foreach (var kvp in cards)
                if ((bool)agent.State[booleanStateSelector(kvp.Value)]!)
                    yield return kvp.Value;
        }
    }
}
