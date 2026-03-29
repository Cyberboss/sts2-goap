using System;
using System.Collections.Generic;
using System.Linq;

namespace Vakuu.Engine
{
    public sealed class Enemy : Combatant
    {
        readonly Queue<IEnemyMove> upcomingMoves;

        internal Enemy(IEnemyArchetype archetype, IDAllocator idAllocator)
        {
            Archetype = archetype ?? throw new ArgumentNullException(nameof(archetype));
            upcomingMoves = new Queue<IEnemyMove>(archetype.Moveset);
            ID = idAllocator.Allocate();
            Alive = true;
            HealthState = State.EnemyCurrentHealthPrefix + ToString();
            MaxHealthState = State.EnemyMaxHealthPrefix + ToString();
            AttackCountState = State.EnemyAttackCountPrefix + ToString();
            AttackAmountState = State.EnemyAttackAmountPrefix + ToString();
        }

        public ulong ID { get; init; }
        public IEnemyArchetype Archetype { get; }
        public IReadOnlyCollection<IEnemyMove> UpcomingMoves => upcomingMoves;
        public IEnemyMove NextMove => UpcomingMoves.First();

        public override string HealthState { get; }

        public override string MaxHealthState { get; }

        public string AttackCountState { get; }
        public string AttackAmountState { get; }

        public bool Alive { get; private set; }

        public override string ToString() => $"{Archetype.Name} (#{ID})";

        public void CycleMoveset()
            => upcomingMoves.Enqueue(upcomingMoves.Dequeue());

        public void Kill()
            => Alive = false;
    }
}
