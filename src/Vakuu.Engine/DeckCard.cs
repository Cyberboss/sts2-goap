using System;
using System.Collections.Generic;
using System.Linq;

namespace Vakuu.Engine
{
    public sealed class DeckCard : ICard
    {
        public ICardArchetype Archetype { get; }

        public bool Upgraded { get; private set; }

        readonly HashSet<CardModifier> additionalModifiers;

        public DeckCard(ICardArchetype archetype, bool upgraded = false)
        {
            Archetype = archetype ?? throw new ArgumentNullException(nameof(archetype));
            additionalModifiers = new HashSet<CardModifier>();
            Upgraded = upgraded;
        }

        public HashSet<CardModifier> GetModifiers()
            => Archetype
                .Modifiers(Upgraded)
                .Concat(additionalModifiers)
                .ToHashSet();

        public void Upgrade()
            => Upgraded = true;
    }
}