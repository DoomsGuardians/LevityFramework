// LevityFramework - 通用 Unity 游戏框架
// 核心指令模块 - BindableProperty 可观察属性

using System;

/// <summary>
/// 泛型可观察属性类，支持值变化回调
/// </summary>
public class BindableProperty<T>
{
    /// <summary>当值发生变化时触发</summary>
    public Action<T> ValueChanged;

    private T m_value;

    public T Value
    {
        get => m_value;
        set
        {
            if (!Equals(m_value, value))
            {
                m_value = value;
                ValueChanged?.Invoke(m_value);
            }
        }
    }

    public BindableProperty() { }

    public BindableProperty(T initialValue)
    {
        m_value = initialValue;
    }

    /// <summary>设置值但不触发回调</summary>
    public void SetValueWithoutNotify(T value)
    {
        m_value = value;
    }
}
