using System;

namespace Vakuu.Engine.Statuses
{
    internal sealed class Block : IStatus
    {
        public string Name => "Block";

        public void OnActionTaken(IActionBuilder actionBuilder, Combatant source, Combatant? target)
        {
            if (target == null)
                return;

            actionBuilder.Reduce(
                new Reducer(
                    (variables, input) => input - (float)Math.Min(input, variables[target.StatusState(this)]),
                    target.IncomingDamageVariable));
        }
    }
}
