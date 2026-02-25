using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    /// <summary>
    /// 模拟 Unity 生命周期的核心管理器
    /// </summary>
    public class LifeCycleSimulator : Singleton<LifeCycleSimulator>
    {
        // FixedUpdate 帧率（限制 30 帧则设为 1/30）
        private const float FixedUpdateInterval = 1f / 2;

        // Update 帧率（限制 60 帧则设为 1/60）
        private const float UpdateInterval = 1f / 2;

        // 高精度计时器（核心计时工具）
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        // 累计时间（用于 FixedUpdate 步进）
        private float _accumulatedTime;

        // 上一次 Update 的时间
        private float _lastUpdateTime;

        // 上一次 FixedUpdate 的时间
        private float _lastFixedUpdateTime;

        // 是否已执行 Start
        private bool _isStartExecuted;

        /// <summary>
        /// 当前帧的时间差（等价于 Unity Time.deltaTime）
        /// </summary>
        public float deltaTime { get; private set; }

        /// <summary>
        /// 程序运行总时间（等价于 Unity Time.time）
        /// </summary>
        public float Time => (float)_stopwatch.ElapsedTicks / Stopwatch.Frequency;

        private Action onUpdate;
        private Action onFixedUpdate;

        /// <summary>
        /// 设定生命周期函数
        /// </summary>
        /// <param name="onStart">模拟 Unity 的 Start 回调</param>
        /// <param name="onUpdate">模拟 Unity 的 Update 回调</param>
        /// <param name="onFixedUpdate">模拟 Unity 的 FixedUpdate 回调</param>
        /// <param name="onQuit">退出循环时的回调（可选）</param>
        public void Prepare(Action onStart, Action onUpdate, Action onFixedUpdate)
        {
            // 1. 执行 Start（仅一次）
            if (!_isStartExecuted)
            {
                onStart?.Invoke();
                _isStartExecuted = true;
                Console.WriteLine("Start 执行完成");
            }

            this.onUpdate = onUpdate;
            this.onFixedUpdate = onFixedUpdate;

            _lastUpdateTime = Time;
            _lastFixedUpdateTime = Time;
        }

        /// <summary>
        /// 主循环
        /// </summary>
        public void Tick()
        {
            // 2. 执行 FixedUpdate（按固定时间步长）
            var checkTime = Time - _lastFixedUpdateTime;
            while (checkTime >= FixedUpdateInterval)
            {
                onFixedUpdate?.Invoke();
                checkTime -= FixedUpdateInterval;
                _lastFixedUpdateTime += FixedUpdateInterval;
            }

            // 3. 执行 Update
            // 限制 Update 最小执行间隔（可选，避免帧率过高）
            checkTime = Time - _lastUpdateTime;
            if (checkTime >= UpdateInterval)
            {
                _lastUpdateTime = Time;
                deltaTime = checkTime;
                // 4. 执行 Update
                onUpdate?.Invoke();
            }
        }
    }
}
