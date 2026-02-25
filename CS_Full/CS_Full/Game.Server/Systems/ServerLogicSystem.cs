using Game.Server;
using Game.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Server
{
    public enum EServerRoomState
    {
        Lobby,
        InBattle,
    }

    public class ServerLogicSystem : Singleton<ServerLogicSystem>, IServerSystem
    {
        public EServerRoomState eRoomState;

        /// <summary>
        /// 本场战斗内，随机数种子
        /// </summary>
        public int randomSeed;


        #region system func

        public void OnStartServer()
        {
            eRoomState = EServerRoomState.Lobby;
        }

        public void OnStopServer()
        {
        }

        public void Start() { }

        public void Update() { }

        public void LogicUpdate() { }

        #endregion

        /// <summary>
        /// 战斗房间开始时
        /// </summary>
        public void StartBattle()
        {
            eRoomState = EServerRoomState.InBattle;

            var gameServerTick = GameHelper_Server.GetGameServerTick();
            randomSeed = (int)(gameServerTick % 100);
        }


        /// <summary>
        /// 战斗房间结束时
        /// </summary>
        public void StopBattle()
        {
            eRoomState = EServerRoomState.Lobby;
            randomSeed = 0;
        }

    }
}
