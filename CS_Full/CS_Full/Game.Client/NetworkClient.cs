using Game.Shared;
using Game.Shared.Proto;
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Game.Client
{
    internal class NetworkClient : Singleton<NetworkClient>
    {
        // TCP相关
        private TcpClient? _tcpClient;
        private NetworkStream? _networkStream;
        private CancellationTokenSource? _cts;

        // 消息队列（线程安全）
        private readonly ConcurrentQueue<(EProtoType, IMessage)> _sendQueue = new ConcurrentQueue<(EProtoType, IMessage)>();

        // 异步任务
        private Task? _recvTask;
        private Task? _sendLoopTask;
        private Task? _heartbeatTask;

        // 状态标识
        public bool IsConnected => _tcpClient?.Connected ?? false;


        #region 事件（解耦业务逻辑）
        /// <summary> 消息接收事件 </summary>
        public event Action<IMessage>? OnMessageReceived;
        
        /// <summary> 连接状态变更事件 </summary>
        public event Action<bool>? OnConnectionStateChanged;
        #endregion


        #region 公开API
        /// <summary>
        /// 连接服务器
        /// </summary>
        public async Task ConnectAsync(string ip, int port)
        {
            if (IsConnected)
                throw new InvalidOperationException("已连接到服务器，无需重复连接");

            // 初始化取消令牌和TCP客户端
            _cts = new CancellationTokenSource();
            _tcpClient = new TcpClient();

            try
            {
                await _tcpClient.ConnectAsync(ip, port);
                _networkStream = _tcpClient.GetStream();

                // 启动核心循环
                StartReceiveLoop();
                StartSendLoop();
                StartHeartbeatLoop();

                // 触发连接成功事件
                OnConnectionStateChanged?.Invoke(true);
                Console.WriteLine("连接服务器成功");
            }
            catch (Exception ex)
            {
                DisposeResources();
                OnConnectionStateChanged?.Invoke(false);
                Console.WriteLine($"连接失败：{ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// 将消息加入发送队列（非阻塞）
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="protoType">消息类型枚举</param>
        /// <param name="message">消息对象</param>
        public void EnqueueMessage(EProtoType protoType, IMessage message)
        {
            if (!IsConnected)
            {
                Console.WriteLine("未连接到服务器，消息发送失败");
                return;
            }

            // 将消息加入队列（ConcurrentQueue自动处理线程安全）
            _sendQueue.Enqueue((protoType, message));
            //Console.WriteLine($"消息已入队：{protoType}");
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public async Task DisconnectAsync()
        {
            // 取消所有异步任务
            _cts?.Cancel();

            // 等待所有任务结束
            if (_recvTask != null) await _recvTask.ConfigureAwait(false);
            if (_sendLoopTask != null) await _sendLoopTask.ConfigureAwait(false);
            if (_heartbeatTask != null) await _heartbeatTask.ConfigureAwait(false);

            // 释放资源
            DisposeResources();

            // 清空队列
            _sendQueue.Clear();

            // 触发断开事件
            OnConnectionStateChanged?.Invoke(false);
            Console.WriteLine("已断开与服务器的连接");
        }

        #endregion 公开API


        #region 内部循环逻辑
        /// <summary>
        /// 接收消息循环
        /// </summary>
        private void StartReceiveLoop()
        {
            if (_networkStream == null || _cts == null) return;

            _recvTask = Task.Run(async () =>
            {
                try
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        var msg = await Protocol.ReceiveAsync(_networkStream, _cts.Token)
                            .ConfigureAwait(false);
                        // 触发消息接收事件，交给业务层处理
                        OnMessageReceived?.Invoke(msg);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Console.WriteLine($"接收消息异常：{ex.Message}");
                    await DisconnectAsync().ConfigureAwait(false);
                }
            }, _cts.Token);
        }

        /// <summary>
        /// 主发送循环（核心：从队列取消息并发送）
        /// </summary>
        private void StartSendLoop()
        {
            if (_networkStream == null || _cts == null) return;

            _sendLoopTask = Task.Run(async () =>
            {
                try
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        // 尝试从队列取出消息
                        if (_sendQueue.TryDequeue(out var msgItem))
                        {
                            var (protoType, message) = msgItem;
                            try
                            {
                                // 发送消息
                                await Protocol.SendAsync(_networkStream, protoType, message, _cts.Token)
                                    .ConfigureAwait(false);
                                Console.WriteLine($"发送消息：{protoType}/{message}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"发送消息失败：{protoType}，{ex.Message}");
                                // 发送失败可选择重新入队（可选）
                                // _sendQueue.Enqueue(msgItem);
                                // await Task.Delay(1000); // 延迟重试
                            }
                        }
                        else
                        {
                            // 队列为空时短暂休眠，避免空循环占用CPU
                            await Task.Delay(10, _cts.Token);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Console.WriteLine($"发送循环异常：{ex.Message}");
                    await DisconnectAsync().ConfigureAwait(false);
                }
            }, _cts.Token);
        }

        /// <summary>
        /// 心跳循环（通过队列发送心跳包）
        /// </summary>
        private void StartHeartbeatLoop()
        {
            return;
            if (!IsConnected || _cts == null) return;

            _heartbeatTask = Task.Run(async () =>
            {
                try
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        // 心跳包也加入队列，统一由发送循环处理
                        var pingMsg = new C2SPing
                        {
                            TimeStamp = GameHelper_Common.GetCurTimeStamp()
                        };
                        EnqueueMessage(EProtoType.ProtoC2Sping, pingMsg);

                        // 每5秒发送一次心跳
                        await Task.Delay(TimeSpan.FromSeconds(5), _cts.Token);
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Console.WriteLine($"心跳循环异常：{ex.Message}");
                }
            }, _cts.Token);
        }
        #endregion

        #region 资源释放
        private void DisposeResources()
        {
            _networkStream?.Dispose();
            _tcpClient?.Dispose();
            _cts?.Dispose();

            _networkStream = null;
            _tcpClient = null;
            _cts = null;
            _recvTask = null;
            _sendLoopTask = null;
            _heartbeatTask = null;
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _ = DisconnectAsync();
            DisposeResources();
        }

        #endregion 资源释放

    }
}
