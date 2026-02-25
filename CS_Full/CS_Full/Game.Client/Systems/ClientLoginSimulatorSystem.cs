using Game.Shared;
using Game.Shared.Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Client
{
    /// <summary>
    /// 客户端 运行逻辑
    /// </summary>
    public class ClientLoginSimulatorSystem : Singleton<ClientLoginSimulatorSystem>, IClientSystem
    {
        bool reqStart = false;

        #region system func
        public void OnClientConnect()
        {
        }

        public void OnClientDisconnect()
        {
        }

        public void Start()
        {
        }

        public void Update()
        {
            if(!reqStart && GameFacade_Client.joinedRoom)
            {
                Msg_BattleStart_Req msg = new();
                NetworkClient.Instance.EnqueueMessage(EProtoType.BattleStartReq, msg);
                reqStart = true;
            }
        }

        public void LogicUpdate()
        {
        }
        #endregion
    }
}
