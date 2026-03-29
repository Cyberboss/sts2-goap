using System.Collections.Generic;

using MountainGoap;

namespace Vakuu.Engine.Goals
{
    internal sealed class WinAndSurvive : ComparativeGoal
    {
        public WinAndSurvive(float weight)
            : base(
                  "Win and Survive",
                  weight,
                  new Dictionary<string, ComparisonValuePair>
                  {
                      {
                          State.EnemiesAlive,
                          new ComparisonValuePair
                          {
                              Value = (byte)0,
                              Operator = ComparisonOperator.Equals,
                          }
                      },
                      {
                          State.PlayerCurrentHealth,
                          new ComparisonValuePair
                          {
                              Operator = ComparisonOperator.GreaterThan,
                              Value = 0,
                          }
                      },
                  })
        {
        }
    }
}
