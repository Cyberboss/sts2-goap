using System.Collections.Generic;

namespace Vakuu.Engine
{
    public interface IEnemyArchetype
    {
        public string Name { get; }
        IReadOnlyList<IEnemyMove> Moveset { get; }
        (ushort Low, ushort High) HPRange(Ascension ascension);

        void OnSpawn(IDictionary<string, object?> state, Ascension ascension)
        {
        }
    }
}
