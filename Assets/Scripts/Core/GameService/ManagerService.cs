// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - ManagerService Manager 服务聚合

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager 服务：负责动态注册、初始化和生命周期管理场景级别的各类 Manager
/// </summary>
public class ManagerService : ILogic
{
    private Dictionary<Type, ManagerBase> managerDic = new Dictionary<Type, ManagerBase>();
    private List<ManagerBase> managerList = new List<ManagerBase>();

    public void OnInit()
    {
        managerDic.Clear();
        managerList.Clear();
    }

    public void OnEnterState()
    {
        foreach (var manager in managerList)
        {
            manager.OnShow();
        }
    }

    public void OnUpdate() { }

    public void UnInit()
    {
        foreach (var manager in managerList)
        {
            manager.UnInit();
        }
        managerDic.Clear();
        managerList.Clear();
    }

    /// <summary>
    /// 注册 Manager
    /// </summary>
    public void RegisterManager(ManagerBase manager)
    {
        var type = manager.GetType();
        if (!managerDic.ContainsKey(type))
        {
            managerDic[type] = manager;
            managerList.Add(manager);
            manager.OnAwake();
        }
        else
        {
            Debug.LogWarning($"Manager {type.Name} already registered!");
        }
    }

    /// <summary>
    /// 获取 Manager
    /// </summary>
    public T GetManager<T>() where T : ManagerBase
    {
        var type = typeof(T);
        if (managerDic.TryGetValue(type, out var manager))
        {
            return manager as T;
        }
        return null;
    }

    /// <summary>
    /// 注销 Manager
    /// </summary>
    public void UnregisterManager<T>() where T : ManagerBase
    {
        var type = typeof(T);
        if (managerDic.TryGetValue(type, out var manager))
        {
            manager.UnInit();
            managerDic.Remove(type);
            managerList.Remove(manager);
        }
    }

    /// <summary>
    /// 场景退出时调用所有 Manager 的 OnExit
    /// </summary>
    public void OnSceneExit()
    {
        foreach (var manager in managerList)
        {
            manager.OnExit();
        }
    }

    /// <summary>
    /// 清空所有 Manager
    /// </summary>
    public void ClearAllManagers()
    {
        foreach (var manager in managerList)
        {
            manager.UnInit();
        }
        managerDic.Clear();
        managerList.Clear();
    }
}
