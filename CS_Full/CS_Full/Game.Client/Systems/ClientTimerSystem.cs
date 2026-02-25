using Game.Client;
using Game.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Client
{
    /// <summary>
    /// 此处放置 客户端 运行逻辑
    /// </summary>
    public class ClientTimerSystem : Singleton<ClientTimerSystem>, IClientSystem
    {
        /// <summary> 服务器大厅帧号 </summary>
        public ulong gameServerTick;

        /// <summary> 服务器战斗房间内的帧号（最新的） </summary>
        public ulong battleServerTick;

        /// <summary> 客户端当前帧号（战斗房间内的） </summary>
        public ulong clientTick;

        /// <summary> 上一帧逻辑帧的时间 </summary>
        private float _lastTickTime;

        /// <summary> RTT 模块 </summary>
        private Client_RTT _rtt = new();

        #region system func
        public void OnClientConnect()
        {
        }

        public void OnClientDisconnect()
        {
            ClearData();
        }

        public void Start()
        {
        }

        public void Update()
        {
        }

        public void LogicUpdate()
        {
            if (clientTick >= battleServerTick) return;

            var intervalPass = TimeUtility.time - _lastTickTime > ConstVariables.LogicFrameIntervalSeconds;
            if (intervalPass)
            {
                ClientTickGrow();
            }

            _rtt.LogicUpdate();
        }

        /// <summary>
        /// 追帧时使用的逻辑，无视帧间隔检查
        /// </summary>
        public void LogicUpdate_FrameChasing()
        {
            ClientTickGrow();
        }
        #endregion

        private void ClearData()
        {
            gameServerTick = 0;
            battleServerTick = 0;
            clientTick = 0;
            _rtt.ClearRttData();
        }

        /// <summary>
        /// 战斗房间开始时
        /// </summary>
        public void BattleStart()
        {
            ClearData();

            _lastTickTime = TimeUtility.time;
        }

        /// <summary>
        /// 战斗房间结束时
        /// </summary>
        public void BattleStop()
        {
            ClearData();
        }

        /// <summary>
        /// 客户端帧号增长
        /// 快速追帧时无视帧间隔
        /// </summary>
        public void ClientTickGrow()
        {
            clientTick++;
            _lastTickTime = TimeUtility.time;

            GameHelper_Common.FileLog(GameFacade_Common.commandSnapshotLogName, $"ClientTickGrow: clientTick:{clientTick} serverTick:{battleServerTick}");
        }

        #region RTT
        public float GetRTTValue()
        {
            return _rtt.GetValue();
        }

        public void OnRttRsp()
        {
            _rtt.OnRttRsp();
        }
        #endregion
    }
}
