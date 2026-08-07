using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Levity.Stage.Tests
{
    public sealed class StageScopeTests
    {
        [Test]
        public void ReleaseRunsManagersInReverseRegistrationOrderExactlyOnce()
        {
            var released = new List<string>();
            var scope = new StageScope(new StageId("mission"));
            scope.Register(new FirstManager(), _ =>
            {
                released.Add("first");
                return Task.CompletedTask;
            });
            scope.Register(new SecondManager(), _ =>
            {
                released.Add("second");
                return Task.CompletedTask;
            });

            scope.ReleaseAsync().GetAwaiter().GetResult();
            scope.ReleaseAsync().GetAwaiter().GetResult();

            Assert.That(released, Is.EqualTo(new[] { "second", "first" }));
        }

        [Test]
        public void ReleasedLeaseNamesTheManagerTypeAndOwningStage()
        {
            var stageId = new StageId("mission");
            var scope = new StageScope(stageId);
            var lease = scope.Register(new FirstManager(), _ => Task.CompletedTask);
            scope.ReleaseAsync().GetAwaiter().GetResult();

            var exception = Assert.Throws<ReleasedStageManagerAccessException>(() => _ = lease.Value);

            Assert.That(exception.ManagerType, Is.EqualTo(typeof(FirstManager)));
            Assert.That(exception.StageId, Is.EqualTo(stageId));
            Assert.That(exception.Message, Does.Contain(nameof(FirstManager)));
            Assert.That(exception.Message, Does.Contain("mission"));
        }

        private sealed class FirstManager { }
        private sealed class SecondManager { }
    }
}
