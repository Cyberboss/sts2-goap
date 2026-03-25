using System.Collections.Generic;
using System.Linq;

namespace SlayTheSpire2.Goap
{
    public interface IEnemyArchetype
    {
        public string Name { get; }
        IReadOnlyList<IEnemyMove> Moveset { get; }
        (ushort Low, ushort High) HPRange(Ascension ascension);

        IEnumerable<IStatus> InitialStatuses(Ascension ascension) => Enumerable.Empty<IStatus>();

        Enemy CreateEnemy(ushort knownHP, Ascension ascension)
            => new Enemy(
                InitialStatuses(ascension),
                this,
                new Health
                {
                    Current = knownHP,
                    Max = knownHP,
                });
    }
}
