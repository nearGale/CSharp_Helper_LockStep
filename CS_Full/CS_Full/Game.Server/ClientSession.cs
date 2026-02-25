using Game.Shared;
using Game.Shared.Proto;
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server
{
    /// <summary>
    /// 客户端会话类（每个客户端连接对应一个实例）
    /// 负责单个客户端的消息接收、发送队列管理、连接状态维护
    /// </summary>
    public class ClientSession : IDisposable
    {

        #region 基础属性
        /// <summary> 会话唯一标识（用于区分不同客户端）</summary>
        public Guid SessionId { get; } = Guid.NewGuid();

        /// <summary> 客户端远程地址（IP+端口）</summary>
        public string RemoteEndPoint => _tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";

        /// <summary> 当前会话是否处于连接状态 </summary>
        public bool IsConnected => _tcpClient.Connected && _cts != null && !_cts.IsCancellationRequested;
        #endregion 基础属性


        #region 核心私有字段
        /// <summary> 客户端TCP连接对象 </summary>
        private readonly TcpClient _tcpClient;

        /// <summary> 网络流（用于读写数据）/// </summary>
        private readonly NetworkStream _networkStream;

        /// <summary> 取消令牌源（控制当前会话的异步任务） </summary>
        private readonly CancellationTokenSource _cts;

        /// <summary> 发送消息队列（线程安全，避免多线程并发写入/读取冲突）</summary>
        private readonly ConcurrentQueue<(EProtoType, IMessage)> _sendQueue = new ConcurrentQueue<(EProtoType, IMessage)>();

        /// <summary> 异步任务：接收消息循环 </summary>
        private Task? _recvTask;

        /// <summary> 异步任务：发送消息循环 /// </summary>
        private Task? _sendLoopTask;
        #endregion 核心私有字段


        #region 事件（解耦业务逻辑）
        /// <summary>
        /// 接收到客户端消息时触发（透传给NetworkServer）
        /// </summary>
        public event Action<ClientSession, IMessage>? OnMessageReceived;

        /// <summary>
        /// 会话断开时触发（通知NetworkServer移除该会话）
        /// </summary>
        public event Action<ClientSession>? OnDisconnected;
        #endregion 事件（解耦业务逻辑）


        #region 构造函数
        /// <summary>
        /// 初始化客户端会话
        /// </summary>
        /// <param name="tcpClient">已建立的TCP客户端连接</param>
        /// <exception cref="ArgumentNullException">TCP连接为空时抛出</exception>
        public ClientSession(TcpClient tcpClient)
        {
            _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient), "TCP客户端连接不能为空");
            _networkStream = tcpClient.GetStream();
            _cts = new CancellationTokenSource();

            // 启动收发核心循环
            StartReceiveLoop();
            StartSendLoop();

            Console.WriteLine($"[{SessionId}] 客户端会话已创建，远程地址：{RemoteEndPoint}");
        }
        #endregion 构造函数



        #region 发送消息（对外API）
        /// <summary>
        /// 将消息加入发送队列（非阻塞，由发送循环异步发送）
        /// </summary>
        /// <typeparam name="T">Proto消息类型（如S2CPong、S2CChat）</typeparam>
        /// <param name="protoType">消息类型枚举（与客户端一致）</param>
        /// <param name="message">消息对象</param>
        public void EnqueueMessage(EProtoType protoType, IMessage message)
        {
            // 校验连接状态
            if (!IsConnected)
            {
                Console.WriteLine($"[{SessionId}] 会话已断开，消息[{protoType}]入队失败");
                return;
            }

            // 校验消息对象
            if (message == null)
            {
                Console.WriteLine($"[{SessionId}] 消息对象为空，入队失败");
                return;
            }

            // 消息入队（ConcurrentQueue自动处理线程安全）
            _sendQueue.Enqueue((protoType, message));
            Console.WriteLine($"[{SessionId}] 消息[{protoType}]已入队，当前队列长度：{_sendQueue.Count}");
        }
        #endregion 发送消息（对外API）


        #region 接收消息循环（内部核心）
        /// <summary>
        /// 启动接收消息循环（持续读取客户端发送的消息）
        /// </summary>
        private void StartReceiveLoop()
        {
            _recvTask = Task.Run(async () =>
            {
                try
                {
                    while (!_cts.Token.IsCancellationRequested && IsConnected)
                    {
                        // 调用共用的Protocol.ReceiveAsync读取完整消息（处理粘包/半包）
                        var msg = await Protocol.ReceiveAsync(_networkStream, _cts.Token)
                            .ConfigureAwait(false);

                        // 触发消息接收事件，交给NetworkServer处理业务逻辑
                        OnMessageReceived?.Invoke(this, msg);
                    }
                }
                catch (OperationCanceledException)
                {
                    // 正常取消（主动断开），无需打印异常
                    Console.WriteLine($"[{SessionId}] 接收循环已取消");
                }
                catch (Exception ex)
                {
                    // 异常断开（网络错误/客户端强制关闭）
                    Console.WriteLine($"[{SessionId}] 接收消息异常：{ex.Message}");
                }
                finally
                {
                    // 无论正常/异常，最终触发断开事件
                    await DisconnectAsync().ConfigureAwait(false);
                }
            }, _cts.Token);
        }
        #endregion 接收消息循环（内部核心）


        #region 发送消息循环（内部核心）
        /// <summary>
        /// 启动发送消息循环（持续从队列取消息并发送给客户端）
        /// </summary>
        private void StartSendLoop()
        {
            _sendLoopTask = Task.Run(async () =>
            {
                try
                {
                    while (!_cts.Token.IsCancellationRequested && IsConnected)
                    {
                        // 尝试从队列取出消息
                        if (_sendQueue.TryDequeue(out var msgItem))
                        {
                            var (protoType, message) = msgItem;
                            try
                            {
                                // 调用共用的Protocol.SendAsync发送消息（带长度前缀，兼容客户端）
                                await Protocol.SendAsync(_networkStream, protoType, message, _cts.Token)
                                    .ConfigureAwait(false);
                                Console.WriteLine($"[{SessionId}] 发送消息：{protoType}/{message}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[{SessionId}] 发送消息[{protoType}]失败：{ex.Message}");
                                // 可选：发送失败后重新入队重试（根据业务需求开启）
                                // _sendQueue.Enqueue(msgItem);
                                // await Task.Delay(1000, _cts.Token); // 延迟1秒重试
                            }
                        }
                        else
                        {
                            // 队列为空时短暂休眠（10ms），避免空循环占用CPU
                            await Task.Delay(10, _cts.Token);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"[{SessionId}] 发送循环已取消");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{SessionId}] 发送循环异常：{ex.Message}");
                }
            }, _cts.Token);
        }
        #endregion 发送消息循环（内部核心）



        #region 断开连接（对外API）
        /// <summary>
        /// 主动断开客户端连接（释放资源+触发断开事件）
        /// </summary>
        public async Task DisconnectAsync()
        {
            // 避免重复断开
            if (!IsConnected) return;

            // 取消所有异步任务
            _cts.Cancel();

            // 等待收发循环任务结束
            if (_recvTask != null)
                await _recvTask.ConfigureAwait(false);
            if (_sendLoopTask != null)
                await _sendLoopTask.ConfigureAwait(false);

            // 释放网络资源
            _networkStream.Dispose();
            _tcpClient.Dispose();
            _cts.Dispose();

            // 触发断开事件（通知NetworkServer移除该会话）
            OnDisconnected?.Invoke(this);
            Console.WriteLine($"[{SessionId}] 客户端会话已断开，远程地址：{RemoteEndPoint}");
        }
        #endregion 断开连接（对外API）


        #region 资源释放（IDisposable）
        /// <summary>
        /// 释放会话所有资源（同步调用断开连接）
        /// </summary>
        public void Dispose()
        {
            // 同步调用异步断开方法（忽略返回值）
            _ = DisconnectAsync();
        }
        #endregion 资源释放（IDisposable）



        readonly TcpClient _tcp;
        readonly NetworkStream _stream;
        //readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public string Id { get; } = Guid.NewGuid().ToString();

        //public ClientSession(TcpClient tcp)
        //{
        //    _tcp = tcp;
        //    _stream = tcp.GetStream();
        //}

        public async Task RunAsync()
        {
            try
            {
                // 发送欢迎消息
                C2SChat msgChat = new C2SChat();
                msgChat.From = "Server";
                msgChat.Text = $"Welcome {Id}";
                await Protocol.SendAsync(_stream, EProtoType.ProtoC2Schat, msgChat, _cts.Token);

                while (!_cts.Token.IsCancellationRequested)
                {
                    var msg = await Protocol.ReceiveAsync(_stream, _cts.Token);
                    await HandleMessageAsync(msg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Session {Id} ended: {ex.Message}");
            }
            finally
            {
                _tcp.Close();
            }
        }

        Task HandleMessageAsync(IMessage msg)
        {
            switch (msg)
            {
                case C2SPing p:
                    Console.WriteLine($"[{Id}] Ping at {p.TimeStamp:o}");

                    S2CPong msgPong = new S2CPong();
                    msgPong.TimeStamp = GameHelper_Common.GetCurTimeStamp();
                    return Protocol.SendAsync(_stream, EProtoType.ProtoS2Cpong, msgPong, _cts.Token);
                case C2SChat c:
                    Console.WriteLine($"[{Id}] {c.From}: {c.Text}");
                    return Task.CompletedTask;
                default:
                    Console.WriteLine($"[{Id}] Unknown message {msg}");
                    //Console.WriteLine($"[{Id}] Unknown message {msg.Type}");
                    return Task.CompletedTask;
            }
        }

        public void Stop() => _cts.Cancel();
    }
}