using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Levity.Editor.ProjectSetup
{
    /// <summary>Applies and validates the checked-in Narrative Action reference baseline.</summary>
    public static class NarrativeActionProjectSetup
    {
        public const string InputActionsPath = "Assets/Input/Levity.inputactions";
        public const string BootstrapScenePath = "Assets/Scenes/GameRoot.unity";
        public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        public const string RenderPipelinePath = "Assets/Settings/Universal Render Pipeline Asset.asset";

        private const string InputReferencesFolder = "Assets/Settings/Input References";
        private const string NaninovelConfigurationFolder =
            "Assets/NaninovelData/Resources/Naninovel/Configuration";

        private static readonly string[] RequiredMaps =
            { "Gameplay", "UI", "Narrative", "System", "Debug" };

        [MenuItem("Tools/Levity/Project Setup/Apply Narrative Action Baseline")]
        public static void Apply()
        {
            AssetDatabase.ImportAsset(InputActionsPath, ImportAssetOptions.ForceUpdate);
            ConfigurePlayer();
            ConfigureRendering();
            ConfigureBuildScenes();
            ConfigureNaninovel();
            ConfigureBootstrapEventSystem();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var failures = Validate();
            if (failures.Count > 0)
                throw new InvalidOperationException(
                    "Narrative Action setup is invalid:\n- " + string.Join("\n- ", failures));

            Debug.Log("Levity Narrative Action baseline applied and validated.");
        }

        [MenuItem("Tools/Levity/Project Setup/Validate Narrative Action Baseline")]
        public static void ValidateFromMenu()
        {
            var failures = Validate();
            if (failures.Count == 0)
            {
                Debug.Log("Levity Narrative Action baseline is valid.");
                return;
            }

            foreach (var failure in failures) Debug.LogError(failure);
            throw new InvalidOperationException($"Narrative Action baseline has {failures.Count} error(s).");
        }

        /// <summary>Command-line entry point for CI and clean-checkout verification.</summary>
        public static void ApplyAndValidate() => Apply();

        /// <summary>Returns actionable validation failures; an empty list means the baseline is valid.</summary>
        public static IReadOnlyList<string> Validate()
        {
            var failures = new List<string>();
            ValidateInput(failures);
            ValidateProjectSettings(failures);
            ValidateRendering(failures);
            ValidateBuildScenes(failures);
            ValidateNaninovel(failures);
            ValidateSceneOwnership(failures);
            return failures;
        }

        private static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Levity";
            PlayerSettings.productName = "LevityFramework";
            PlayerSettings.runInBackground = true;
        }

        private static void ConfigureRendering()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(RenderPipelinePath);
            if (pipeline == null)
                throw new InvalidOperationException($"Missing URP asset at '{RenderPipelinePath}'.");

            GraphicsSettings.defaultRenderPipeline = pipeline;
            var originalLevel = QualitySettings.GetQualityLevel();
            for (var index = 0; index < QualitySettings.names.Length; index++)
            {
                QualitySettings.SetQualityLevel(index, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(originalLevel, false);
        }

        private static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScenePath, true),
                new EditorBuildSettingsScene(MainMenuScenePath, true)
            };
        }

        private static void ConfigureNaninovel()
        {
            SetBoolean("EngineConfiguration.asset", "SceneIndependent", true);
            SetBoolean("EngineConfiguration.asset", "InitializeOnApplicationLoad", false);
            SetBoolean("InputConfiguration.asset", "SpawnEventSystem", false);
            SetBoolean("InputConfiguration.asset", "SpawnInputModule", false);
            SetBoolean("InputConfiguration.asset", "ProcessLegacyBindings", false);
            SetBoolean("CameraConfiguration.asset", "UseUICamera", false);

            var input = LoadConfiguration("InputConfiguration.asset");
            var serialized = new SerializedObject(input);
            var property = serialized.FindProperty("InputActions");
            if (property == null)
                throw new InvalidOperationException("Naninovel InputConfiguration has no InputActions property.");
            property.objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(input);
        }

        private static void ConfigureBootstrapEventSystem()
        {
            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            var systems = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (systems.Length > 1)
                throw new InvalidOperationException(
                    $"Bootstrap contains {systems.Length} EventSystems. Keep exactly one project-owned EventSystem.");

            var eventSystem = systems.FirstOrDefault();
            if (eventSystem == null)
            {
                var owner = GameObject.Find("GameRoot");
                if (owner == null)
                    throw new InvalidOperationException("Bootstrap scene must contain a GameRoot object.");
                var child = new GameObject("EventSystem", typeof(EventSystem));
                child.transform.SetParent(owner.transform, false);
                eventSystem = child.GetComponent<EventSystem>();
            }

            var standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null) UnityEngine.Object.DestroyImmediate(standalone);
            var module = eventSystem.GetComponent<InputSystemUIInputModule>() ??
                         eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            module.actionsAsset = actions;
            module.move = LoadOrCreateReference(actions, "UI/Navigate");
            module.submit = LoadOrCreateReference(actions, "UI/Submit");
            module.cancel = LoadOrCreateReference(actions, "UI/Cancel");
            module.point = LoadOrCreateReference(actions, "UI/Point");
            module.leftClick = LoadOrCreateReference(actions, "UI/Click");
            module.scrollWheel = LoadOrCreateReference(actions, "UI/ScrollWheel");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(previous) && previous != BootstrapScenePath && File.Exists(previous))
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }

        private static InputActionReference LoadOrCreateReference(InputActionAsset asset, string actionPath)
        {
            EnsureAssetFolder(InputReferencesFolder);
            var fileName = actionPath.Replace('/', '-') + ".asset";
            var path = $"{InputReferencesFolder}/{fileName}";
            var reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
            if (reference == null)
            {
                reference = InputActionReference.Create(asset.FindAction(actionPath, true));
                reference.name = Path.GetFileNameWithoutExtension(fileName);
                AssetDatabase.CreateAsset(reference, path);
            }
            else
            {
                reference.Set(asset.FindAction(actionPath, true));
                reference.name = Path.GetFileNameWithoutExtension(fileName);
                EditorUtility.SetDirty(reference);
            }
            return reference;
        }

        private static void EnsureAssetFolder(string path)
        {
            var segments = path.Split('/');
            var current = segments[0];
            for (var index = 1; index < segments.Length; index++)
            {
                var next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private static UnityEngine.Object LoadConfiguration(string fileName)
        {
            var path = $"{NaninovelConfigurationFolder}/{fileName}";
            return AssetDatabase.LoadMainAssetAtPath(path) ??
                   throw new InvalidOperationException($"Missing Naninovel configuration '{path}'.");
        }

        private static void SetBoolean(string fileName, string propertyName, bool value)
        {
            var configuration = LoadConfiguration(fileName);
            var serialized = new SerializedObject(configuration);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"{fileName} has no '{propertyName}' property.");
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(configuration);
        }

        private static void ValidateInput(ICollection<string> failures)
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
            {
                failures.Add($"Missing canonical Input Actions asset at '{InputActionsPath}'.");
                return;
            }
            foreach (var map in RequiredMaps.Where(map => actions.FindActionMap(map) == null))
                failures.Add($"Canonical Input Actions asset is missing the '{map}' map.");
        }

        private static void ValidateProjectSettings(ICollection<string> failures)
        {
            var settings = File.ReadAllText("ProjectSettings/ProjectSettings.asset");
            if (!settings.Contains("activeInputHandler: 1"))
                failures.Add("Player Settings must use Input System Package only (Active Input Handling). ");
            if (!PlayerSettings.runInBackground)
                failures.Add("Player Settings must enable Run In Background for deterministic async services.");
        }

        private static void ValidateRendering(ICollection<string> failures)
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(RenderPipelinePath);
            if (pipeline == null || GraphicsSettings.defaultRenderPipeline != pipeline)
                failures.Add("Graphics Settings must own the checked-in URP asset.");
            var quality = File.ReadAllText("ProjectSettings/QualitySettings.asset");
            var configured = quality.Split(new[] { "customRenderPipeline: {fileID: 11400000" },
                StringSplitOptions.None).Length - 1;
            if (configured != QualitySettings.names.Length)
                failures.Add("Every quality level must reference the checked-in URP asset.");
        }

        private static void ValidateBuildScenes(ICollection<string> failures)
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
            if (!scenes.SequenceEqual(new[] { BootstrapScenePath, MainMenuScenePath }))
                failures.Add("Build Scenes must contain enabled GameRoot then MainMenu, in that order.");
        }

        private static void ValidateNaninovel(ICollection<string> failures)
        {
            ValidateBoolean("EngineConfiguration.asset", "InitializeOnApplicationLoad", false, failures);
            ValidateBoolean("EngineConfiguration.asset", "SceneIndependent", true, failures);
            ValidateBoolean("InputConfiguration.asset", "SpawnEventSystem", false, failures);
            ValidateBoolean("InputConfiguration.asset", "SpawnInputModule", false, failures);
            ValidateBoolean("InputConfiguration.asset", "ProcessLegacyBindings", false, failures);
            ValidateBoolean("CameraConfiguration.asset", "UseUICamera", false, failures);

            var serialized = new SerializedObject(LoadConfiguration("InputConfiguration.asset"));
            var input = serialized.FindProperty("InputActions")?.objectReferenceValue;
            if (input != AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath))
                failures.Add("Naninovel must use the canonical Levity Input Actions asset.");
        }

        private static void ValidateBoolean(
            string fileName,
            string propertyName,
            bool expected,
            ICollection<string> failures)
        {
            var serialized = new SerializedObject(LoadConfiguration(fileName));
            var property = serialized.FindProperty(propertyName);
            if (property == null || property.boolValue != expected)
                failures.Add($"Naninovel {fileName}.{propertyName} must be {expected}.");
        }

        private static void ValidateSceneOwnership(ICollection<string> failures)
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                var bootstrap = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
                var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (eventSystems.Length != 1)
                    failures.Add($"Bootstrap must own exactly one EventSystem; found {eventSystems.Length}.");
                else if (eventSystems[0].GetComponent<InputSystemUIInputModule>() == null)
                    failures.Add("Bootstrap EventSystem must use InputSystemUIInputModule.");

                var menu = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
                var cameras = UnityEngine.Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                var listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (cameras.Length != 1) failures.Add($"Main Menu must own exactly one camera; found {cameras.Length}.");
                if (listeners.Length != 1) failures.Add($"Main Menu must own exactly one Audio Listener; found {listeners.Length}.");
            }
            finally
            {
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }
    }
}
