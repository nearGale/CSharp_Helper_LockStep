using Game.Shared;
using Game.Shared.Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Client
{
    public enum ERTTState
    {
        None, // 未开始
        WaitingRsp, // 等待回包中
        BeforeSend, // 发送间隔中
    }

    public class Client_RTT
    {
        /// <summary> RTT，单位 秒 </summary>
        private float _rttVal;

        /// <summary> RTT 请求时间 </summary>
        private float _rttReqTime;

        /// <summary> RTT 回包时间 </summary>
        private float _rttRspTime;

        /// <summary>
        /// rtt 状态
        /// </summary>
        private ERTTState _eRttState;

        public void ClearRttData()
        {
            _rttVal = 0;
            ClearRttRoundData();
        }

        public void ClearRttRoundData()
        {
            _rttReqTime = 0;
            _rttRspTime = 0;
            _eRttState = ERTTState.None;
        }

        /// <summary>
        /// 检查并更新 RTT 数据
        /// </summary>
        public void LogicUpdate()
        {
            if (ClientRoomSystem.Instance.GetClientInRoomState() == ERoomState.UnConnected)
            {
                ClearRttData();
                return;
            }

            switch (_eRttState)
            {
                case ERTTState.None:
                    // 发起 rtt 请求
                    RttReq();
                    _rttReqTime = TimeUtility.time;
                    _eRttState = ERTTState.WaitingRsp;
                    break;
                case ERTTState.WaitingRsp:
                    break;
                case ERTTState.BeforeSend:
                    if (TimeUtility.time - _rttRspTime > 5)
                    {
                        _eRttState = ERTTState.None; // 完成一个轮回
                        ClearRttRoundData();
                    }
                    break;
                default:
                    break;
            }
        }

        private void RttReq()
        {
            Msg_PingPong_Req msg = new();
            NetworkClient.Instance.EnqueueMessage(EProtoType.PingPongReq, msg);
        }

        public void OnRttRsp()
        {
            _rttRspTime = TimeUtility.time;

            _rttVal = _rttRspTime - _rttReqTime;
            _eRttState = ERTTState.BeforeSend;
            GameHelper_Common.LogDebug($"RTT:{_rttVal}");
        }

        public float GetValue()
        {
            return _rttVal;
        }

    }
}
