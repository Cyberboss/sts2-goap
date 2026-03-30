using System;

namespace Vakuu.Engine.Statuses
{
    internal sealed class Block : IStatus
    {
        public string Name => "Block";

        public void OnTurnStart(IActionBuilder actionBuilder, Combatant combatant)
            => actionBuilder.Reduce(
                new Reducer(
                    _ => 0,
                    combatant.StatusState(this),
                    "Reset Block"));

        public void OnActionTaken(IActionBuilder actionBuilder, Combatant source, Combatant? target)
        {
            if (target == null)
                return;

            actionBuilder.Reduce(
                new Reducer(
                    (variables, input) =>
                    {
                        var block = variables[target.StatusState(this)];
                        var clamped = (float)Math.Min(input, block);
                        var result = input - clamped;
                        return result;
                    },
                    target.IncomingDamageVariable,
                    "Apply Block Damage Reduction"));
        }
    }
}
