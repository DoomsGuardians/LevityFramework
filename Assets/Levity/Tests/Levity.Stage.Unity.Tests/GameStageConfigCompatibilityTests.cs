using System;
using System.Collections;
using System.Reflection;
using Levity.Stage.Compatibility;
using NUnit.Framework;
using UnityEngine;

namespace Levity.Stage.Tests.Unity
{
    public sealed class GameStageConfigCompatibilityTests
    {
        [Test]
        public void LegacyIntegerBootResolvesConfiguredStrongStageIdWithMigrationDiagnostic()
        {
            var configType = Type.GetType("GameStageConfig, Assembly-CSharp", true);
            var itemType = Type.GetType("StageConfigItem, Assembly-CSharp", true);
            var config = ScriptableObject.CreateInstance(configType);
            try
            {
                var item = Activator.CreateInstance(itemType);
                itemType.GetField("stageID").SetValue(item, 7);
                itemType.GetField("stageId").SetValue(item, "mission");
                itemType.GetField("sceneName").SetValue(item, "Scenes/Mission");
                ((IList)configType.GetField("stageConfigList").GetValue(config)).Add(item);
                var legacyEntry = configType.GetMethod("ResolveLegacyStage");

                var result = legacyEntry.Invoke(config, new object[] { 7 });
                var descriptor = result.GetType().GetProperty("Descriptor").GetValue(result);
                var resolvedId = descriptor.GetType().GetProperty("Id").GetValue(descriptor);
                var obsolete = legacyEntry.GetCustomAttribute<ObsoleteAttribute>();

                Assert.That(resolvedId, Is.EqualTo(new StageId("mission")));
                Assert.That(obsolete.Message, Does.Contain("StageId"));
                Assert.That(obsolete.Message, Does.Contain("CreateStageRegistry"));
                var legacyLoad = Type.GetType("StageSystem, Assembly-CSharp", true)
                    .GetMethod("LoadStage", new[] { typeof(int), typeof(Action), typeof(int) });
                Assert.That(legacyLoad.GetCustomAttribute<ObsoleteAttribute>().Message,
                    Does.Contain("StageId"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ExistingConfigurationWithoutStrongIdUsesStableLegacyMapping()
        {
            var configType = Type.GetType("GameStageConfig, Assembly-CSharp", true);
            var itemType = Type.GetType("StageConfigItem, Assembly-CSharp", true);
            var config = ScriptableObject.CreateInstance(configType);
            try
            {
                var item = Activator.CreateInstance(itemType);
                itemType.GetField("stageID").SetValue(item, 3);
                itemType.GetField("sceneName").SetValue(item, "Scenes/Legacy");
                ((IList)configType.GetField("stageConfigList").GetValue(config)).Add(item);

                var result = configType.GetMethod("ResolveLegacyStage")
                    .Invoke(config, new object[] { 3 });
                var descriptor = result.GetType().GetProperty("Descriptor").GetValue(result);

                Assert.That(descriptor.GetType().GetProperty("Id").GetValue(descriptor),
                    Is.EqualTo(new StageId("legacy-3")));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
