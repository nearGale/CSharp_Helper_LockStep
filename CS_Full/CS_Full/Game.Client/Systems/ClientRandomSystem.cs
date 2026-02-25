using Game.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Client
{
    /// <summary>
    /// 此处放置 客户端 随机数逻辑
    /// </summary>
    public class ClientRandomSystem : Singleton<ClientRandomSystem>, ISystem
    {
        /// <summary> 随机数种子 </summary>
        public int randomSeed;

        private Random randomWithFixedSeed;

        public void Start()
        {
        }

        public void Update()
        {
        }

        public void LogicUpdate()
        {
        }

        /// <summary>
        /// 进入战斗房间时
        /// </summary>
        /// <param name="seed">随机数种子</param>
        public void BattleStart(int seed)
        {
            randomSeed = seed;
            randomWithFixedSeed = new Random(seed);
            GameHelper_Common.LogDebug($"RandomSeed:{seed}");
        }

        /// <summary>
        /// 战斗房间结束时
        /// </summary>
        public void BattleStop()
        {
            randomSeed = 0;
        }

        public int GetRandomInt(int min, int max)
        {
            var randomInt = randomWithFixedSeed.Next(min, max);
            GameHelper_Common.LogDebug($"RandomInt:{randomInt}");

            return randomInt;
        }
    }
}
