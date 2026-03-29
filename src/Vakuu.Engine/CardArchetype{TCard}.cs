using System.Collections.Generic;
using System.Linq;

namespace Vakuu.Engine
{
    public abstract class CardArchetype<TCard> : Singleton<TCard>, ICardArchetype
        where TCard : CardArchetype<TCard>, new()
    {
        protected static readonly IEnumerable<IReadOnlyCollection<IReadOnlyCollection<Enemy>>> SelfTargeted = new List<IReadOnlyCollection<IReadOnlyCollection<Enemy>>>
        {
            new List<IReadOnlyCollection<Enemy>>
            {
                new List<Enemy>(),
            },
        };

        public abstract string Name { get; }

        public abstract CardType Type { get; }
        public abstract CardPool Pool { get; }

        public void BuildAction(IReadOnlyCollection<Enemy> targets, IActionBuilder builder, PlayerCharacter character, bool upgraded)
        {
            FieldCard.FinalizePreviousCardAction(targets, builder, character);

            builder.Reduce(
                new Reducer(
                    input => input + 1,
                    Variables.PlayerCardActionNumber));

            BuildActionImpl(targets, builder, upgraded);
        }

        public abstract IEnumerable<IReadOnlyCollection<IReadOnlyCollection<Enemy>>> SelectTargetPermutations(IEnumerable<Enemy> potentialTargets, bool upgraded);

        protected abstract void BuildActionImpl(IReadOnlyCollection<Enemy> targets, IActionBuilder builder, bool upgraded);

        protected static IEnumerable<IReadOnlyCollection<IReadOnlyCollection<Enemy>>> SingleTargeted(IEnumerable<Enemy> potentialTargets)
            => potentialTargets.Select(
                enemy => new List<List<Enemy>>
                {
                    new List<Enemy>
                    {
                        enemy
                    },
                });
    }
}
