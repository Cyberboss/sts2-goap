using System;
using System.Collections.Generic;
using System.Linq;

using Vakuu.Engine.Cards.Ironclad;

namespace Vakuu.Engine
{
    public sealed class PlayerCharacter : Combatant
    {
        public Character Character { get; }

        public IReadOnlyList<IRelic> Relics { get; }

        public List<DeckCard> Deck { get; }

        public override string HealthState => State.PlayerCurrentHealth;

        public override string MaxHealthState => State.PlayerMaxHealth;

        public PlayerCharacter(Character character)
        {
            Relics = new List<IRelic>(1);
            Deck = new List<DeckCard>(10);
            Character = character;

            switch (character)
            {
                case Character.Ironclad:
                    Deck.AddRange(Enumerable.Repeat<object?>(null, 6).Select(_ => new DeckCard(Cards.Ironclad.Strike.Instance)));
                    Deck.AddRange(Enumerable.Repeat<object?>(null, 4).Select(_ => new DeckCard(Cards.Ironclad.Defend.Instance)));
                    Deck.Add(new DeckCard(Bash.Instance));
                    break;
                default:
                    throw new NotImplementedException($"No initial deck for {character}!");
            }
        }
    }
}
