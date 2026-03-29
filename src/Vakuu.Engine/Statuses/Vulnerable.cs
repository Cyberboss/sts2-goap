namespace Vakuu.Engine.Statuses
{
    internal sealed class Vulnerable : IStatus
    {
        public string Name => "Vulnerable";

        public void OnActionTaken(IActionBuilder actionBuilder, Combatant source, Combatant? target)
        {
            if (target == null)
                return;

            actionBuilder.Reduce(
                new Reducer(
                    (variables, input) => input * ((variables[target.StatusState(this)] > 0) ? 1.5f : 1.0f),
                    target.IncomingDamageVariable));
        }
    }
}
