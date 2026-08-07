// LevityFramework - 通用 Unity 游戏框架
// 资源服务模块 - GenericPool 泛型对象池

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 泛型对象池
/// 支持任何实现 IPoolable 的类型
/// </summary>
/// <typeparam name="T">池化对象类型</typeparam>
public class GenericPool<T> where T : class, IPoolable, new()
{
    private readonly Stack<T> available = new Stack<T>();
    private readonly HashSet<T> active = new HashSet<T>();
    private readonly int maxSize;

    /// <summary>
    /// 可用对象数量
    /// </summary>
    public int AvailableCount => available.Count;

    /// <summary>
    /// 活跃对象数量
    /// </summary>
    public int ActiveCount => active.Count;

    /// <summary>
    /// 总对象数量
    /// </summary>
    public int TotalCount => available.Count + active.Count;

    /// <summary>
    /// 创建对象池
    /// </summary>
    /// <param name="initialSize">初始大小</param>
    /// <param name="maxSize">最大大小（0 = 无限制）</param>
    public GenericPool(int initialSize = 0, int maxSize = 0)
    {
        this.maxSize = maxSize;
        Prewarm(initialSize);
    }

    /// <summary>
    /// 预热对象池
    /// </summary>
    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (maxSize > 0 && TotalCount >= maxSize) break;
            available.Push(new T());
        }
    }

    /// <summary>
    /// 获取对象
    /// </summary>
    public T Get()
    {
        T item;

        if (available.Count > 0)
        {
            item = available.Pop();
        }
        else
        {
            item = new T();
        }

        active.Add(item);
        item.OnSpawn();
        return item;
    }

    /// <summary>
    /// 返还对象
    /// </summary>
    public void Return(T item)
    {
        if (item == null || !active.Contains(item)) return;

        active.Remove(item);
        item.OnDespawn();

        if (maxSize <= 0 || available.Count < maxSize)
        {
            available.Push(item);
        }
    }

    /// <summary>
    /// 返还所有活跃对象
    /// </summary>
    public void ReturnAll()
    {
        foreach (var item in active)
        {
            item.OnDespawn();
            if (maxSize <= 0 || available.Count < maxSize)
            {
                available.Push(item);
            }
        }
        active.Clear();
    }

    /// <summary>
    /// 清空对象池
    /// </summary>
    public void Clear()
    {
        foreach (var item in active)
        {
            item.OnDespawn();
        }
        active.Clear();
        available.Clear();
    }

    /// <summary>
    /// 修剪空闲对象
    /// </summary>
    /// <param name="keepCount">保留数量</param>
    public void Trim(int keepCount)
    {
        while (available.Count > keepCount)
        {
            available.Pop();
        }
    }
}

/// <summary>
/// GameObject 对象池
/// 专门用于 Unity GameObject 的对象池
/// </summary>
public class GameObjectPool
{
    private readonly Stack<GameObject> available = new Stack<GameObject>();
    private readonly HashSet<GameObject> active = new HashSet<GameObject>();
    private readonly GameObject prefab;
    private readonly Transform parent;
    private readonly int maxSize;

    public int AvailableCount => available.Count;
    public int ActiveCount => active.Count;
    public int TotalCount => available.Count + active.Count;

    /// <summary>
    /// 创建 GameObject 对象池
    /// </summary>
    /// <param name="prefab">预制体</param>
    /// <param name="parent">父节点</param>
    /// <param name="initialSize">初始大小</param>
    /// <param name="maxSize">最大大小（0 = 无限制）</param>
    public GameObjectPool(GameObject prefab, Transform parent = null, int initialSize = 0, int maxSize = 0)
    {
        this.prefab = prefab;
        this.parent = parent;
        this.maxSize = maxSize;
        Prewarm(initialSize);
    }

    /// <summary>
    /// 预热对象池
    /// </summary>
    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (maxSize > 0 && TotalCount >= maxSize) break;

            var go = UnityEngine.Object.Instantiate(prefab, parent);
            go.SetActive(false);
            available.Push(go);
        }
    }

    /// <summary>
    /// 获取对象
    /// </summary>
    public GameObject Get(Vector3 position = default, Quaternion rotation = default)
    {
        GameObject go;

        if (available.Count > 0)
        {
            go = available.Pop();
        }
        else
        {
            go = UnityEngine.Object.Instantiate(prefab, parent);
        }

        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);
        active.Add(go);

        // 调用 IPoolable
        var poolable = go.GetComponent<IPoolable>();
        poolable?.OnSpawn();

        return go;
    }

    /// <summary>
    /// 获取对象并设置父节点
    /// </summary>
    public GameObject Get(Transform newParent, Vector3 localPosition = default)
    {
        var go = Get();
        go.transform.SetParent(newParent);
        go.transform.localPosition = localPosition;
        return go;
    }

    /// <summary>
    /// 返还对象
    /// </summary>
    public void Return(GameObject go)
    {
        if (go == null || !active.Contains(go)) return;

        // 调用 IPoolable
        var poolable = go.GetComponent<IPoolable>();
        poolable?.OnDespawn();

        active.Remove(go);
        go.SetActive(false);
        go.transform.SetParent(parent);

        if (maxSize <= 0 || available.Count < maxSize)
        {
            available.Push(go);
        }
        else
        {
            UnityEngine.Object.Destroy(go);
        }
    }

    /// <summary>
    /// 返还所有活跃对象
    /// </summary>
    public void ReturnAll()
    {
        var toReturn = new List<GameObject>(active);
        foreach (var go in toReturn)
        {
            Return(go);
        }
    }

    /// <summary>
    /// 清空对象池
    /// </summary>
    public void Clear()
    {
        foreach (var go in active)
        {
            if (go != null) UnityEngine.Object.Destroy(go);
        }
        foreach (var go in available)
        {
            if (go != null) UnityEngine.Object.Destroy(go);
        }
        active.Clear();
        available.Clear();
    }
}
