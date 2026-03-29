namespace Vakuu.Engine.Statuses
{
    internal sealed class Strength : IStatus
    {
        public string Name => "Strength";

        public void OnActionTaken(IActionBuilder actionBuilder, Combatant source, Combatant? target)
        {
            if (target == null)
                return;

            actionBuilder.Reduce(
                new Reducer(
                    (variables, input) => input * ((variables[target.StatusState(this)] > 0) ? 1.5f : 1.0f),
                    source.AttackAmountVariable));
        }
    }
}
