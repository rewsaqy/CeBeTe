using NUnit.Framework;
using Yumiwa.Domain;

namespace Yumiwa.Application.Tests
{
    /// <summary>EditMode tests for use-case routing against a faked machine.</summary>
    [TestFixture]
    public sealed class MovementUseCaseTests
    {
        [Test]
        public void JumpShootExecutes()
        {
            var machine = new FakeMovementStateMachine();
            new ExecuteJumpShootUseCase(machine).Execute();
            CollectionAssert.AreEqual(
                new[] { MovementState.JumpShoot }, machine.Requested);
        }
    }
}
