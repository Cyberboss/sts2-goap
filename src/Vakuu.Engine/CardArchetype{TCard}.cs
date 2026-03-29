using System;
using System.Collections.Generic;
using System.Linq;

namespace Vakuu.Engine
{
    public abstract class CardArchetype<TCard> : Singleton<TCard>, ICardArchetype
        where TCard : CardArchetype<TCard>, new()
    {
        public abstract string Name { get; }

        public abstract CardType Type { get; }
        public abstract CardPool Pool { get; }

        public abstract void BuildAction(IReadOnlyCollection<Enemy> targets, IActionBuilder builder, bool upgraded);
        public abstract IEnumerable<IReadOnlyCollection<IReadOnlyCollection<Enemy>>> SelectTargetPermutations(IEnumerable<Enemy> potentialTargets, bool upgraded);
        protected static IEnumerable<IReadOnlyCollection<IReadOnlyCollection<Enemy>>> SingleTargeted(IEnumerable<Enemy> potentialTargets)
            => potentialTargets.Select(
                enemy => new List<List<Enemy>>
                {
                    new List<Enemy>
                    {
                        enemy
                    },
                });

        protected static IEnumerable<IReadOnlyCollection<IReadOnlyCollection<Enemy>>> SelfTargeted()
        {
            yield return Array.Empty<List<Enemy>>();
        }
    }
}
