// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - ResService 资源服务

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 资源服务：资源加载和对象池管理
/// 可根据项目需要集成 Addressable Assets 或其他资源管理系统
/// </summary>
public class ResService : ILogic
{
    private Dictionary<string, Queue<GameObject>> poolDic = new Dictionary<string, Queue<GameObject>>();

    public void OnInit()
    {
        poolDic.Clear();
    }

    public void OnEnterState() { }

    public void OnUpdate() { }

    public void UnInit()
    {
        ClearAllPools();
    }

    #region 资源加载（可根据项目扩展）
    /// <summary>
    /// 同步加载资源（从 Resources 文件夹）
    /// </summary>
    public T LoadResource<T>(string path) where T : UnityEngine.Object
    {
        return Resources.Load<T>(path);
    }

    /// <summary>
    /// 异步加载资源（从 Resources 文件夹）
    /// </summary>
    public void LoadResourceAsync<T>(string path, Action<T> callback) where T : UnityEngine.Object
    {
        var request = Resources.LoadAsync<T>(path);
        request.completed += (op) =>
        {
            callback?.Invoke(request.asset as T);
        };
    }

    /// <summary>
    /// 加载场景
    /// </summary>
    public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single, Action onComplete = null)
    {
        var operation = SceneManager.LoadSceneAsync(sceneName, mode);
        if (onComplete != null)
        {
            operation.completed += (op) => onComplete?.Invoke();
        }
    }
    #endregion

    #region 对象池
    /// <summary>
    /// 从对象池获取对象
    /// </summary>
    public GameObject GetFromPool(string key, GameObject prefab, Transform parent = null)
    {
        if (poolDic.TryGetValue(key, out var queue) && queue.Count > 0)
        {
            var obj = queue.Dequeue();
            obj.SetActive(true);
            if (parent != null)
            {
                obj.transform.SetParent(parent);
            }
            return obj;
        }

        // 池中没有，创建新对象
        var newObj = GameObject.Instantiate(prefab, parent);
        return newObj;
    }

    /// <summary>
    /// 回收对象到对象池
    /// </summary>
    public void ReturnToPool(string key, GameObject obj)
    {
        if (!poolDic.ContainsKey(key))
        {
            poolDic[key] = new Queue<GameObject>();
        }
        obj.SetActive(false);
        poolDic[key].Enqueue(obj);
    }

    /// <summary>
    /// 清空所有对象池
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var queue in poolDic.Values)
        {
            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
                if (obj != null)
                {
                    GameObject.Destroy(obj);
                }
            }
        }
        poolDic.Clear();
    }

    /// <summary>
    /// 清空指定对象池
    /// </summary>
    public void ClearPool(string key)
    {
        if (poolDic.TryGetValue(key, out var queue))
        {
            while (queue.Count > 0)
            {
                var obj = queue.Dequeue();
                if (obj != null)
                {
                    GameObject.Destroy(obj);
                }
            }
            poolDic.Remove(key);
        }
    }
    #endregion
}
