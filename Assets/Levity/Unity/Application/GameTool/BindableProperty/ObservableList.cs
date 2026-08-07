// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - ObservableList 可观察列表

using System;
using System.Collections.Generic;

/// <summary>
/// 可观察列表，支持列表变化事件
/// </summary>
public class ObservableList<T> : List<T>
{
    /// <summary>当列表数量发生变化时触发：(列表自身, 是增加还是减少, 增加或减少的元素)</summary>
    public event Action<List<T>, bool, T> CountChanged;

    /// <summary>当列表中的元素被修改时触发</summary>
    public event Action<T> ItemModified;

    public new void Add(T item)
    {
        base.Add(item);
        CountChanged?.Invoke(this, true, item);
    }

    public new bool Remove(T item)
    {
        bool result = base.Remove(item);
        if (result)
        {
            CountChanged?.Invoke(this, false, item);
        }
        return result;
    }

    public new void Clear()
    {
        foreach (var item in this)
        {
            CountChanged?.Invoke(this, false, item);
        }
        base.Clear();
    }

    public new void Insert(int index, T item)
    {
        base.Insert(index, item);
        CountChanged?.Invoke(this, true, item);
    }

    public new void RemoveAt(int index)
    {
        T item = this[index];
        base.RemoveAt(index);
        CountChanged?.Invoke(this, false, item);
    }

    public new T this[int index]
    {
        get => base[index];
        set
        {
            base[index] = value;
            ItemModified?.Invoke(value);
        }
    }
}
