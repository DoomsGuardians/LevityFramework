// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - GameStageConfig 关卡配置

using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 全局关卡配置 ScriptableObject
/// </summary>
[CreateAssetMenu(menuName = "LevityFramework/Stage/GameStageConfig")]
public class GameStageConfig : SerializedScriptableObject
{
    [Title("Stage Configurations"), PropertyOrder(-5)]
    [Searchable]
    [ListDrawerSettings(ShowIndexLabels = true, ShowItemCount = true, ListElementLabelName = nameof(StageConfigItem.StageLabel), DraggableItems = false, ShowFoldout = true, DefaultExpandedState = true, NumberOfItemsPerPage = 8)]
    public List<StageConfigItem> stageConfigList = new();

    [ShowInInspector, PropertyOrder(-4)]
    [DictionaryDrawerSettings(IsReadOnly = true, KeyLabel = "Stage ID", ValueLabel = "Scene Name")]
    private Dictionary<int, string> StageIdToScene => stageConfigList?
        .Where(item => item != null)
        .GroupBy(item => item.stageID)
        .ToDictionary(group => group.Key, group => group.LastOrDefault()?.sceneName ?? string.Empty)
        ?? new Dictionary<int, string>();

    /// <summary>
    /// 根据 StageID 获取关卡配置
    /// </summary>
    public StageConfigItem GetStageItem(int stageID)
    {
        return stageConfigList.Find(i => i.stageID == stageID);
    }
}

/// <summary>
/// 单个关卡的配置项
/// </summary>
[System.Serializable]
[InlineProperty]
[HideReferenceObjectPicker]
public class StageConfigItem
{
    [ShowInInspector, PropertyOrder(-10)]
    [FoldoutGroup("Basic Info", Expanded = true)]
    [LabelText("Summary"), PropertyTooltip("Stage ID / Game Mode / Scene")]
    private string StageSummary => $"{stageID} | {gameMode} | {sceneName}";

    [FoldoutGroup("Basic Info")]
    [LabelText("Stage ID"), LabelWidth(110)]
    public int stageID;

    [FoldoutGroup("Basic Info")]
    [LabelText("Scene Name"), LabelWidth(110)]
    public string sceneName;

    [FoldoutGroup("Basic Info")]
    [LabelText("Game Mode"), LabelWidth(110)]
    public GameMode gameMode;

    [FoldoutGroup("Basic Info")]
    [LabelText("Description"), TextArea]
    public string stageDescription;

    [FoldoutGroup("Content"), ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, ShowIndexLabels = true)]
    [LabelText("Managers"), AssetsOnly]
    public GameObject[] GameMangers;

    [FoldoutGroup("Content")]
    [LabelText("Base Window"), AssetsOnly]
    [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
    public GameObject UIWindowBase;

    [FoldoutGroup("Content")]
    [InfoBox("已知未消费字段：当前 StageSystem 不读取或应用 RoleConfig。填写此项不会产生运行时行为。", InfoMessageType.Warning)]
    [LabelText("Role Config"), InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
    public ScriptableObject RoleConfig;

    [FoldoutGroup("Content")]
    [InfoBox("已知未消费字段：当前 StageSystem 不读取 preLoadItems，也不会执行资产预加载。", InfoMessageType.Warning)]
    [LabelText("Preload Assets"), InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
    public ScriptableObject preLoadItems;

    [FoldoutGroup("Camera & FX")]
    [LabelText("Virtual Camera"), AssetsOnly]
    [PreviewField(Alignment = ObjectFieldAlignment.Center)]
    [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Foldout)]
    public GameObject camera;

#if NANINOVEL
    [FoldoutGroup("Narrative"), InlineProperty, HideLabel]
    public NaninovelStageFlow naninovelFlow = new();
#endif

    public string StageLabel => string.IsNullOrWhiteSpace(sceneName)
        ? $"Stage {stageID}"
        : $"{stageID:000} - {sceneName}";
}

#if NANINOVEL
/// <summary>
/// Naninovel 剧情期间的相机模式
/// </summary>
public enum NaniCameraMode
{
    /// <summary>替换：关闭主相机，只渲染 Naninovel 相机（纯 AVG 模式）</summary>
    Replace,
    /// <summary>叠加：主相机保留，Naninovel 相机叠加渲染（剧情与游戏世界并存）</summary>
    Overlay
}

/// <summary>
/// Naninovel 剧情流程配置
/// </summary>
[System.Serializable]
[InlineProperty]
public class NaninovelStageFlow
{
    [LabelText("Auto Play On Enter"), LabelWidth(160)]
    public bool autoPlayOnEnter;

    [LabelText("Camera Mode"), LabelWidth(160)]
    [Tooltip("Replace: 关闭主相机，仅渲染 Naninovel 相机（纯 AVG）。Overlay: 主相机保留，Naninovel 相机叠加（剧情与游戏并存）。")]
    public NaniCameraMode cameraMode = NaniCameraMode.Replace;

    [LabelText("Use AVG Camera"), LabelWidth(160)]
    [Tooltip("Use the Naninovel AVG camera instead of the one defined in the stage config.")]
    public bool useNaniCamera = false;

    [LabelText("Script Name"), LabelWidth(160)]
    [Tooltip("Naninovel script resource name used when auto playing.")]
    public string scriptName;

    [LabelText("Start Label"), LabelWidth(160)]
    [Tooltip("Optional: label to start playback from, leave empty to start from beginning.")]
    public string startLabel;

    [LabelText("Wait For Completion"), LabelWidth(160)]
    [Tooltip("Wait for the Naninovel script to finish before restoring gameplay input.")]
    public bool waitForCompletion = true;

    [LabelText("Pause Gameplay Input"), LabelWidth(160)]
    [Tooltip("Pause gameplay input while the script is playing.")]
    public bool pauseGameplayInput = true;
}
#endif
