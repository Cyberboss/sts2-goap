namespace Vakuu.Engine.Statuses
{
    internal sealed class Strength : IStatus
    {
        public string Name => "Strength";

        public void OnActionTaken(IActionBuilder actionBuilder, Combatant source, Combatant? target)
        {
            if (target == null)
                return;
        }
    }
}
