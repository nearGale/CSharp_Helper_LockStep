using Game.Shared.Proto;
using Google.Protobuf;

namespace Game.Shared
{
    public partial class CommonUtil
    {
        public static byte[] ProtoHeaderSerialize(EProtoType eProtoType)
        {
            // 2. 构造协议头（指定MsgId）
            var header = new MsgHeader
            {
                MsgId = (int)eProtoType,
                Channel = 1, // TCP通道
                PlayerId = 0 // 未登录时为0
            };
            GameHelper_Common.LogDebug($"header:{header.MsgId}");

            using (MemoryStream output = new MemoryStream())
            {
                header.WriteTo(output);
                return output.ToArray();
            }
        }

        public static byte[] ProtoSerialize(IMessage msg)
        {
            using (MemoryStream output = new MemoryStream())
            {
                msg.WriteTo(output);
                return output.ToArray();
            }
        }

        //public static T ProtoDeserialize<T>(byte[] bytes) where T : IMessage, new()
        //{
        //    T desErrorLog = T.Parser.ParseFrom(bytes);
        //    return desErrorLog;
        //}
    }
}
