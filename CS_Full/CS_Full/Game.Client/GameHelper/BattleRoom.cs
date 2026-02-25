using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Client
{
    public static partial class GameHelper_Client
    {
        public static ulong GetClientTick()
        {
            return ClientTimerSystem.Instance.clientTick;
        }

        public static ulong GetBattleServerTick()
        {
            return ClientTimerSystem.Instance.battleServerTick;
        }

        public static ulong GetGameServerTick()
        {
            return ClientTimerSystem.Instance.gameServerTick;
        }

        /// <summary>
        /// 加速追帧
        /// </summary>
        public static void ChasingOneFrame()
        {
            ClientFrameSyncSystem.Instance.LogicUpdate();
            ClientLogicSystem.Instance.LogicUpdate();
            ClientTimerSystem.Instance.LogicUpdate_FrameChasing();
        }

        public static string GetLocalPlayerName()
        {
            return GameFacade_Client.PlayerName;
        }

        public static float GetRTT()
        {
            return ClientTimerSystem.Instance.GetRTTValue();
        }
    }
}
