using System.Collections;
using System.Collections.Generic;

namespace Game.Shared
{
    public class FixedPointNumber
    {
        /// <summary>
        /// 浮点数转定点数
        /// </summary>
        /// <param name="val">浮点数</param>
        /// <returns>定点数</returns>
        public static long GetFixedPointLong(float val)
        {
            long finalVal = (long)(val * 1000);
            return finalVal;
        }

        /// <summary>
        /// 定点数转成浮点数
        /// </summary>
        /// <param name="val">定点数</param>
        /// <returns>浮点数</returns>
        public static float GetFixedPointFloat(long val)
        {
            float finalVal = (float)val / 1000f;
            return finalVal;
        }
    }
}