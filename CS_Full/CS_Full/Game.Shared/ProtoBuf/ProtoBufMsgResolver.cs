using Game.Shared.Proto;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared.ProtoBuf
{
    /// <summary>
    /// 消息解析器（单例，全局复用）
    /// </summary>
    public class ProtoBufMsgResolver
    {
        // 核心映射表：MsgId → Protobuf消息的解析器
        private Dictionary<int, MessageParser> _msgParserMap = new Dictionary<int, MessageParser>();

        // 单例实例
        public static ProtoBufMsgResolver Instance { get; } = new ProtoBufMsgResolver();

        // 构造函数：初始化映射表
        private ProtoBufMsgResolver()
        {
            // 注册所有消息的解析器（Google.Protobuf自带的Parser属性）
            _msgParserMap.Add((int)EProtoType.ProtoC2Sping, C2SPing.Parser);
            _msgParserMap.Add((int)EProtoType.ProtoS2Cpong, S2CPong.Parser);
            _msgParserMap.Add((int)EProtoType.ProtoC2Schat, C2SChat.Parser);
            _msgParserMap.Add((int)EProtoType.PingPongReq, Msg_PingPong_Req.Parser);
            _msgParserMap.Add((int)EProtoType.PingPongRsp, Msg_PingPong_Rsp.Parser);
            _msgParserMap.Add((int)EProtoType.PlayerConnectRsp, Msg_PlayerConnect_Rsp.Parser);
            _msgParserMap.Add((int)EProtoType.PlayerIdentifyReq, Msg_PlayerIdentify_Req.Parser);
            _msgParserMap.Add((int)EProtoType.PlayerIdentifyRsp, Msg_PlayerIdentify_Rsp.Parser);
            _msgParserMap.Add((int)EProtoType.ClientWillDisconnectRsp, Msg_ClientWillDisconnect_Rsp.Parser);
            _msgParserMap.Add((int)EProtoType.JoinNtf, Msg_Join_Ntf.Parser);
            _msgParserMap.Add((int)EProtoType.BattleStartReq, Msg_BattleStart_Req.Parser);
            _msgParserMap.Add((int)EProtoType.BattleStartNtf, Msg_BattleStart_Ntf.Parser);
            _msgParserMap.Add((int)EProtoType.BattleStopReq, Msg_BattleStop_Req.Parser);
            _msgParserMap.Add((int)EProtoType.BattleStopNtf, Msg_BattleStop_Ntf.Parser);
            _msgParserMap.Add((int)EProtoType.BattlePauseReq, Msg_BattlePause_Req.Parser);
            _msgParserMap.Add((int)EProtoType.BattleResumeReq, Msg_BattleResume_Req.Parser);
            _msgParserMap.Add((int)EProtoType.BattlePauseRsp, Msg_BattlePause_Rsp.Parser);
            _msgParserMap.Add((int)EProtoType.CommandReq, Msg_Command_Req.Parser);
            _msgParserMap.Add((int)EProtoType.CommandNtf, Msg_Command_Ntf.Parser);
            _msgParserMap.Add((int)EProtoType.CommandAllRsp, Msg_CommandAll_Rsp.Parser);
        }

        // 根据MsgId获取对应的解析器
        public MessageParser GetParser(int msgId)
        {
            if (_msgParserMap.TryGetValue(msgId, out var parser))
            {
                return parser;
            }
            throw new ArgumentException($"未找到MsgId={msgId}对应的Protobuf解析器");
        }
    }
}
