using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for the movement transition matrix.</summary>
    [TestFixture]
    public sealed class PlayerMovementStateTests
    {
        private static PlayerMovementState Fresh() => new PlayerMovementState();

        [Test]
        public void IdleToJumpShootRequiresTwentyStamina()
        {
            Assert.IsTrue(Fresh().CanEnter(MovementState.JumpShoot, 20f, true));
            Assert.IsFalse(Fresh().CanEnter(MovementState.JumpShoot, 19f, true));
        }

        [Test]
        public void NoAmmoDeniesJumpShootButAllowsIdleExit()
        {
            var state = Fresh();
            Assert.IsFalse(state.CanEnter(MovementState.JumpShoot, 100f, false));
            state.Enter(MovementState.JumpShoot);
            Assert.IsTrue(state.CanEnter(MovementState.Idle, 0f, false));
        }

        [Test]
        public void EnterResetsStateTimer()
        {
            var state = Fresh();
            state.Enter(MovementState.JumpShoot);
            state.Tick(0.4f);
            state.Enter(MovementState.Idle);
            Assert.AreEqual(0f, state.StateTime);
            Assert.AreEqual(MovementState.Idle, state.Current);
        }
    }
}
