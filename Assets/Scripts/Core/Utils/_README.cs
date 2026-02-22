// LevityFramework - 通用 Unity 游戏框架
// Utils 目录说明

/*
Utils 目录结构建议：

Utils/
├── Extensions/                 # 扩展方法
│   ├── ColorExtensions.cs      # 颜色扩展
│   ├── ListExtensions.cs       # 列表扩展
│   ├── TransformExtensions.cs  # Transform 扩展
│   ├── UnityExtensions.cs      # Unity 通用扩展
│   └── VectorExtensions.cs     # 向量扩展
│
├── Coroutines/                 # 协程工具
│   └── WaitFor.cs              # 协程等待缓存
│
├── Animation/                  # 动画工具
│   └── DOTweenExtensions.cs    # DOTween 扩展
│
└── Debug/                      # 调试工具
    └── LogExtensions.cs        # 日志扩展

当前为保持向后兼容，文件保持在 Utils 根目录。
建议在 Unity 编辑器中通过拖拽方式重新组织文件结构，
Unity 会自动更新所有引用和 .meta 文件。
*/
