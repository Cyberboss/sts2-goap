namespace Vakuu.Engine.Relics
{
    sealed class BurningBlood : IRelic
    {
        static readonly Reducer Reducer = new Reducer(
            input => input + 6,
            State.PlayerCurrentHealth);

        public string Name => "Burning Blood";

        public void OnCombatEnd(IActionBuilder actionBuilder)
            => actionBuilder.Reduce(Reducer);
    }
}
