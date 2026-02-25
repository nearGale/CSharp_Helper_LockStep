using Game.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Client
{
    public class GameFacade_Client
    {
        public static string PlayerName;

        public static List<IClientSystem> clientSystems = new()
        {
            // 客户端系统
            // 执行
            //     Start()
            //     Update()
            //     LogicUpdate()

            ClientMessageSystem.Instance,
            ClientRoomSystem.Instance,
            
            // ==================================
            // 战斗中系统，加速追帧就跑这几个系统
            // 追帧：GameHelper_Client.ChasingOneFrame()
            ClientFrameSyncSystem.Instance, // 执行指令
            ClientLogicSystem.Instance, // 逻辑更新
            ClientTimerSystem.Instance, // 帧数更新
            // ==================================

            ClientChasingFrameSystem.Instance,

            ClientLoginSimulatorSystem.Instance, // 没有GUI的模拟终端
        };

        public static bool joinedRoom = false;
    }
}
