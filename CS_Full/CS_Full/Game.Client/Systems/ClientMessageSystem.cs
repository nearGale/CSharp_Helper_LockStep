using Game.Client;
using Game.Shared;
using Game.Shared.Extensions;
using Game.Shared.Proto;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Client
{
    /// <summary>
    /// 客户端  消息绑定 + 处理
    /// </summary>
    public class ClientMessageSystem : Singleton<ClientMessageSystem>, IClientSystem
    {
        /// <summary>
        ///  容器：消息类型 -> 对应的处理委托（Action<T> 存为 Delegate）
        /// </summary>
        private readonly Dictionary<Type, Delegate> _messageHandlers = new();

        #region system func
        public void OnClientConnect()
        {
            RegisterMessageHandlers();
        }

        public void OnClientDisconnect()
        {
            UnRegisterMessageHandlers();
        }

        public void Start()
        {
        }

        public void Update()
        {
        }

        public void LogicUpdate()
        {
        }

        #endregion

        private void RegisterMessageHandlers()
        {
            //GameHelper_Common.LogDebug("注册消息");
            RegisterHandler<Msg_PingPong_Rsp>(OnPingPongRsp);

            RegisterHandler<Msg_PlayerConnect_Rsp>(OnPlayerConnectRsp);
            RegisterHandler<Msg_PlayerIdentify_Rsp>(OnPlayerIdentifyRsp);
            RegisterHandler<Msg_ClientWillDisconnect_Rsp>(OnClientWillDisconnectRsp);

            RegisterHandler<Msg_Join_Ntf>(OnJoinNtf);
            RegisterHandler<Msg_BattleStart_Ntf>(OnBattleStart);
            RegisterHandler<Msg_BattleStop_Ntf>(OnBattleStop);
            RegisterHandler<Msg_BattlePause_Rsp>(OnBattlePause);

            RegisterHandler<Msg_Command_Ntf>(OnCommandNtf);
            RegisterHandler<Msg_CommandAll_Rsp>(OnCommandAllRsp);
        }

        private void UnRegisterMessageHandlers()
        {
            UnregisterHandler<Msg_PlayerConnect_Rsp>();
            UnregisterHandler<Msg_PlayerIdentify_Rsp>();
            UnregisterHandler<Msg_ClientWillDisconnect_Rsp>();

            UnregisterHandler<Msg_Join_Ntf>();
            UnregisterHandler<Msg_BattleStart_Ntf>();
            UnregisterHandler<Msg_BattleStop_Ntf>();
            UnregisterHandler<Msg_BattlePause_Rsp>();

            UnregisterHandler<Msg_Command_Ntf>();
            UnregisterHandler<Msg_CommandAll_Rsp>();
        }

        private void RegisterHandler<T>(Action<T> handler)
        {
            _messageHandlers[typeof(T)] = handler;
        }

        private void UnregisterHandler<T>()
        {
            _messageHandlers.Remove(typeof(T));
        }

        public void HandleMessage<T>(T msg)
        {
            if (msg != null && _messageHandlers.TryGetValue(msg.GetType(), out var method))
            {
                GameHelper_Common.LogDebug($"收到消息：{msg.GetType()}/{msg}");
                var action = (method);
                action.DynamicInvoke(msg);
            }
        }

        #region 消息处理函数

        private void OnPingPongRsp(Msg_PingPong_Rsp msg)
        {
            ClientTimerSystem.Instance.OnRttRsp();
        }


        private void OnPlayerConnectRsp(Msg_PlayerConnect_Rsp msg)
        {
            var name = GameHelper_Client.GetLocalPlayerName();
            if (!name.IsNullOrEmpty())
            {
                Msg_PlayerIdentify_Req msgIdentify = new()
                {
                    PlayerName = name,
                };
                NetworkClient.Instance.EnqueueMessage(EProtoType.PlayerIdentifyReq, msgIdentify);
            }
            else
            {
                _ = NetworkClient.Instance.DisconnectAsync();
                GameHelper_Common.LogDebug("ERR!!! Can't Req Identify: no player name!");
            }
        }

        private void OnPlayerIdentifyRsp(Msg_PlayerIdentify_Rsp msg)
        {
            //GameHelper_Common.LogDebug($"Client: OnPlayerIdentifyRsp playerId:{msg.PlayerId}");

            ClientRoomSystem.Instance.OnPlayerIdentified(msg);
        }

        private void OnClientWillDisconnectRsp(Msg_ClientWillDisconnect_Rsp msg)
        {
            GameHelper_Common.LogDebug($"Client: WillDisconnect");

            _ = NetworkClient.Instance.DisconnectAsync();
        }

        private void OnJoinNtf(Msg_Join_Ntf msg)
        {
            //GameHelper_Common.LogDebug($"Client: OnPlayerJoinNtf:{msg.PlayerIds.GetString()}");

            GameFacade_Client.joinedRoom = true;
        }

        private void OnBattleStart(Msg_BattleStart_Ntf msg)
        {
            var randomSeed = msg.RandomSeed;

            ClientRandomSystem.Instance.BattleStart(randomSeed); // 设置随机数种子
            ClientRoomSystem.Instance.BattleStart();
            ClientFrameSyncSystem.Instance.BattleStart();
            ClientTimerSystem.Instance.BattleStart();
            ClientLogicSystem.Instance.BattleStart();
        }

        private void OnBattleStop(Msg_BattleStop_Ntf msg)
        {
            // 战斗结束，回到 lobby 状态
            ClientRandomSystem.Instance.BattleStop(); // 清除随机数种子
            ClientRoomSystem.Instance.BattleStop();
            ClientFrameSyncSystem.Instance.BattleStop();
            ClientTimerSystem.Instance.BattleStop();
            ClientLogicSystem.Instance.BattleStop();
        }

        private void OnBattlePause(Msg_BattlePause_Rsp msg)
        {
            ClientRoomSystem.Instance.battlePause = msg.IsPause;
        }

        // TODO: battleRoomId

        private void OnCommandNtf(Msg_Command_Ntf msg)
        {
            ClientFrameSyncSystem.Instance.OnSyncCommands(msg);
        }

        private void OnCommandAllRsp(Msg_CommandAll_Rsp msg)
        {
            ClientFrameSyncSystem.Instance.OnSyncCommandsAll(msg);
        }
        #endregion 消息处理函数
    }
}
