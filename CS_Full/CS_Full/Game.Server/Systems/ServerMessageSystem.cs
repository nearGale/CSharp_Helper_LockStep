using Game.Server;
using Game.Shared;
using Game.Shared.Extensions;
using Game.Shared.Proto;
using Google.Protobuf.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Server
{
    /// <summary>
    /// 服务端消息处理
    /// </summary>
    public class ServerMessageSystem : Singleton<ServerMessageSystem>, IServerSystem
    {
        /// <summary>
        ///  容器：消息类型 -> 对应的处理委托（Action<T> 存为 Delegate）
        /// </summary>
        private readonly Dictionary<Type, Delegate> _messageHandlers = new();

        #region system func

        public void OnStartServer()
        {
            RegisterMessageHandler();
        }

        public void OnStopServer()
        {
            UnRegisterMessageHandler();
        }

        public void Start() { }

        public void Update() { }

        public void LogicUpdate() { }

        #endregion

        /// <summary>
        /// 注册消息回调
        /// </summary>
        public void RegisterMessageHandler()
        {
            RegisterHandler<Msg_PingPong_Req>(OnPingPongReq);

            RegisterHandler<Msg_PlayerIdentify_Req>(OnPlayerIdentifyReq);
            RegisterHandler<Msg_BattleStart_Req>(OnBattleStartReq);
            RegisterHandler<Msg_BattleStop_Req>(OnBattleStopReq);

            RegisterHandler<Msg_BattlePause_Req>(OnBattlePauseReq);
            RegisterHandler<Msg_BattleResume_Req>(OnBattleResumeReq);
            RegisterHandler<Msg_Command_Req>(OnCommandReq);
        }

        public void UnRegisterMessageHandler()
        {
            UnregisterHandler<Msg_PlayerIdentify_Req>();
            UnregisterHandler<Msg_BattleStart_Req>();
            UnregisterHandler<Msg_BattleStop_Req>();

            UnregisterHandler<Msg_BattlePause_Req>();
            UnregisterHandler<Msg_BattleResume_Req>();
            UnregisterHandler<Msg_Command_Req>();
        }

        private void RegisterHandler<T>(Action<ClientSession, T> handler)
        {
            _messageHandlers[typeof(T)] = handler;
        }

        private void UnregisterHandler<T>()
        {
            _messageHandlers.Remove(typeof(T));
        }

        public void HandleMessage<T>(ClientSession conn, T msg)
        {
            if (_messageHandlers.TryGetValue(msg.GetType(), out var method))
            {
                method.DynamicInvoke(conn, msg);
            }
        }

        #region 消息回调

        private void OnPingPongReq(ClientSession conn, Msg_PingPong_Req msg)
        {
            Msg_PingPong_Rsp msgRsp = new();
            NetworkServer.Instance.SendMessageToClient(conn.SessionId, EProtoType.PingPongRsp, msgRsp);
        }

        private void OnPlayerIdentifyReq(ClientSession conn, Msg_PlayerIdentify_Req msg)
        {
            var (eResult, playerId) = ServerPlayerSystem.Instance.TryAddPlayer(conn, msg.PlayerName);

            Msg_PlayerIdentify_Rsp msgRsp = new()
            {
                Result = eResult,
                PlayerId = playerId
            };
            NetworkServer.Instance.SendMessageToClient(conn.SessionId, EProtoType.PlayerIdentifyRsp, msgRsp);

            if (eResult == EIdentifyResult.Failed)
            {
                Msg_ClientWillDisconnect_Rsp msgDisconnect = new();
                NetworkServer.Instance.SendMessageToClient(conn.SessionId, EProtoType.ClientWillDisconnectRsp, msgRsp);
                return;
            }

            // 如果在战斗中，向客户端同步所有消息
            if (GameHelper_Server.IsInBattleRoom())
            {
                GameHelper_Server.NotifyBattleStart(conn);
                ServerCommandStorageSystem.Instance.SyncAllCommands(conn);
            }

            var ids = ServerPlayerSystem.Instance.playerId2Info.Keys;
            Msg_Join_Ntf msgNtf = new Msg_Join_Ntf();
            msgNtf.PlayerIds.AddRange(ids);

            NetworkServer.Instance.BroadcastMessage(EProtoType.JoinNtf, msgNtf);
            GameHelper_Common.LogDebug($"Server: DoPlayerJoinNtf:{ids.GetString()}");
        }

        private void OnBattleStartReq(ClientSession conn, Msg_BattleStart_Req msg)
        {
            if (ServerLogicSystem.Instance.eRoomState == EServerRoomState.InBattle)
                return;

            ServerTimerSystem.Instance.StartBattle();
            ServerCommandStorageSystem.Instance.StartBattle();
            ServerCommandSyncSystem.Instance.StartBattle();
            ServerLogicSystem.Instance.StartBattle();

            GameHelper_Server.NotifyBattleStart();
        }

        private void OnBattleStopReq(ClientSession conn, Msg_BattleStop_Req msg)
        {
            ServerTimerSystem.Instance.StopBattle();
            ServerCommandStorageSystem.Instance.StopBattle();
            ServerCommandSyncSystem.Instance.StopBattle();
            ServerLogicSystem.Instance.StopBattle();

            GameHelper_Server.NotifyBattleStop();
        }

        private void OnBattlePauseReq(ClientSession conn, Msg_BattlePause_Req msg)
        {
            ServerTimerSystem.Instance.battlePause = true;
            Msg_BattlePause_Rsp msgRsp = new()
            {
                IsPause = true
            };
            NetworkServer.Instance.BroadcastMessage(EProtoType.BattlePauseRsp, msgRsp);
        }

        private void OnBattleResumeReq(ClientSession conn, Msg_BattleResume_Req msg)
        {
            ServerTimerSystem.Instance.battlePause = false;

            Msg_BattlePause_Rsp msgRsp = new()
            {
                IsPause = false
            };
            NetworkServer.Instance.BroadcastMessage(EProtoType.BattlePauseRsp, msgRsp);
        }

        private void OnCommandReq(ClientSession conn, Msg_Command_Req msg)
        {
            var playerId = ServerPlayerSystem.Instance.GetPlayerIdBySessionId(conn.SessionId);
            if (playerId == 0) return;

            ServerCommandSyncSystem.Instance.CacheClientCommand(playerId, msg.ECommand);
        }

        #endregion


    }
}
