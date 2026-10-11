using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>Application state machine: rule checks, stamina spend, landing exits.</summary>
    public sealed class MovementStateMachine : IMovementStateMachine
    {
        private readonly PlayerMovementState entity;
        private readonly IStaminaService stamina;
        private readonly IAmmoReadModel ammo;
        private readonly IEventBus bus;

        public MovementStateMachine(
            PlayerMovementState entity,
            IStaminaService stamina,
            IAmmoReadModel ammo,
            IEventBus bus)
        {
            this.entity = entity;
            this.stamina = stamina;
            this.ammo = ammo;
            this.bus = bus;
        }

        public MovementState Current => entity.Current;
        public float StateTime => entity.StateTime;
        public bool Grounded => entity.Grounded;

        public bool RequestEnter(MovementState next)
        {
            if (Current == next)
                return true;
            if (!entity.CanEnter(next, stamina.Current, ammo.HasAmmo))
                return false;
            float cost = MovementStateTable.Get(next).StaminaCost;
            if (!stamina.TrySpend(cost))
                return false;
            TransitionTo(next);
            return true;
        }

        public void RequestExit()
        {
            if (Current == MovementState.Idle)
                return;
            TransitionTo(MovementState.Idle);
        }

        public void SetGrounded(bool grounded)
        {
            entity.SetGrounded(grounded);
            if (grounded && Current == MovementState.JumpShoot)
                TransitionTo(MovementState.Idle);
        }

        public void Tick(float deltaTime)
        {
            entity.Tick(deltaTime);
            stamina.Tick(deltaTime);
        }

        private void TransitionTo(MovementState next)
        {
            var previous = entity.Current;
            entity.Enter(next);
            bus.Publish(new MovementStateChangedEvent(previous, next));
        }
    }
}
