using System;

namespace Vakuu.Engine
{
    public sealed class Enemy : Combatant
    {
        internal Enemy(IEnemyArchetype archetype, IDAllocator idAllocator)
        {
            Archetype = archetype ?? throw new ArgumentNullException(nameof(archetype));
            ID = idAllocator.Allocate();
            var stringified = ToString();
            HealthState = State.EnemyCurrentHealthPrefix + stringified;
            MaxHealthState = State.EnemyMaxHealthPrefix + stringified;
            AttackCountState = State.EnemyAttackCountPrefix + stringified;
            AttackAmountVariable = State.EnemyAttackAmountPrefix + stringified;
            BlockGainVariable = Variables.EnemyBlockGainPrefix + stringified;
            IncomingDamageVariable = Variables.EnemyIncomingDamagePrefix + stringified;
        }

        public ulong ID { get; init; }
        public IEnemyArchetype Archetype { get; }

        public override string HealthState { get; }

        public override string MaxHealthState { get; }

        public override string BlockGainVariable { get; }

        public override string IncomingDamageVariable { get; }

        public override string AttackAmountVariable { get; }

        public string AttackCountState { get; }

        public bool Alive { get; private set; }

        public override string ToString() => $"{Archetype.Name} (#{ID})";

        public void ApplyMoves(IActionBuilder actionBuilder, PlayerCharacter character, Ascension ascension, int movesetIndex)
        {
            var moveset = Archetype.Moveset;
            var move = moveset[moveset.Count % movesetIndex];
            bool targetsPlayer = move.Apply(actionBuilder, this, ascension);
            actionBuilder.Reduce(
                new Reducer(
                    (variables, input) => input + ((float)Math.Floor(variables[AttackAmountVariable]) * variables[AttackCountState]),
                    character.IncomingDamageVariable));
            StatusRepository.Apply(status => status.OnActionTaken(actionBuilder, this, targetsPlayer ? character : null));
        }
    }
}
