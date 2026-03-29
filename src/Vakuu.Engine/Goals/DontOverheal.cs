using System.Collections.Generic;

using MountainGoap;

namespace Vakuu.Engine.Goals
{
    internal sealed class DontOverheal : ComparativeGoal
    {
        public DontOverheal(float weight, ushort maxHP)
            : base(
                  "Don't Overheal",
                  weight,
                  new Dictionary<string, ComparisonValuePair>
                  {
                      {
                          State.PlayerCurrentHealth,
                          new ComparisonValuePair
                          {
                              Operator = ComparisonOperator.LessThanOrEquals,
                              Value = maxHP,
                          }
                      },
                  })
        {
        }
    }
}
