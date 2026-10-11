using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for machine orchestration: spend, exits, events.</summary>
    [TestFixture]
    public sealed class MovementStateMachineTests
    {
        private FakeTimeSource clock;
        private EventBus bus;
        private StubAmmoReadModel ammo;
        private MovementStateMachine machine;
        private StaminaService stamina;

        [SetUp]
        public void SetUp()
        {
            clock = new FakeTimeSource();
            bus = new EventBus();
            ammo = new StubAmmoReadModel(true);
            stamina = new StaminaService(new StaminaPool(100f, 10f, 1.5f), clock, bus);
            machine = new MovementStateMachine(
                new PlayerMovementState(), stamina, ammo, bus);
        }

        [Test]
        public void EnterJumpShootSpendsTwentyAndPublishesTransition()
        {
            MovementStateChangedEvent evt = default;
            bus.Subscribe<MovementStateChangedEvent>(e => evt = e);
            machine.SetGrounded(false);
            Assert.IsTrue(machine.RequestEnter(MovementState.JumpShoot));
            Assert.AreEqual(MovementState.JumpShoot, machine.Current);
            Assert.AreEqual(80f, stamina.Current);
            Assert.AreEqual(MovementState.Idle, evt.Previous);
            Assert.AreEqual(MovementState.JumpShoot, evt.Current);
        }

        [Test]
        public void LandingExitsJumpShoot()
        {
            machine.SetGrounded(false);
            machine.RequestEnter(MovementState.JumpShoot);
            machine.SetGrounded(true);
            Assert.AreEqual(MovementState.Idle, machine.Current);
        }

        [Test]
        public void InsufficientStaminaDeniesEntry()
        {
            machine.SetGrounded(false);
            stamina.TrySpend(95f);
            Assert.IsFalse(machine.RequestEnter(MovementState.JumpShoot));
            Assert.AreEqual(MovementState.Idle, machine.Current);
        }
    }
}
