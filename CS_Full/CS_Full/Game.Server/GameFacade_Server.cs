using Game.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Server
{
    public class GameFacade_Server
    {
        /// <summary>
        /// 这里放了所有的 system，一帧中运行按照这里的顺序执行
        /// </summary>
        public static List<IServerSystem> serverSystems = new()
        {
            // 服务器端系统
            // 执行
            //     Start()
            //     Update()
            //     LogicUpdate()
            // 服务器端专属
            //     OnStartServer()
            //     OnStopServer()
            
            ServerTimerSystem.Instance,
            ServerPlayerSystem.Instance,
            ServerMessageSystem.Instance,
            ServerLogicSystem.Instance,
            ServerCommandStorageSystem.Instance,
            ServerCommandSyncSystem.Instance,
        };
    }
}
