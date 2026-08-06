using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Levity.Composition.Tests
{
    public sealed class CompositionLifecycleTests
    {
        [Test]
        public void MissingDependencyFailsBeforeAnyModuleStarts()
        {
            var events = new List<string>();
            var composition = new Composition();
            composition.Register(new RecordingModule("consumer", events), typeof(IMissingDependency));

            var exception = Assert.Throws<CompositionException>(() => composition.Start());

            Assert.That(exception.Message, Does.Contain(nameof(IMissingDependency)));
            Assert.That(events, Is.Empty);
        }

        [Test]
        public void ShutdownRunsInReverseOwnershipOrder()
        {
            var events = new List<string>();
            var composition = new Composition();
            composition.Register(new RecordingModule("owner", events));
            composition.Register(new RecordingModule("owned", events));

            composition.Start();
            composition.Shutdown();

            Assert.That(events, Is.EqualTo(new[]
            {
                "initialize:owner",
                "initialize:owned",
                "start:owner",
                "start:owned",
                "shutdown:owned",
                "shutdown:owner"
            }));
        }

        private interface IMissingDependency { }

        private sealed class RecordingModule : ICompositionModule
        {
            private readonly string name;
            private readonly ICollection<string> events;

            public RecordingModule(string name, ICollection<string> events)
            {
                this.name = name;
                this.events = events;
            }

            public void Initialize(ICompositionServices services) => events.Add($"initialize:{name}");

            public void Start() => events.Add($"start:{name}");

            public void Shutdown() => events.Add($"shutdown:{name}");
        }
    }
}
