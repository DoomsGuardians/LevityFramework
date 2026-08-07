// LevityFramework - 通用 Unity 游戏框架
// 核心系统模块 - MonoItemSystem MonoBehaviour 项目管理

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MonoItem 接口，用于场景中需要在关卡初始化时调用的 MonoBehaviour
/// </summary>
public interface IMonoItem
{
    /// <summary>
    /// 关卡初始化时调用
    /// </summary>
    void OnMonoItemInit();

    /// <summary>
    /// 关卡卸载时调用
    /// </summary>
    void OnMonoItemUnload();
}

/// <summary>
/// MonoItem 系统：管理场景中实现 IMonoItem 接口的对象
/// 在关卡加载完成后统一初始化
/// </summary>
public class MonoItemSystem : ILogic
{
    private List<IMonoItem> monoItems = new List<IMonoItem>();
    private bool isInitialized = false;

    public void OnInit() { }

    public void OnUpdate() { }

    public void OnEnterState() { }

    public void UnInit()
    {
        UnloadMonoItems();
    }

    /// <summary>
    /// 注册 MonoItem
    /// </summary>
    public void RegisterMonoItem(IMonoItem item)
    {
        if (item != null && !monoItems.Contains(item))
        {
            monoItems.Add(item);

            // 如果系统已经初始化，立即调用新注册项的初始化
            if (isInitialized)
            {
                item.OnMonoItemInit();
            }
        }
    }

    /// <summary>
    /// 注销 MonoItem
    /// </summary>
    public void UnregisterMonoItem(IMonoItem item)
    {
        if (item != null)
        {
            monoItems.Remove(item);
        }
    }

    /// <summary>
    /// 初始化所有 MonoItem（由 StageSystem 在关卡加载完成后调用）
    /// </summary>
    public void InitMonoItem()
    {
        // 查找场景中所有实现 IMonoItem 的对象
        var sceneItems = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var item in sceneItems)
        {
            if (item is IMonoItem monoItem && !monoItems.Contains(monoItem))
            {
                monoItems.Add(monoItem);
            }
        }

        // 调用所有 MonoItem 的初始化
        foreach (var item in monoItems)
        {
            try
            {
                item.OnMonoItemInit();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MonoItemSystem] Error initializing MonoItem: {e.Message}");
            }
        }

        isInitialized = true;
    }

    /// <summary>
    /// 卸载所有 MonoItem（在场景切换前调用）
    /// </summary>
    public void UnloadMonoItems()
    {
        foreach (var item in monoItems)
        {
            try
            {
                item.OnMonoItemUnload();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MonoItemSystem] Error unloading MonoItem: {e.Message}");
            }
        }

        monoItems.Clear();
        isInitialized = false;
    }

    /// <summary>
    /// 获取已注册的 MonoItem 数量
    /// </summary>
    public int GetMonoItemCount() => monoItems.Count;
}
