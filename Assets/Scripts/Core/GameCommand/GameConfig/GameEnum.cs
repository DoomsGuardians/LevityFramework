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
/// UI 窗口层级（6层设计）
/// 每层预留 100 个 Order In Layer 空间
/// </summary>
public enum UILayer
{
    /// <summary>场景层: 0-99 (3D UI、世界空间 UI)</summary>
    Scene = 0,
    /// <summary>背景层: 100-199 (全屏背景、天气效果)</summary>
    Background = 100,
    /// <summary>普通层: 200-299 (主要游戏 UI、HUD)</summary>
    Normal = 200,
    /// <summary>信息层: 300-399 (状态栏、提示信息)</summary>
    Info = 300,
    /// <summary>顶层: 400-499 (弹窗、对话框)</summary>
    Top = 400,
    /// <summary>提示层: 500-599 (Toast、Loading、系统提示)</summary>
    Tip = 500
}

/// <summary>
/// UI 窗口层级（旧版，保留向后兼容）
/// </summary>
[System.Obsolete("请使用 UILayer 枚举，提供更细粒度的分层控制")]
public enum WindowLayer
{
    Null,
    Base,   // -> UILayer.Normal
    Pop,    // -> UILayer.Top
    Top     // -> UILayer.Tip
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
