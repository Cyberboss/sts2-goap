using System;
using System.Collections.Generic;

namespace Vakuu.Engine
{
    public interface IEncounter
    {
        EncounterType EncounterType { get; }

        string Name { get; }

        IReadOnlyList<IEnemyArchetype> Archetypes { get; }

        internal IEnumerable<Enemy> CreateOpponents(IReadOnlyList<ushort> knownHPs, IDictionary<string, object?> state, IDAllocator idAllocator, Ascension ascension)
        {
            ArgumentNullException.ThrowIfNull(knownHPs);
            if (knownHPs.Count != Archetypes.Count)
                throw new ArgumentOutOfRangeException(nameof(knownHPs), knownHPs.Count, $"Expected {Archetypes.Count} HP values for instantiating enemies!");

            for (var i = 0; i < Archetypes.Count; ++i)
            {
                var archetype = Archetypes[i];
                var enemy = new Enemy(archetype, idAllocator);

                var hp = knownHPs[i];
                state.Add(enemy.MaxHealthState, hp);
                state.Add(enemy.HealthState, hp);
                state.Add(enemy.BlockGainVariable, (ushort)0);
                state.Add(enemy.AttackCountState, 0);
                state.Add(enemy.AttackAmountVariable, 0);

                archetype.OnSpawn(state, ascension);
                yield return enemy;
            }
        }
    }
}
