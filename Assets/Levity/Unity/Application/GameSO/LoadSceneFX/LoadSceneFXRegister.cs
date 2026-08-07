// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - LoadSceneFXRegister 场景转换特效注册表

using UnityEngine;

/// <summary>
/// 场景转换特效注册表 ScriptableObject
/// 配置进入和退出场景时的屏幕特效
/// </summary>
[CreateAssetMenu(fileName = "LoadSceneFXRegister", menuName = "LevityFramework/Stage/LoadSceneFXRegister")]
public class LoadSceneFXRegister : ScriptableObject
{
    [Tooltip("进入场景时执行的特效命令")]
    public CommandLogicSOBase enterStageLogic;

    [Tooltip("退出场景时执行的特效命令")]
    public CommandLogicSOBase exitStateLogic;
}
