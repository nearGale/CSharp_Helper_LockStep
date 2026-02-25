using Game.Shared;
using Game.Shared.Extensions;
using Game.Shared.Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Server
{
    /// <summary>
    /// 在服务器端，玩家的信息
    /// </summary>
    public class ServerPlayerInfo
    {
        /// <summary>
        /// 本次服务器运行时 playerId 
        /// 每次 ID 验证成功时分配一个
        /// </summary>
        public uint playerId;

        /// <summary>
        /// 玩家的名称
        /// </summary>
        public string playerName;

        /// <summary>
        /// 玩家对应的 network Session Id
        /// 换设备登录时会变化
        /// </summary>
        public Guid sessionId;
    }

    public class ServerPlayerSystem : Singleton<ServerPlayerSystem>, IServerSystem
    {
        /// <summary> playerId 生成号 </summary>
        private uint _playerIndex = 0;

        /// <summary>
        /// 字典： playerId => player info
        /// </summary>
        public Dictionary<uint, ServerPlayerInfo> playerId2Info = new();

        /// <summary>
        /// 字典： playerName => player info
        /// </summary>
        public Dictionary<string, ServerPlayerInfo> playerName2Info = new();

        /// <summary>
        /// 字典： player netIdentity netId => player info
        /// </summary>
        public Dictionary<Guid, ServerPlayerInfo> playerSessionId2Info = new();

        #region system func

        public void OnStartServer()
        {
            _playerIndex = 0;
            playerId2Info.Clear();
            playerName2Info.Clear();
            playerSessionId2Info.Clear();
        }

        public void OnStopServer()
        {
            _playerIndex = 0;
            playerId2Info.Clear();
            playerName2Info.Clear();
            playerSessionId2Info.Clear();
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

        /// <summary>
        /// 尝试创建玩家，缓存玩家的id、与playerId、netIdentity的映射关系
        ///     玩家的id：目前是名字
        ///     playerId：本次服务器运行中临时生成
        ///     netIdentity：本次连接的主体，断线重连后会重新分配一个
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public (EIdentifyResult, uint) TryAddPlayer(ClientSession conn, string playerName)
        {
            if (playerName.IsNullOrEmpty())
            {
                return (EIdentifyResult.Failed, 0);
            }

            GameHelper_Common.LogDebug($"Server: TryAddPlayer: {playerName} netId:{conn.SessionId}");

            if (playerName2Info.TryGetValue(playerName, out var elderPlayerInfo))
            {
                // 如果 该玩家已经在服务器中
                //   => 断开原有连接 (A)
                //   => 缓存新的 netIdentity 数据 (B)

                // A
                Guid elderSessionId = Guid.Empty;
                var elderSession = NetworkServer.Instance.GetClientSessionBySessionId(elderPlayerInfo.sessionId);
                if (elderSession != null)
                {
                    elderSessionId = elderSession.SessionId;

                    Msg_ClientWillDisconnect_Rsp msgDisconnect = new();
                    NetworkServer.Instance.SendMessageToClient(elderSessionId, EProtoType.ClientWillDisconnectRsp, msgDisconnect);
                }

                // B:
                RefreshPlayerInfoCache(conn, elderPlayerInfo, elderSessionId);

                return (EIdentifyResult.Replace, elderPlayerInfo.playerId);
            }
            else
            {
                // 新登录的 玩家，缓存 playerInfo
                _playerIndex++;
                var playerInfo = new ServerPlayerInfo()
                {
                    playerId = _playerIndex,
                    playerName = playerName,
                    sessionId = conn.SessionId,
                };
                playerId2Info.Add(_playerIndex, playerInfo);
                playerName2Info.Add(playerName, playerInfo);
                playerSessionId2Info.Add(conn.SessionId, playerInfo);
                return (EIdentifyResult.Succeed, _playerIndex);
            }
        }

        /// <summary>
        /// 根据 连接的Id，获取 玩家ID
        /// </summary>
        /// <param name="netId">网络连接的 id</param>
        public uint GetPlayerIdBySessionId(Guid sessionId)
        {
            if (playerSessionId2Info.TryGetValue(sessionId, out var playerInfo) && playerInfo != null)
            {
                return playerInfo.playerId;
            }

            return 0; // player id 不会为0， 0是非法值
        }


        /// <summary>
        /// 重连时，刷新对 playerInfo 的映射缓存
        /// </summary>
        private void RefreshPlayerInfoCache(ClientSession conn, ServerPlayerInfo playerInfo, Guid elderSessionId)
        {
            // playerId2Info : playerId 不变
            // playerName2Info : playerName 不变

            //playerNetId2Info : 移除老的，替换成新的
            playerSessionId2Info.Remove(elderSessionId);
            playerSessionId2Info.Add(conn.SessionId, playerInfo);

            // 旧的 netIdentity 已经要求断开，存下新的 netIdentity
            playerInfo.sessionId = conn.SessionId;

            GameHelper_Common.LogDebug($"Server: RefreshPlayerInfoCache: " +
                $"name:{playerInfo.playerName} " +
                $"id:{playerInfo.playerId} " +
                $"netId:{elderSessionId} -> {conn.SessionId}");
        }
    }
}
