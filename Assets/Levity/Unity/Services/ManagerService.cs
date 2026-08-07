// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - ManagerService Manager 服务聚合

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Levity.Stage;
using UnityEngine;

/// <summary>
/// Manager 服务：负责动态注册、初始化和生命周期管理场景级别的各类 Manager
/// </summary>
public class ManagerService : ILogic
{
    private Dictionary<Type, ManagerBase> managerDic = new Dictionary<Type, ManagerBase>();
    private List<ManagerBase> managerList = new List<ManagerBase>();
    private StageScope currentStageScope;

    public void OnInit()
    {
        managerDic.Clear();
        managerList.Clear();
        currentStageScope = null;
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
        ClearAllManagers();
    }

    /// <summary>Sets the Stage scope used by subsequent Manager registrations.</summary>
    public void BindStageScope(StageScope scope)
    {
        currentStageScope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    /// <summary>Registers a Manager into an explicit current or provisional Stage scope.</summary>
    public StageManagerLease<T> RegisterManager<T>(T manager, StageScope scope)
        where T : ManagerBase
    {
        if (manager == null) throw new ArgumentNullException(nameof(manager));
        if (scope == null) throw new ArgumentNullException(nameof(scope));

        var type = manager.GetType();
        var lease = scope.Register(manager, ReleaseManagerAsync);
        managerDic[type] = manager;
        managerList.Add(manager);
        manager.OnAwake();
        return lease;
    }

    /// <summary>
    /// 注册 Manager
    /// </summary>
    [Obsolete(
        "RegisterManager(manager) is legacy. Bind a StageScope and use RegisterManager(manager, scope) so lifetime ownership is explicit.")]
    public void RegisterManager(ManagerBase manager)
    {
        if (manager == null) throw new ArgumentNullException(nameof(manager));
        Debug.LogWarning(
            "RegisterManager(manager) is using compatibility lifetime. " +
            "Bind a StageScope and register with RegisterManager(manager, scope).");
        var type = manager.GetType();
        if (!managerDic.ContainsKey(type))
        {
            managerDic[type] = manager;
            managerList.Add(manager);
            manager.OnAwake();
        }
        else Debug.LogWarning($"Manager {type.Name} already registered!");
    }

    /// <summary>Returns a checked lease that rejects access after the owning Stage exits.</summary>
    public StageManagerLease<T> GetManagerLease<T>() where T : ManagerBase
    {
        if (currentStageScope == null)
            throw new InvalidOperationException(
                "No current StageScope is bound. Call BindStageScope before resolving Managers.");
        return currentStageScope.Resolve<T>();
    }

    /// <summary>
    /// 获取 Manager
    /// </summary>
    [Obsolete(
        "GetManager<T>() returns an unchecked reference. Use GetManagerLease<T>() and access its Value instead.")]
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
        if (currentStageScope != null)
        {
            currentStageScope.ReleaseAsync().GetAwaiter().GetResult();
            currentStageScope = null;
            return;
        }

        foreach (var manager in managerList) manager.OnExit();
    }

    /// <summary>
    /// 清空所有 Manager
    /// </summary>
    public void ClearAllManagers()
    {
        if (currentStageScope != null)
        {
            currentStageScope.ReleaseAsync().GetAwaiter().GetResult();
            currentStageScope = null;
        }

        foreach (var manager in managerList.ToArray()) manager.UnInit();
        managerDic.Clear();
        managerList.Clear();
    }

    private Task ReleaseManagerAsync<T>(T manager) where T : ManagerBase
    {
        manager.OnExit();
        manager.UnInit();
        var type = manager.GetType();
        if (managerDic.TryGetValue(type, out var registered) && ReferenceEquals(registered, manager))
            managerDic.Remove(type);
        managerList.Remove(manager);
        return Task.CompletedTask;
    }
}
