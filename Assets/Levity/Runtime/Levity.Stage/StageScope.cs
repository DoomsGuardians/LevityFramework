using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Levity.Stage
{
    public sealed class ReleasedStageManagerAccessException : InvalidOperationException
    {
        public ReleasedStageManagerAccessException(Type managerType, StageId stageId)
            : base($"Manager '{managerType.FullName}' belonged to released Stage '{stageId}'.")
        {
            ManagerType = managerType;
            StageId = stageId;
        }

        public Type ManagerType { get; }
        public StageId StageId { get; }
    }

    public sealed class StageManagerNotRegisteredException : InvalidOperationException
    {
        public StageManagerNotRegisteredException(Type managerType, StageId stageId)
            : base($"Manager '{managerType.FullName}' is not registered in Stage '{stageId}'.")
        {
            ManagerType = managerType;
            StageId = stageId;
        }

        public Type ManagerType { get; }
        public StageId StageId { get; }
    }

    public sealed class DuplicateStageManagerRegistrationException : InvalidOperationException
    {
        public DuplicateStageManagerRegistrationException(Type managerType, StageId stageId)
            : base($"Manager '{managerType.FullName}' is already registered in Stage '{stageId}'.")
        {
            ManagerType = managerType;
            StageId = stageId;
        }

        public Type ManagerType { get; }
        public StageId StageId { get; }
    }

    public sealed class StageManagerLease<T> where T : class
    {
        private readonly StageScope scope;
        private readonly T manager;

        internal StageManagerLease(StageScope scope, T manager)
        {
            this.scope = scope;
            this.manager = manager;
        }

        public T Value
        {
            get
            {
                scope.EnsureActive(typeof(T));
                return manager;
            }
        }
    }

    public sealed class StageScope
    {
        private readonly object sync = new object();
        private readonly Dictionary<Type, IManagerRegistration> registrations =
            new Dictionary<Type, IManagerRegistration>();
        private readonly List<IManagerRegistration> releaseOrder =
            new List<IManagerRegistration>();
        private bool released;

        public StageScope(StageId stageId) => StageId = stageId;

        public StageId StageId { get; }

        public StageManagerLease<T> Register<T>(T manager, Func<T, Task> release)
            where T : class
        {
            if (manager == null) throw new ArgumentNullException(nameof(manager));
            if (release == null) throw new ArgumentNullException(nameof(release));

            lock (sync)
            {
                EnsureActive(typeof(T));
                if (registrations.ContainsKey(typeof(T)))
                    throw new DuplicateStageManagerRegistrationException(typeof(T), StageId);

                var registration = new ManagerRegistration<T>(manager, release);
                registrations.Add(typeof(T), registration);
                releaseOrder.Add(registration);
                return new StageManagerLease<T>(this, manager);
            }
        }

        public StageManagerLease<T> Resolve<T>() where T : class
        {
            lock (sync)
            {
                EnsureActive(typeof(T));
                if (!registrations.TryGetValue(typeof(T), out var registration))
                    throw new StageManagerNotRegisteredException(typeof(T), StageId);

                return new StageManagerLease<T>(this, ((ManagerRegistration<T>)registration).Manager);
            }
        }

        public async Task ReleaseAsync()
        {
            IManagerRegistration[] pending;
            lock (sync)
            {
                if (released) return;
                released = true;
                pending = releaseOrder.ToArray();
                registrations.Clear();
                releaseOrder.Clear();
            }

            List<Exception> failures = null;
            for (var index = pending.Length - 1; index >= 0; index--)
            {
                try
                {
                    await pending[index].ReleaseAsync();
                }
                catch (Exception exception)
                {
                    if (failures == null) failures = new List<Exception>();
                    failures.Add(exception);
                }
            }

            if (failures != null) throw new AggregateException(failures);
        }

        internal void EnsureActive(Type managerType)
        {
            lock (sync)
            {
                if (released)
                    throw new ReleasedStageManagerAccessException(managerType, StageId);
            }
        }

        private interface IManagerRegistration
        {
            Task ReleaseAsync();
        }

        private sealed class ManagerRegistration<T> : IManagerRegistration where T : class
        {
            private readonly Func<T, Task> release;

            public ManagerRegistration(T manager, Func<T, Task> release)
            {
                Manager = manager;
                this.release = release;
            }

            public T Manager { get; }

            public Task ReleaseAsync() => release(Manager);
        }
    }
}
