// LevityFramework - 通用 Unity 游戏框架
// 核心系统模块 - RoleSystem 角色系统

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色系统：管理玩家角色和 AI 的实例化、激活、卸载
/// </summary>
public class RoleSystem : ILogic
{
    private Dictionary<string, Player> playerDic = new Dictionary<string, Player>();
    private List<Player> playerList = new List<Player>();
    public Player CurrentPlayer { get; private set; }

    public void OnInit()
    {
        playerDic.Clear();
        playerList.Clear();
    }

    public void OnEnterState() { }

    public void OnUpdate() { }

    public void UnInit()
    {
        UnloadAllPlayers();
    }

    /// <summary>
    /// 注册玩家
    /// </summary>
    public void RegisterPlayer(string id, Player player)
    {
        if (!playerDic.ContainsKey(id))
        {
            playerDic[id] = player;
            playerList.Add(player);
            player.OnAwake();
        }
    }

    /// <summary>
    /// 设置当前玩家
    /// </summary>
    public void SetCurrentPlayer(string id)
    {
        if (playerDic.TryGetValue(id, out var player))
        {
            CurrentPlayer = player;
        }
    }

    /// <summary>
    /// 获取玩家
    /// </summary>
    public Player GetPlayer(string id)
    {
        if (playerDic.TryGetValue(id, out var player))
        {
            return player;
        }
        return null;
    }

    /// <summary>
    /// 获取所有玩家
    /// </summary>
    public List<Player> GetAllPlayers()
    {
        return new List<Player>(playerList);
    }

    /// <summary>
    /// 卸载玩家
    /// </summary>
    public void UnloadPlayer(string id)
    {
        if (playerDic.TryGetValue(id, out var player))
        {
            player.UnInit();
            playerDic.Remove(id);
            playerList.Remove(player);

            if (CurrentPlayer == player)
            {
                CurrentPlayer = null;
            }
        }
    }

    /// <summary>
    /// 卸载所有玩家
    /// </summary>
    public void UnloadAllPlayers()
    {
        foreach (var player in playerList)
        {
            player.UnInit();
        }
        playerDic.Clear();
        playerList.Clear();
        CurrentPlayer = null;
    }
}
