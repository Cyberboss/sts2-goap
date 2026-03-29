using System.Collections.Generic;

using MountainGoap;

namespace Vakuu.Engine.Goals
{
    internal sealed class MaximizeHP : ExtremeGoal
    {
        public MaximizeHP(float weight)
            : base(
                  "Maximize HP",
                  weight,
                  new Dictionary<string, bool>
                  {
                      { State.PlayerCurrentHealth, true },
                  })
        {
        }
    }
}
