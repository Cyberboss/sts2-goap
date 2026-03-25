using System;
using System.Collections.Generic;

namespace SlayTheSpire2.Goap
{
    public sealed class Enemy : Combatant
    {
        readonly Queue<IEnemyMove> upcomingMoves;

        public Enemy(IEnumerable<IStatus> statuses, IEnemyArchetype archetype, Health health)
            : base(statuses, health)
        {
            Archetype = archetype ?? throw new ArgumentNullException(nameof(archetype));
            upcomingMoves = new Queue<IEnemyMove>(archetype.Moveset);
        }

        public Guid ID { get; init; }
        public IEnemyArchetype Archetype { get; }
        public IReadOnlyCollection<IEnemyMove> UpcomingMoves => upcomingMoves;
    }
}
