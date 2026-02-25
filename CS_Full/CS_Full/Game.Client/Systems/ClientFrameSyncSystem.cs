using Game.Shared;
using Game.Shared.Proto;
using Google.Protobuf.Collections;

namespace Game.Client
{
    /// <summary>
    /// 此处放置 客户端 同步帧数据，当帧模拟
    /// </summary>
    public class ClientFrameSyncSystem : Singleton<ClientFrameSyncSystem>, IClientSystem
    {
        /// <summary>
        /// 字典：帧 -> 这一帧的指令集合
        /// </summary>
        public Dictionary<ulong, OneFrameCommands> frameCommandDict = new();

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
            ProcessTick();
        }
        #endregion

        private void ClearData()
        {
            frameCommandDict.Clear();
        }

        /// <summary>
        /// 战斗房间开始时
        /// </summary>
        public void BattleStart()
        {
            ClearData();
        }

        /// <summary>
        /// 战斗房间结束时
        /// </summary>
        public void BattleStop()
        {
            ClearData();
        }

        private void ProcessTick()
        {
            var clientTick = GameHelper_Client.GetClientTick();
            var battleServerTick = GameHelper_Client.GetBattleServerTick();

            if (frameCommandDict.Remove(clientTick, out var oneFramCommands))
            {
                foreach (var cmd in oneFramCommands.Details)
                {
                    ProcessCommand(clientTick, cmd);
                }
            }
        }

        public void OnSyncCommands(Msg_Command_Ntf msg)
        {
            ClientTimerSystem.Instance.battleServerTick = msg.CurBattleServerTick;
            DoSyncCommand(msg.CurBattleServerTick, msg.CommandsSet);

            if (GameFacade_Common.enableCommandSnapshot)
            {
                GameHelper_Common.FileLog(GameFacade_Common.commandSnapshotLogName, $"OnSyncCommands serverTick:{msg.CurBattleServerTick}");
            }
        }

        public void OnSyncCommandsAll(Msg_CommandAll_Rsp msg)
        {

            ClientTimerSystem.Instance.battleServerTick = msg.SyncedBattleServerTick;
            DoSyncCommand(msg.SyncedBattleServerTick, msg.CommandsSet);

            if (GameFacade_Common.enableCommandSnapshot)
            {
                GameHelper_Common.FileLog(GameFacade_Common.commandSnapshotLogName, $"OnSyncCommandsAll serverTick:{msg.SyncedBattleServerTick}");
            }
        }

        /// <summary>
        /// 将同步指令存下
        /// </summary>
        /// <param name="curBattleServerTick">同步到的战斗服务器最大帧</param>
        /// <param name="commandsSet">指令集合</param>
        private void DoSyncCommand(ulong curBattleServerTick, RepeatedField<OneFrameCommands> commandsSet)
        {
            //GameHelper_Common.UILog($"Client: Rcv: {clientTick} {Time.time}");

            var clientTick = GameHelper_Client.GetClientTick();

            foreach (var command in commandsSet) // command 是一帧的指令集合
            {
                GameHelper_Common.LogDebug($"Client: Rcv: {command.ServerTick} at {clientTick}/{GameHelper_Client.GetBattleServerTick()}");
                if (!frameCommandDict.ContainsKey(command.ServerTick))
                {
                    frameCommandDict.Add(command.ServerTick, command);
                }
                else
                {
                    GameHelper_Common.LogError("ERR!!! 有重复帧的指令");
                }
            }
        }

        private void ProcessCommand(ulong tick, CommandDetail detail)
        {
            ClientLogicSystem.Instance.ProcessCommand(detail);
            GameHelper_Common.LogDebug($"Client: ProcessCommand:{detail.PlayerId} {detail.ECommand} at tick:{tick}");
        }
    }
}
