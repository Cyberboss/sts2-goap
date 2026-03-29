using System.Collections.Generic;

using MountainGoap;

namespace Vakuu.Engine.Goals
{
    internal sealed class DontLoseMaxHP : ExtremeGoal
    {
        public DontLoseMaxHP(float weight)
            : base(
                  "Don't Lose Max HP",
                  weight,
                  new Dictionary<string, bool>
                  {
                      { State.PlayerMaxHealth, true },
                  })
        {
        }
    }
}
