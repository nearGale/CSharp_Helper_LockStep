using System;
using System.Diagnostics;

namespace Game.Shared
{
    /// <summary>
    /// 模拟 Unity 的 Time.time 功能，提供程序启动后的累计运行时间（秒）
    /// </summary>
    public static class TimeUtility
    {
        // 静态 Stopwatch 实例，程序启动时自动初始化并开始计时
        private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        /// <summary>
        /// 等价于 Unity 的 Time.time：获取程序启动后的累计运行时间（秒）
        /// </summary>
        public static float time
        {
            get
            {
                // ElapsedTicks：获取计时器累计的刻度数
                // Stopwatch.Frequency：每秒钟的刻度数（固定值，高精度）
                // 转换公式：秒 = 总刻度数 / 每秒刻度数
                return (float)_stopwatch.ElapsedTicks / Stopwatch.Frequency;
            }
        }

        // 可选：重置计时器（模拟场景切换等重置时间的需求）
        public static void ResetTime()
        {
            _stopwatch.Reset();
            _stopwatch.Start();
        }
    }
}
