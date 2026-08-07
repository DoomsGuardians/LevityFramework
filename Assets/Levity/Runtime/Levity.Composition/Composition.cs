using System;
using System.Collections.Generic;

namespace Levity.Composition
{
    /// <summary>
    /// Runs registered modules through deterministic Initialize, Start, and Shutdown phases.
    /// </summary>
    public sealed class Composition : ICompositionServices, IDisposable
    {
        private readonly List<Registration> registrations = new List<Registration>();
        private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private int initializedCount;
        private bool started;
        private bool shutdown;

        /// <summary>Registers a module for lifecycle ownership without exposing it as a service.</summary>
        public void Register(ICompositionModule module, params Type[] requiredServices)
        {
            if (module == null) throw new ArgumentNullException(nameof(module));
            Register(null, module, requiredServices);
        }

        /// <summary>Registers a module behind the service type consumed by dependants.</summary>
        public void RegisterService<TService>(TService module, params Type[] requiredServices)
            where TService : class, ICompositionModule
        {
            Register(typeof(TService), module, requiredServices);
        }

        /// <summary>Validates dependencies, then initializes and starts every module in registration order.</summary>
        public void Start()
        {
            EnsureCanStart();
            ValidateDependencies();

            try
            {
                for (var index = 0; index < registrations.Count; index++)
                {
                    registrations[index].Module.Initialize(this);
                    initializedCount++;
                }

                for (var index = 0; index < registrations.Count; index++)
                {
                    registrations[index].Module.Start();
                }

                started = true;
            }
            catch
            {
                ShutdownInitializedModules();
                shutdown = true;
                throw;
            }
        }

        /// <summary>Returns a required registered service.</summary>
        public TService Get<TService>() where TService : class
        {
            if (services.TryGetValue(typeof(TService), out var service))
            {
                return (TService)service;
            }

            throw new CompositionException($"Required service {typeof(TService).FullName} is not registered.");
        }

        /// <summary>Shuts initialized modules down once, in reverse registration order.</summary>
        public void Shutdown()
        {
            if (shutdown) return;
            ShutdownInitializedModules();
            shutdown = true;
            started = false;
        }

        public void Dispose() => Shutdown();

        private void Register(Type serviceType, ICompositionModule module, Type[] requiredServices)
        {
            if (started || initializedCount > 0 || shutdown)
            {
                throw new InvalidOperationException("Modules must be registered before Composition.Start().");
            }

            if (serviceType != null && services.ContainsKey(serviceType))
            {
                throw new CompositionException($"Service {serviceType.FullName} is already registered.");
            }

            if (serviceType != null)
            {
                services.Add(serviceType, module);
            }
            registrations.Add(new Registration(module, requiredServices ?? Array.Empty<Type>()));
        }

        private void ValidateDependencies()
        {
            foreach (var registration in registrations)
            {
                foreach (var dependency in registration.RequiredServices)
                {
                    if (dependency == null)
                    {
                        throw new CompositionException($"{registration.Module.GetType().FullName} declares a null dependency.");
                    }

                    if (!services.ContainsKey(dependency))
                    {
                        throw new CompositionException(
                            $"{registration.Module.GetType().FullName} requires {dependency.FullName}, but it is not registered.");
                    }
                }
            }
        }

        private void EnsureCanStart()
        {
            if (started || initializedCount > 0)
            {
                throw new InvalidOperationException("Composition has already started.");
            }

            if (shutdown)
            {
                throw new InvalidOperationException("A shut down Composition cannot be restarted.");
            }
        }

        private void ShutdownInitializedModules()
        {
            for (var index = initializedCount - 1; index >= 0; index--)
            {
                registrations[index].Module.Shutdown();
            }

            initializedCount = 0;
        }

        private sealed class Registration
        {
            public Registration(ICompositionModule module, Type[] requiredServices)
            {
                Module = module;
                RequiredServices = requiredServices;
            }

            public ICompositionModule Module { get; }
            public Type[] RequiredServices { get; }
        }
    }
}
