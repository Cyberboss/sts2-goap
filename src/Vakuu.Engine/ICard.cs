using System.Collections.Generic;

namespace Vakuu.Engine
{
    public interface ICard
    {
        ICardArchetype Archetype { get; }

        bool Upgraded { get; }

        void Upgrade();

        public IReadOnlySet<CardModifier> Modifiers { get; }
    }
}