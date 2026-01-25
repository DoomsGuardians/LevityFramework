// LevityFramework - 通用 Unity 游戏框架
// 核心服务模块 - PrecisionTimer 高精度定时器

using System;
using System.Collections.Concurrent;
using System.Threading;
using UnityEngine;

/// <summary>
/// 定时器类型
/// </summary>
public enum TimerType
{
    RealTime,       // 现实时间（基于 UTC）
    ScaledTime,     // 游戏时间（受 Time.timeScale 影响）
    UnscaledTime    // 游戏非缩放时间
}

/// <summary>
/// 高精度定时器实现
/// </summary>
public class PrecisionTimer : GameTimerBase
{
    #region 数据结构
    private class TimerTask
    {
        public int tid;
        public int delay;
        public int remaining;
        public Action taskCB;
        public Action cancelCB;
        public Action LoopEndCB;
        public int loopCount;
        public TimerType timerType;
        public double startTime;
        public double destTime;
        public bool isPaused;
        public double pauseTime;
        public Func<double> GetTime;
        public int countIndex;

        public double CurrentTime => GetTime();
        public double RemainingMs => isPaused ? remaining : Math.Max(destTime - CurrentTime, 0);
    }

    private struct TimerCallback
    {
        public int tid;
        public Action callback;
        public Action loopEndCB;
    }
    #endregion

    #region 配置参数
    private const int MIN_INTERVAL = 10;
    private const int TID_MAX = int.MaxValue - 1;
    #endregion

    private readonly ConcurrentDictionary<int, TimerTask> _timerDic;
    private readonly ConcurrentQueue<TimerCallback> _callbackQueue;
    private readonly Thread _workerThread;
    private readonly CancellationTokenSource _cts;
    private readonly long _timeBase;
    private TimerType _defaultType;

    public PrecisionTimer(
        int checkInterval = MIN_INTERVAL,
        bool useMainThreadCB = false,
        TimerType defaultType = TimerType.RealTime)
    {
        _timerDic = new ConcurrentDictionary<int, TimerTask>();
        _callbackQueue = useMainThreadCB ? new ConcurrentQueue<TimerCallback>() : null;
        _timeBase = DateTime.UtcNow.Ticks;
        this._defaultType = defaultType;

        if (checkInterval != 0)
        {
            _cts = new CancellationTokenSource();
            void StartTime()
            {
                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        UpdateTime();
                        Thread.Sleep(checkInterval);
                    }
                }
                catch (ThreadAbortException e)
                {
                    warnFunc?.Invoke("PrecisionTimer Thread Abort: " + e);
                }
            }
            _workerThread = new Thread(new ThreadStart(StartTime));
            _workerThread.Start();
        }
    }

    #region 公开接口
    public override int AddTimer(int time, Action taskCB, Action cancelCB, int count = 1)
    {
        return AddTimer(time, taskCB, cancelCB, null, count, null);
    }

    public int AddTimer(int delayMs, Action taskCB, Action cancelCB, Action loopDoneCB = null, int loopCount = 1, TimerType? type = null)
    {
        var timerType = type ?? _defaultType;
        var now = GetCurrentTime(timerType);
        var task = new TimerTask
        {
            tid = GenerateTid(),
            delay = delayMs,
            remaining = delayMs,
            taskCB = taskCB,
            cancelCB = cancelCB,
            loopCount = loopCount,
            timerType = timerType,
            GetTime = () => GetCurrentTime(timerType),
            startTime = now,
            destTime = now + delayMs,
            LoopEndCB = loopDoneCB,
        };
        return _timerDic.TryAdd(task.tid, task) ? task.tid : -1;
    }

    public bool AdjustTime(int tid, int changedDelayMs)
    {
        if (!_timerDic.TryGetValue(tid, out var task)) return false;

        lock (task)
        {
            if (task.isPaused)
            {
                task.remaining += changedDelayMs;
            }
            else
            {
                double currentTime = task.GetTime();
                double newRemaining = task.destTime - currentTime + changedDelayMs;
                task.destTime = newRemaining + currentTime;
            }
        }
        return true;
    }

    public bool SetPaused(int tid, bool paused)
    {
        if (!_timerDic.TryGetValue(tid, out var task)) return false;

        lock (task)
        {
            if (task.isPaused == paused) return true;

            if (paused)
            {
                task.pauseTime = task.CurrentTime;
                task.remaining = (int)(task.destTime - task.pauseTime);
            }
            else
            {
                var pausedDuration = task.CurrentTime - task.pauseTime;
                task.startTime += pausedDuration;
                task.destTime = task.startTime + task.remaining;
            }
            task.isPaused = paused;
        }
        return true;
    }

    public int QueryRemaining(int tid)
    {
        if (!_timerDic.TryGetValue(tid, out var task)) return -1;
        return (int)Math.Ceiling(task.RemainingMs);
    }

    public override bool DeleteTimer(int tid)
    {
        if (!_timerDic.TryRemove(tid, out var task)) return false;
        EnqueueCallback(tid, task.cancelCB, null);
        return true;
    }

    public void UpdateMainThread()
    {
        while (_callbackQueue != null && _callbackQueue.TryDequeue(out var cb))
        {
            cb.callback?.Invoke();
            cb.loopEndCB?.Invoke();
        }
    }
    #endregion

    #region 核心逻辑
    public void UpdateTime()
    {
        UpdateTasks();
    }

    private void UpdateTasks()
    {
        foreach (var pair in _timerDic)
        {
            var task = pair.Value;
            if (task.isPaused) continue;

            var now = task.CurrentTime;
            if (now < task.destTime) continue;

            ++task.countIndex;
            if (task.loopCount > 0)
            {
                --task.loopCount;
                if (task.loopCount == 0)
                {
                    FinishTimer(task.tid);
                }
                else
                {
                    task.destTime = task.startTime + (task.countIndex + 1) * task.delay;
                    EnqueueCallback(task.tid, task.taskCB, null);
                }
            }
            else
            {
                task.destTime = task.startTime + (task.countIndex + 1) * task.delay;
                EnqueueCallback(task.tid, task.taskCB, null);
            }
        }
    }

    private double GetCurrentTime(TimerType type)
    {
        return type switch
        {
            TimerType.ScaledTime => Time.timeAsDouble * 1000,
            TimerType.UnscaledTime => Time.unscaledTimeAsDouble * 1000,
            _ => (DateTime.UtcNow.Ticks - _timeBase) / TimeSpan.TicksPerMillisecond
        };
    }

    private void FinishTimer(int tid)
    {
        if (_timerDic.TryRemove(tid, out TimerTask timerTask))
        {
            EnqueueCallback(tid, timerTask.taskCB, timerTask.LoopEndCB);
        }
        else
        {
            errorFunc?.Invoke($"Remove timerTask tid:{tid} failed");
        }
    }

    private void EnqueueCallback(int tid, Action callback, Action loopEndCB)
    {
        if (_callbackQueue != null)
        {
            _callbackQueue.Enqueue(new TimerCallback
            {
                tid = tid,
                callback = callback,
                loopEndCB = loopEndCB
            });
        }
        else
        {
            callback?.Invoke();
            loopEndCB?.Invoke();
        }
    }
    #endregion

    #region 基类实现
    public override void ResetTimer()
    {
        _timerDic.Clear();
        if (_cts != null)
        {
            _cts.Cancel();
            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join();
            }
        }
    }

    protected override int GenerateTid()
    {
        int newTid;
        do
        {
            newTid = Interlocked.Increment(ref tid);
            if (newTid > TID_MAX) Interlocked.Exchange(ref tid, 0);
        } while (_timerDic.ContainsKey(newTid));
        return newTid;
    }
    #endregion
}
