using System.Collections.Generic;
using System.Linq;

namespace Vakuu.Engine
{
    public interface ICardArchetype
    {
        string Name { get; }

        CardType Type { get; }
        CardPool Pool { get; }

        /// <summary>
        /// Select ways this card could be targeted.
        /// </summary>
        /// <param name="potentialTargets"><see cref="Enemy"/>s that can be targeted.</param>
        /// <param name="upgraded">If the card is upgraded.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> representing the complete set of permutations of USER CHOICE in how this card could be played. Each element is a permutation of how RANDOM SELECTION would play out for that choice. The innermost <see cref="List{T}"/> indicates the order targed <see cref="Enemy"/>s will be affected.</returns>
        IEnumerable<IReadOnlyCollection<IReadOnlyCollection<Enemy>>> SelectTargetPermutations(IEnumerable<Enemy> potentialTargets, bool upgraded);

        IEnumerable<CardModifier> Modifiers(bool upgraded) => Enumerable.Empty<CardModifier>();

        void BuildAction(IReadOnlyCollection<Enemy> targets, IActionBuilder builder, PlayerCharacter character, bool upgraded);

        string ToString(bool upgraded)
            => upgraded
                ? $"{Name}+"
                : Name;
    }
}