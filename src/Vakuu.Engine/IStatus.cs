namespace Vakuu.Engine
{
    public interface IStatus
    {
        public string Name { get; }

        void OnTurnStart(IActionBuilder actionBuilder, Combatant combatant)
        {
        }

        void OnActionTaken(IActionBuilder actionBuilder, Combatant source, Combatant? target)
        {
        }

        void OnTurnEnd(IActionBuilder actionBuilder, Combatant combatant)
        {
        }
    }
}