// LevityFramework - 通用 Unity 游戏框架
// 工具模块 - ListExtensions 列表扩展方法

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// List 和 IEnumerable 扩展方法
/// 提供便捷的列表操作
/// </summary>
public static class ListExtensions
{
    private static Random rng;

    #region 判断方法

    /// <summary>
    /// 判断集合是否为空或 null
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IList<T> list)
    {
        return list == null || list.Count == 0;
    }

    /// <summary>
    /// 判断集合是否为空或 null（IEnumerable 版本）
    /// </summary>
    public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
    {
        return enumerable == null || !enumerable.Any();
    }

    /// <summary>
    /// 判断集合不为空且有元素
    /// </summary>
    public static bool IsNotEmpty<T>(this IList<T> list)
    {
        return list != null && list.Count > 0;
    }

    #endregion

    #region 随机操作

    /// <summary>
    /// Fisher-Yates 洗牌算法，原地打乱列表
    /// </summary>
    public static IList<T> Shuffle<T>(this IList<T> list)
    {
        if (rng == null) rng = new Random();

        int count = list.Count;
        while (count > 1)
        {
            --count;
            int index = rng.Next(count + 1);
            (list[index], list[count]) = (list[count], list[index]);
        }
        return list;
    }

    /// <summary>
    /// 获取随机元素
    /// </summary>
    public static T GetRandom<T>(this IList<T> list)
    {
        if (list.IsNullOrEmpty()) return default;
        if (rng == null) rng = new Random();
        return list[rng.Next(list.Count)];
    }

    /// <summary>
    /// 获取多个不重复的随机元素
    /// </summary>
    public static List<T> GetRandomMultiple<T>(this IList<T> list, int count)
    {
        if (list.IsNullOrEmpty() || count <= 0) return new List<T>();
        if (count >= list.Count) return new List<T>(list);

        var copy = new List<T>(list);
        copy.Shuffle();
        return copy.Take(count).ToList();
    }

    #endregion

    #region 元素操作

    /// <summary>
    /// 交换两个位置的元素
    /// </summary>
    public static void Swap<T>(this IList<T> list, int indexA, int indexB)
    {
        (list[indexA], list[indexB]) = (list[indexB], list[indexA]);
    }

    /// <summary>
    /// 移动元素到新位置
    /// </summary>
    public static void Move<T>(this IList<T> list, int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;

        T item = list[fromIndex];
        list.RemoveAt(fromIndex);

        if (toIndex > fromIndex) toIndex--;
        list.Insert(toIndex, item);
    }

    /// <summary>
    /// 安全获取元素，索引越界返回默认值
    /// </summary>
    public static T GetSafe<T>(this IList<T> list, int index, T defaultValue = default)
    {
        if (list == null || index < 0 || index >= list.Count)
            return defaultValue;
        return list[index];
    }

    /// <summary>
    /// 获取第一个元素，如果为空返回默认值
    /// </summary>
    public static T FirstOrDefault<T>(this IList<T> list, T defaultValue = default)
    {
        return list.IsNullOrEmpty() ? defaultValue : list[0];
    }

    /// <summary>
    /// 获取最后一个元素，如果为空返回默认值
    /// </summary>
    public static T LastOrDefault<T>(this IList<T> list, T defaultValue = default)
    {
        return list.IsNullOrEmpty() ? defaultValue : list[list.Count - 1];
    }

    #endregion

    #region 复制与过滤

    /// <summary>
    /// 深拷贝列表（浅拷贝元素）
    /// </summary>
    public static List<T> Clone<T>(this IList<T> list)
    {
        return new List<T>(list);
    }

    /// <summary>
    /// 过滤列表，返回符合条件的新列表
    /// </summary>
    public static List<T> Filter<T>(this IList<T> source, Predicate<T> predicate)
    {
        var result = new List<T>();
        foreach (var item in source)
        {
            if (predicate(item))
            {
                result.Add(item);
            }
        }
        return result;
    }

    /// <summary>
    /// 用另一个列表的内容刷新当前列表
    /// </summary>
    public static void RefreshWith<T>(this IList<T> list, IList<T> source)
    {
        list.Clear();
        foreach (var item in source)
        {
            list.Add(item);
        }
    }

    #endregion

    #region 批量操作

    /// <summary>
    /// 对每个元素执行操作
    /// </summary>
    public static void ForEach<T>(this IList<T> list, Action<T> action)
    {
        foreach (var item in list)
        {
            action(item);
        }
    }

    /// <summary>
    /// 对每个元素执行操作（带索引）
    /// </summary>
    public static void ForEach<T>(this IList<T> list, Action<T, int> action)
    {
        for (int i = 0; i < list.Count; i++)
        {
            action(list[i], i);
        }
    }

    /// <summary>
    /// 添加多个元素
    /// </summary>
    public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            list.Add(item);
        }
    }

    /// <summary>
    /// 移除所有符合条件的元素
    /// </summary>
    public static int RemoveAll<T>(this IList<T> list, Predicate<T> predicate)
    {
        int removed = 0;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (predicate(list[i]))
            {
                list.RemoveAt(i);
                removed++;
            }
        }
        return removed;
    }

    #endregion

    #region 查找

    /// <summary>
    /// 查找元素索引，未找到返回 -1
    /// </summary>
    public static int FindIndex<T>(this IList<T> list, Predicate<T> predicate)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 查找最后一个符合条件的元素索引
    /// </summary>
    public static int FindLastIndex<T>(this IList<T> list, Predicate<T> predicate)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (predicate(list[i]))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// 查找符合条件的元素
    /// </summary>
    public static T Find<T>(this IList<T> list, Predicate<T> predicate)
    {
        foreach (var item in list)
        {
            if (predicate(item))
                return item;
        }
        return default;
    }

    /// <summary>
    /// 查找所有符合条件的元素
    /// </summary>
    public static List<T> FindAll<T>(this IList<T> list, Predicate<T> predicate)
    {
        var result = new List<T>();
        foreach (var item in list)
        {
            if (predicate(item))
                result.Add(item);
        }
        return result;
    }

    #endregion
}
