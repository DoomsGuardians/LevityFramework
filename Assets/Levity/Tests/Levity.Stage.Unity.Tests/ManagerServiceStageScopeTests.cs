using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Levity.Stage.Tests.Unity
{
    public sealed class ManagerServiceStageScopeTests
    {
        [Test]
        public void SceneExitReleasesScopedManagerAndInvalidatesItsLease()
        {
            var managerServiceType = Type.GetType("ManagerService, Assembly-CSharp", true);
            var managerType = Type.GetType("ScopedManagerTestProbe, Assembly-CSharp", true);
            var service = Activator.CreateInstance(managerServiceType);
            managerServiceType.GetMethod("OnInit").Invoke(service, null);
            var scope = new StageScope(new StageId("mission"));
            managerServiceType.GetMethod("BindStageScope").Invoke(service, new object[] { scope });
            var gameObject = new GameObject("Scoped Manager Test Probe");

            try
            {
                var manager = gameObject.AddComponent(managerType);
                var register = managerServiceType.GetMethods()
                    .Single(method =>
                        method.Name == "RegisterManager" &&
                        method.IsGenericMethodDefinition &&
                        method.GetParameters().Length == 2)
                    .MakeGenericMethod(managerType);
                var lease = register.Invoke(service, new object[] { manager, scope });

                managerServiceType.GetMethod("OnSceneExit").Invoke(service, null);

                Assert.That(managerType.GetProperty("ExitCount").GetValue(manager), Is.EqualTo(1));
                Assert.That(managerType.GetProperty("UnInitCount").GetValue(manager), Is.EqualTo(1));
                var invocation = Assert.Throws<TargetInvocationException>(() =>
                    lease.GetType().GetProperty("Value").GetValue(lease));
                Assert.That(invocation.InnerException, Is.TypeOf<ReleasedStageManagerAccessException>());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void LegacyManagerEntryPointsCarryActionableMigrationDiagnostics()
        {
            var managerServiceType = Type.GetType("ManagerService, Assembly-CSharp", true);
            var legacyRegister = managerServiceType.GetMethods()
                .Single(method =>
                    method.Name == "RegisterManager" &&
                    !method.IsGenericMethod &&
                    method.GetParameters().Length == 1);
            var legacyResolve = managerServiceType.GetMethods()
                .Single(method =>
                    method.Name == "GetManager" &&
                    method.IsGenericMethodDefinition);

            var registerDiagnostic = legacyRegister.GetCustomAttribute<ObsoleteAttribute>();
            var resolveDiagnostic = legacyResolve.GetCustomAttribute<ObsoleteAttribute>();

            Assert.That(registerDiagnostic.Message, Does.Contain("StageScope"));
            Assert.That(resolveDiagnostic.Message, Does.Contain("GetManagerLease"));
        }

        [Test]
        public void CandidateMayRegisterSameManagerTypeBeforePreviousStageReleases()
        {
            var managerServiceType = Type.GetType("ManagerService, Assembly-CSharp", true);
            var managerType = Type.GetType("ScopedManagerTestProbe, Assembly-CSharp", true);
            var service = Activator.CreateInstance(managerServiceType);
            managerServiceType.GetMethod("OnInit").Invoke(service, null);
            var previousScope = new StageScope(new StageId("menu"));
            var candidateScope = new StageScope(new StageId("mission"));
            var previousObject = new GameObject("Previous Manager Test Probe");
            var candidateObject = new GameObject("Candidate Manager Test Probe");

            try
            {
                var registerDefinition = managerServiceType.GetMethods()
                    .Single(method =>
                        method.Name == "RegisterManager" &&
                        method.IsGenericMethodDefinition &&
                        method.GetParameters().Length == 2);
                var register = registerDefinition.MakeGenericMethod(managerType);
                var previous = previousObject.AddComponent(managerType);
                var candidate = candidateObject.AddComponent(managerType);
                register.Invoke(service, new object[] { previous, previousScope });
                register.Invoke(service, new object[] { candidate, candidateScope });
                managerServiceType.GetMethod("BindStageScope").Invoke(
                    service,
                    new object[] { candidateScope });

                previousScope.ReleaseAsync().GetAwaiter().GetResult();

                var resolve = managerServiceType.GetMethod("GetManagerLease")
                    .MakeGenericMethod(managerType);
                var candidateLease = resolve.Invoke(service, null);
                var resolved = candidateLease.GetType().GetProperty("Value").GetValue(candidateLease);
                Assert.That(resolved, Is.SameAs(candidate));
                Assert.That(managerType.GetProperty("ExitCount").GetValue(previous), Is.EqualTo(1));
                Assert.That(managerType.GetProperty("ExitCount").GetValue(candidate), Is.Zero);
            }
            finally
            {
                candidateScope.ReleaseAsync().GetAwaiter().GetResult();
                UnityEngine.Object.DestroyImmediate(previousObject);
                UnityEngine.Object.DestroyImmediate(candidateObject);
            }
        }
    }
}
