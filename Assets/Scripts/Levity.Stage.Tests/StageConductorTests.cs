using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Levity.Stage.Tests
{
    public sealed class StageConductorTests
    {
        [Test]
        public void UnknownTargetFailsValidationAndPreservesTheCurrentStage()
        {
            var current = new RecordingStageHandle(
                new StageDescriptor(new StageId("menu"), "Scenes/Menu"));
            var loader = new RecordingStageLoader();
            var conductor = new StageConductor(new StageRegistry(), loader, current);

            var result = conductor.ChangeAsync(new StageId("missing"))
                .GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(StageChangeStatus.Failed));
            Assert.That(result.Failure.Code, Is.EqualTo(StageChangeFailureCode.StageNotFound));
            Assert.That(result.Failure.Phase, Is.EqualTo(StageChangePhase.Validate));
            Assert.That(conductor.CurrentStage, Is.SameAs(current.Descriptor));
            Assert.That(loader.ValidateCount, Is.Zero);
            Assert.That(current.ReleaseCount, Is.Zero);
        }

        [Test]
        public void UnsafeTargetFailsBeforePreparationAndPreservesTheCurrentStage()
        {
            var target = new StageDescriptor(new StageId("mission"), "Scenes/Mission");
            var registry = new StageRegistry();
            registry.Register(target);
            var current = new RecordingStageHandle(
                new StageDescriptor(new StageId("menu"), "Scenes/Menu"));
            var loader = new RecordingStageLoader
            {
                Validation = StageLoadValidation.Invalid("Additive loading is unavailable.")
            };
            var conductor = new StageConductor(registry, loader, current);

            var result = conductor.ChangeAsync(target.Id).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(StageChangeStatus.Failed));
            Assert.That(result.Failure.Code, Is.EqualTo(StageChangeFailureCode.ValidationFailed));
            Assert.That(result.Failure.Phase, Is.EqualTo(StageChangePhase.Validate));
            Assert.That(result.Failure.Message, Does.Contain("Additive loading"));
            Assert.That(loader.PrepareCount, Is.Zero);
            Assert.That(conductor.CurrentStage, Is.SameAs(current.Descriptor));
        }

        [Test]
        public void PreparationFailureNamesThePhaseAndPreservesTheCurrentStage()
        {
            var target = new StageDescriptor(new StageId("mission"), "Scenes/Mission");
            var registry = new StageRegistry();
            registry.Register(target);
            var current = new RecordingStageHandle(
                new StageDescriptor(new StageId("menu"), "Scenes/Menu"));
            var loader = new RecordingStageLoader
            {
                PrepareException = new System.InvalidOperationException("load failed")
            };
            var conductor = new StageConductor(registry, loader, current);

            var result = conductor.ChangeAsync(target.Id).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(StageChangeStatus.Failed));
            Assert.That(result.Failure.Code, Is.EqualTo(StageChangeFailureCode.PreparationFailed));
            Assert.That(result.Failure.Phase, Is.EqualTo(StageChangePhase.Prepare));
            Assert.That(result.Failure.Exception, Is.SameAs(loader.PrepareException));
            Assert.That(conductor.CurrentStage, Is.SameAs(current.Descriptor));
            Assert.That(current.ReleaseCount, Is.Zero);
        }

        [Test]
        public void MissingPreparedHandleReturnsAPreparationFailure()
        {
            var target = new StageDescriptor(new StageId("mission"), "Scenes/Mission");
            var registry = new StageRegistry();
            registry.Register(target);
            var previous = new RecordingStageHandle(
                new StageDescriptor(new StageId("menu"), "Scenes/Menu"));
            var conductor = new StageConductor(
                registry,
                new RecordingStageLoader { ReturnNullHandle = true },
                previous);

            var result = conductor.ChangeAsync(target.Id).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(StageChangeStatus.Failed));
            Assert.That(result.Failure.Code, Is.EqualTo(StageChangeFailureCode.PreparationFailed));
            Assert.That(result.Failure.Phase, Is.EqualTo(StageChangePhase.Prepare));
            Assert.That(conductor.CurrentStage, Is.SameAs(previous.Descriptor));
        }

        [Test]
        public void ActivationFailureReleasesTheCandidateAndPreservesTheCurrentStage()
        {
            var target = new StageDescriptor(new StageId("mission"), "Scenes/Mission");
            var registry = new StageRegistry();
            registry.Register(target);
            var current = new RecordingStageHandle(
                new StageDescriptor(new StageId("menu"), "Scenes/Menu"));
            var candidate = new RecordingStageHandle(target)
            {
                ActivateException = new System.InvalidOperationException("activate failed")
            };
            var loader = new RecordingStageLoader { PreparedHandle = candidate };
            var conductor = new StageConductor(registry, loader, current);

            var result = conductor.ChangeAsync(target.Id).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(StageChangeStatus.Failed));
            Assert.That(result.Failure.Code, Is.EqualTo(StageChangeFailureCode.ActivationFailed));
            Assert.That(result.Failure.Phase, Is.EqualTo(StageChangePhase.Activate));
            Assert.That(candidate.ActivateCount, Is.EqualTo(1));
            Assert.That(candidate.ReleaseCount, Is.EqualTo(1));
            Assert.That(current.ActivateCount, Is.EqualTo(1));
            Assert.That(current.ReleaseCount, Is.Zero);
            Assert.That(conductor.CurrentStage, Is.SameAs(current.Descriptor));
        }

        [Test]
        public void SuccessfulChangeCommitsTheCandidateThenReleasesThePreviousStage()
        {
            var target = new StageDescriptor(new StageId("mission"), "Scenes/Mission");
            var registry = new StageRegistry();
            registry.Register(target);
            var previous = new RecordingStageHandle(
                new StageDescriptor(new StageId("menu"), "Scenes/Menu"));
            var candidate = new RecordingStageHandle(target);
            var loader = new RecordingStageLoader { PreparedHandle = candidate };
            var conductor = new StageConductor(registry, loader, previous);

            var result = conductor.ChangeAsync(target.Id).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(StageChangeStatus.Completed));
            Assert.That(result.Failure, Is.Null);
            Assert.That(conductor.CurrentStage, Is.SameAs(target));
            Assert.That(candidate.ActivateCount, Is.EqualTo(1));
            Assert.That(candidate.ReleaseCount, Is.Zero);
            Assert.That(previous.ReleaseCount, Is.EqualTo(1));
        }

        [Test]
        public void ConcurrentChangeIsRejectedWhileTheFirstChangeProceeds()
        {
            var firstTarget = new StageDescriptor(new StageId("mission"), "Scenes/Mission");
            var secondTarget = new StageDescriptor(new StageId("results"), "Scenes/Results");
            var registry = new StageRegistry();
            registry.Register(firstTarget);
            registry.Register(secondTarget);
            var activation = new TaskCompletionSource<bool>();
            var candidate = new RecordingStageHandle(firstTarget)
            {
                ActivateCompletion = activation.Task
            };
            var loader = new RecordingStageLoader { PreparedHandle = candidate };
            var conductor = new StageConductor(registry, loader);

            var firstChange = conductor.ChangeAsync(firstTarget.Id);
            var rejected = conductor.ChangeAsync(secondTarget.Id).GetAwaiter().GetResult();

            Assert.That(rejected.Status, Is.EqualTo(StageChangeStatus.Failed));
            Assert.That(rejected.Failure.Code, Is.EqualTo(StageChangeFailureCode.ChangeInProgress));
            Assert.That(rejected.Failure.Phase, Is.EqualTo(StageChangePhase.Acquire));
            Assert.That(loader.PrepareCount, Is.EqualTo(1));
            Assert.That(firstChange.IsCompleted, Is.False);

            activation.SetResult(true);
            Assert.That(firstChange.GetAwaiter().GetResult().Status, Is.EqualTo(StageChangeStatus.Completed));
        }

        [Test]
        public void CancellationDuringActivationReleasesTheCandidateAndPreservesTheCurrentStage()
        {
            var target = new StageDescriptor(new StageId("mission"), "Scenes/Mission");
            var registry = new StageRegistry();
            registry.Register(target);
            var previous = new RecordingStageHandle(
                new StageDescriptor(new StageId("menu"), "Scenes/Menu"));
            var candidate = new RecordingStageHandle(target) { WaitForCancellation = true };
            var loader = new RecordingStageLoader { PreparedHandle = candidate };
            var conductor = new StageConductor(registry, loader, previous);
            var cancellation = new CancellationTokenSource();

            var change = conductor.ChangeAsync(target.Id, cancellation.Token);
            cancellation.Cancel();
            var result = change.GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(StageChangeStatus.Cancelled));
            Assert.That(result.Failure, Is.Null);
            Assert.That(candidate.ReleaseCount, Is.EqualTo(1));
            Assert.That(previous.ActivateCount, Is.EqualTo(1));
            Assert.That(previous.ReleaseCount, Is.Zero);
            Assert.That(conductor.CurrentStage, Is.SameAs(previous.Descriptor));
        }

        [Test]
        public void UnexpectedValidationExceptionReturnsATypedFailure()
        {
            var target = new StageDescriptor(new StageId("mission"), "Scenes/Mission");
            var registry = new StageRegistry();
            registry.Register(target);
            var previous = new RecordingStageHandle(
                new StageDescriptor(new StageId("menu"), "Scenes/Menu"));
            var loader = new RecordingStageLoader
            {
                ValidationException = new System.InvalidOperationException("validation broke")
            };
            var conductor = new StageConductor(registry, loader, previous);

            var result = conductor.ChangeAsync(target.Id).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(StageChangeStatus.Failed));
            Assert.That(result.Failure.Code, Is.EqualTo(StageChangeFailureCode.UnexpectedFailure));
            Assert.That(result.Failure.Phase, Is.EqualTo(StageChangePhase.Validate));
            Assert.That(result.Failure.Exception, Is.SameAs(loader.ValidationException));
            Assert.That(conductor.CurrentStage, Is.SameAs(previous.Descriptor));
        }

        [Test]
        public void PreviousReleaseFailureIsReportedAfterTheCandidateRemainsCommitted()
        {
            var target = new StageDescriptor(new StageId("mission"), "Scenes/Mission");
            var registry = new StageRegistry();
            registry.Register(target);
            var previous = new RecordingStageHandle(
                new StageDescriptor(new StageId("menu"), "Scenes/Menu"))
            {
                ReleaseException = new System.InvalidOperationException("release failed")
            };
            var candidate = new RecordingStageHandle(target);
            var conductor = new StageConductor(
                registry,
                new RecordingStageLoader { PreparedHandle = candidate },
                previous);

            var result = conductor.ChangeAsync(target.Id).GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(StageChangeStatus.Failed));
            Assert.That(result.Failure.Code, Is.EqualTo(StageChangeFailureCode.ReleasePreviousFailed));
            Assert.That(result.Failure.Phase, Is.EqualTo(StageChangePhase.ReleasePrevious));
            Assert.That(result.Failure.Exception, Is.SameAs(previous.ReleaseException));
            Assert.That(conductor.CurrentStage, Is.SameAs(target));
            Assert.That(previous.ReleaseCount, Is.EqualTo(1));
            Assert.That(candidate.ReleaseCount, Is.Zero);
        }

        private sealed class RecordingStageLoader : IStageLoader
        {
            public int ValidateCount { get; private set; }
            public int PrepareCount { get; private set; }
            public StageLoadValidation Validation { get; set; } = StageLoadValidation.Valid();
            public System.Exception ValidationException { get; set; }
            public System.Exception PrepareException { get; set; }
            public IStageHandle PreparedHandle { get; set; }
            public bool ReturnNullHandle { get; set; }

            public StageLoadValidation Validate(StageDescriptor target)
            {
                ValidateCount++;
                if (ValidationException != null) throw ValidationException;
                return Validation;
            }

            public Task<IStageHandle> PrepareAsync(
                StageDescriptor target,
                CancellationToken cancellationToken)
            {
                PrepareCount++;
                if (PrepareException != null)
                    return Task.FromException<IStageHandle>(PrepareException);
                if (ReturnNullHandle) return Task.FromResult<IStageHandle>(null);
                return Task.FromResult(PreparedHandle ?? new RecordingStageHandle(target));
            }
        }

        private sealed class RecordingStageHandle : IStageHandle
        {
            public RecordingStageHandle(StageDescriptor descriptor) => Descriptor = descriptor;

            public StageDescriptor Descriptor { get; }
            public int ActivateCount { get; private set; }
            public int ReleaseCount { get; private set; }
            public System.Exception ActivateException { get; set; }
            public System.Exception ReleaseException { get; set; }
            public Task ActivateCompletion { get; set; }
            public bool WaitForCancellation { get; set; }

            public Task ActivateAsync(CancellationToken cancellationToken)
            {
                ActivateCount++;
                if (WaitForCancellation)
                    return Task.Delay(Timeout.Infinite, cancellationToken);
                if (ActivateCompletion != null) return ActivateCompletion;
                return ActivateException == null
                    ? Task.CompletedTask
                    : Task.FromException(ActivateException);
            }

            public Task ReleaseAsync(CancellationToken cancellationToken)
            {
                ReleaseCount++;
                return ReleaseException == null
                    ? Task.CompletedTask
                    : Task.FromException(ReleaseException);
            }
        }
    }
}
