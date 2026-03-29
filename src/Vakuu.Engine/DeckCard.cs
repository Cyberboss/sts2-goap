using System;
using System.Collections.Generic;
using System.Linq;

namespace Vakuu.Engine
{
    public sealed class DeckCard : ICard
    {
        public ICardArchetype Archetype { get; }

        public bool Upgraded
        {
            get => upgraded;
            private set
            {
                upgraded = value;
                Modifiers = Archetype
                    .Modifiers(upgraded)
                    .Concat(additionalModifiers)
                    .ToHashSet();
            }
        }

        readonly HashSet<CardModifier> additionalModifiers;
        bool upgraded;

        public DeckCard(ICardArchetype archetype, bool upgraded = false)
        {
            Archetype = archetype ?? throw new ArgumentNullException(nameof(archetype));
            additionalModifiers = new HashSet<CardModifier>();
            Upgraded = upgraded;

            if (Modifiers == null)
                throw new InvalidOperationException("Should be set by Upgraded!");
        }

        public IReadOnlySet<CardModifier> Modifiers { get; private set; }

        public void Upgrade()
            => Upgraded = true;
    }
}