namespace Vakuu.Engine
{
    public interface IRelic
    {
        void OnCombatEnd(IActionBuilder actionBuilder)
        {
        }

        void OnTurnStart(IActionBuilder actionBuilder)
        {
        }

        void OnActionTaken(IActionBuilder actionBuilder)
        {
        }

        void OnTurnEnd(IActionBuilder actionBuilder)
        {
        }
    }
}