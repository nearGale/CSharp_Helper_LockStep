using Game.Server;
using Game.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Server
{
    public class ServerTimerSystem : Singleton<ServerTimerSystem>, IServerSystem
    {
        /// <summary> 匹配大厅的时间戳 </summary>
        public ulong gameServerTick;

        /// <summary> 战斗局内的时间戳 </summary>
        public ulong battleServerTick;

        /// <summary> 战斗服务器是否暂停 </summary>
        public bool battlePause;

        /// <summary> 战斗开始时的时间 </summary>
        private float _battleStartTime;

        /// <summary> 上一帧逻辑帧的时间 </summary>
        private float _lastTickTime;

        #region system func

        public void OnStartServer()
        {
            gameServerTick = 0;
        }

        public void OnStopServer()
        {
        }

        public void Start()
        {
        }

        public void Update()
        {
        }

        public void LogicUpdate()
        {
            gameServerTick++;

            var intervalPass = TimeUtility.time - _lastTickTime > ConstVariables.LogicFrameIntervalSeconds;
            if (GameHelper_Server.IsInBattleRoom()
                && !battlePause
                && intervalPass)
            {
                battleServerTick++;
                _lastTickTime = TimeUtility.time;
            }
        }

        #endregion

        /// <summary>
        /// 战斗房间开始时
        /// </summary>
        public void StartBattle()
        {
            battleServerTick = 0;
            battlePause = false;
            _battleStartTime = TimeUtility.time;
            _lastTickTime = TimeUtility.time;
        }

        /// <summary>
        /// 战斗房间结束时
        /// </summary>
        public void StopBattle()
        {
            battleServerTick = 0;
            battlePause = false;
        }
    }
}
