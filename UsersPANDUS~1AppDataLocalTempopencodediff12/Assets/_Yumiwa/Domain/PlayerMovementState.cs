namespace Yumiwa.Domain
{
    /// <summary>Pure movement-state entity: transition validation plus state timing.</summary>
    public sealed class PlayerMovementState
    {
        public MovementState Current { get; private set; } = MovementState.Idle;
        public float StateTime { get; private set; }
        public bool Grounded { get; private set; } = true;

        public bool CanEnter(MovementState next, float stamina, bool hasAmmo)
        {
            if (next == MovementState.Idle)
                return Current != MovementState.Idle;
            if (!MovementStateTable.TryGet(next, out var data) || stamina < data.StaminaCost)
                return false;
            switch (next)
            {
                case MovementState.Walk:
                    return Current == MovementState.Idle || Current == MovementState.Crouch;
                case MovementState.Sprint:
                    return (Current == MovementState.Walk || Current == MovementState.Idle) && Grounded;
                case MovementState.Crouch:
                    return Grounded && Current != MovementState.Crouch;
                case MovementState.Jump:
                    return Grounded && Current != MovementState.Jump;
                default:
                    return false;
            }
        }

        public void Enter(MovementState next)
        {
            Current = next;
            StateTime = 0f;
        }

        public void SetGrounded(bool grounded)
        {
            Grounded = grounded;
            if (grounded && Current == MovementState.Jump)
                Current = MovementState.Idle;
        }

        public void Tick(float deltaTime) => StateTime += deltaTime;
    }
}
