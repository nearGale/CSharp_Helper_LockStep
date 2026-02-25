using Game.Shared.Proto;
using Game.Shared.ProtoBuf;
using Google.Protobuf;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Shared
{
    public static class Protocol
    {
        static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // 发送：4 字节大端长度 + ProtoBuf
        public static async Task SendAsync(NetworkStream stream, EProtoType eProtoType, IMessage message, CancellationToken ct = default)
        {
            var payload_header = CommonUtil.ProtoHeaderSerialize(eProtoType);
            var lenBuf_header = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(lenBuf_header, payload_header.Length);

            var payload = CommonUtil.ProtoSerialize(message);
            var lenBuf = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(lenBuf, payload.Length);
            
            await stream.WriteAsync(lenBuf_header, 0, 4, ct).ConfigureAwait(false);
            await stream.WriteAsync(payload_header, 0, payload_header.Length, ct).ConfigureAwait(false);
            await stream.WriteAsync(lenBuf, 0, 4, ct).ConfigureAwait(false);
            await stream.WriteAsync(payload, 0, payload.Length, ct).ConfigureAwait(false);
            await stream.FlushAsync(ct).ConfigureAwait(false);
            GameHelper_Common.LogDebug($"Header:{payload_header.Length},Msg:{payload.Length}");
        }

        // 接收：读取长度后读取负载，返回具体 IMessage 实例
        public static async Task<IMessage> ReceiveAsync(NetworkStream stream, CancellationToken ct = default)
        {
            // 1. 先读协议头长度前缀
            var lenBuf_header = new byte[4];
            await ReadExactAsync(stream, lenBuf_header, 0, 4, ct).ConfigureAwait(false);
            var len_header = BinaryPrimitives.ReadInt32BigEndian(lenBuf_header);
            if (len_header <= 0) throw new InvalidDataException("Header:Invalid message length");

            // 2. 读取协议头
            var payload_header = new byte[len_header];
            await ReadExactAsync(stream, payload_header, 0, len_header, ct).ConfigureAwait(false);

            // 3. 先解析协议头
            var header = MsgHeader.Parser.ParseFrom(payload_header);
            int msgId = header.MsgId;
            long playerId = header.PlayerId;

            GameHelper_Common.LogDebug($"header:{header.MsgId}");

            // 4. 先读协议主体长度前缀
            var lenBuf = new byte[4];
            await ReadExactAsync(stream, lenBuf, 0, 4, ct).ConfigureAwait(false);

            IMessage msg;
            var parser = ProtoBufMsgResolver.Instance.GetParser(msgId);

            var len = BinaryPrimitives.ReadInt32BigEndian(lenBuf);
            var payload = new byte[len];
            if (len > 0)
            {
                // 2. 读取协议主体
                await ReadExactAsync(stream, payload, 0, len, ct).ConfigureAwait(false);

                // 3. 根据MsgId获取解析器，解析消息体
                msg = parser.ParseFrom(payload); // 解析出具体的结构体
            }
            else
            {
                msg = parser.ParseFrom(payload); // 解析出具体的结构体
            }


            GameHelper_Common.LogDebug($"收到消息: {msgId}/{msg.GetType()}/{msg}");
            return msg;
        }

        static async Task ReadExactAsync(Stream s, byte[] buffer, int offset, int count, CancellationToken ct)
        {
            var read = 0;
            while (read < count)
            {
                var n = await s.ReadAsync(buffer, offset + read, count - read, ct).ConfigureAwait(false);
                if (n == 0) throw new EndOfStreamException();
                read += n;
            }
        }
    }
}