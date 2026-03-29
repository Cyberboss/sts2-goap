namespace Vakuu.Engine
{
    public abstract class Combatant
    {
        public abstract string HealthState { get; }
        public abstract string MaxHealthState { get; }

        public abstract string BlockGainVariable { get; }

        public abstract string IncomingDamageVariable { get; }
        public abstract string AttackAmountVariable { get; }

        public string StatusState<TStatus>()
            where TStatus : IStatus
            => StatusState(StatusRepository.Get<TStatus>());

        public string StatusState(IStatus status)
            => $"{State.StatusPrefix}{this}: {status.Name}";
    }
}
