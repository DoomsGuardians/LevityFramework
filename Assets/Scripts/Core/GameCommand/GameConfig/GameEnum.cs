// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - GameEnum 通用枚举定义

/// <summary>
/// 游戏模式枚举（可根据项目扩展）
/// </summary>
public enum GameMode
{
    Null,
    GameStart,
    GamePlay,
    Training,
    Narrative
}

/// <summary>
/// 游戏角色类型枚举（可根据项目扩展）
/// </summary>
public enum GameRole
{
    Null,
    Player
}

/// <summary>
/// AI 类型枚举（可根据项目扩展）
/// </summary>
public enum GameAI
{
    Null,
}

/// <summary>
/// UI 窗口层级
/// </summary>
public enum WindowLayer
{
    Null,
    Base,   // 0-99
    Pop,    // 100-199
    Top     // 200-300
}

/// <summary>
/// 音频类型
/// </summary>
public enum AudioType
{
    Null,
    SFX,    // 音效
    BGM,    // 背景音乐
}
