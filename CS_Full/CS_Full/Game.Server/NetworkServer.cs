using Game.Shared;
using Game.Shared.Proto;
using Google.Protobuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Game.Server
{
    /// <summary>
    /// 单例网络服务器（对应客户端NetworkClient）
    /// </summary>
    public sealed class NetworkServer : Singleton<NetworkServer>
    {

        #region 核心字段
        /// <summary> TCP监听器 </summary>        
        private TcpListener? _tcpListener;

        /// <summary> 取消令牌（控制服务器整体任务） </summary>
        private CancellationTokenSource? _cts;
        
        /// <summary> 监听客户端连接的任务 </summary>        
        private Task? _listenTask;

        /// <summary> 客户端会话字典（线程安全，Key=SessionId，Value=ClientSession）/// </summary>
        private readonly ConcurrentDictionary<Guid, ClientSession> _clientSessions = new();
        #endregion 核心字段


        #region 事件（解耦业务逻辑）
        /// <summary> 客户端连接事件 </summary>
        public event Action<ClientSession>? OnClientConnected;

        /// <summary> 客户端断开事件 </summary>
        public event Action<ClientSession>? OnClientDisconnected;

        /// <summary> 接收客户端消息事件 </summary>
        public event Action<ClientSession, IMessage>? OnClientMessageReceived;
        #endregion 事件（解耦业务逻辑）


        /// <summary>
        /// 服务器是否正在运行
        /// </summary>
        public bool IsRunning => _tcpListener != null && _cts != null && !_cts.IsCancellationRequested;


        #region 公开API
        /// <summary>
        /// 启动服务器
        /// </summary>
        /// <param name="ip">监听IP（0.0.0.0表示监听所有网卡）</param>
        /// <param name="port">监听端口</param>
        public async Task StartAsync(string ip = "0.0.0.0", int port = 9000)
        {
            if (IsRunning)
                throw new InvalidOperationException("服务器已启动，无需重复启动");

            foreach (var system in GameFacade_Server.serverSystems)
            {
                system.OnStartServer();
            }

            // 初始化取消令牌和监听器
            _cts = new CancellationTokenSource();
            var ipAddress = IPAddress.Parse(ip);
            _tcpListener = new TcpListener(ipAddress, port);

            try
            {
                // 启动监听
                _tcpListener.Start();
                Console.WriteLine($"服务器已启动，监听 {ip}:{port}");

                // 启动客户端连接监听循环
                _listenTask = StartListenLoop();
                await _listenTask;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("服务器监听已取消");
            }
            catch (Exception ex)
            {
                StopAsync().Wait();
                Console.WriteLine($"服务器启动失败：{ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public async Task StopAsync()
        {
            if (!IsRunning) return;

            // 取消所有任务
            _cts?.Cancel();

            // 停止监听
            _tcpListener?.Stop();

            // 等待监听任务结束
            if (_listenTask != null) await _listenTask.ConfigureAwait(false);

            // 断开所有客户端连接
            foreach (var session in _clientSessions.Values)
            {
                await session.DisconnectAsync().ConfigureAwait(false);
            }
            _clientSessions.Clear();

            // 释放资源
            _cts?.Dispose();
            _tcpListener = null;
            _cts = null;

            foreach (var system in GameFacade_Server.serverSystems)
            {
                system.OnStopServer();
            }

            Console.WriteLine("服务器已停止");
        }

        /// <summary>
        /// 向指定客户端发送消息（入队）
        /// </summary>
        /// <param name="sessionId">客户端会话ID</param>
        /// <param name="protoType">消息类型</param>
        /// <param name="message">消息对象</param>
        public void SendMessageToClient(Guid sessionId, EProtoType protoType, IMessage message)
        {
            if (_clientSessions.TryGetValue(sessionId, out var session) && session.IsConnected)
            {
                session.EnqueueMessage(protoType, message);
            }
            else
            {
                Console.WriteLine($"客户端会话[{sessionId}]不存在或已断开，消息发送失败");
            }
        }

        /// <summary>
        /// 广播消息给所有在线客户端
        /// </summary>
        /// <param name="protoType">消息类型</param>
        /// <param name="message">消息对象</param>
        public void BroadcastMessage(EProtoType protoType, IMessage message)
        {
            foreach (var session in _clientSessions.Values)
            {
                if (session.IsConnected)
                {
                    session.EnqueueMessage(protoType, message);
                }
            }
            Console.WriteLine($"广播消息：{protoType}，在线客户端数：{_clientSessions.Count}");
        }

        public ClientSession GetClientSessionBySessionId(Guid sessionId)
        {
            if (_clientSessions.TryGetValue(sessionId, out var session))
            {
                return session;
            }
            return null;
        }
        #endregion 公开API


        #region 内部逻辑
        /// <summary>
        /// 监听客户端连接循环
        /// </summary>
        private async Task StartListenLoop()
        {
            if (_tcpListener == null || _cts == null) return;

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // 等待客户端连接（异步非阻塞）
                    var tcpClient = await _tcpListener.AcceptTcpClientAsync(_cts.Token)
                        .ConfigureAwait(false);

                    // 创建客户端会话
                    var clientSession = new ClientSession(tcpClient);
                    Console.WriteLine($"客户端[{clientSession.SessionId}]已连接，当前在线数：{_clientSessions.Count + 1}");

                    // 注册会话事件
                    clientSession.OnMessageReceived += OnClientSessionMessageReceived;
                    clientSession.OnDisconnected += OnClientSessionDisconnected;

                    // 添加到会话字典
                    _clientSessions.TryAdd(clientSession.SessionId, clientSession);

                    // 触发客户端连接事件
                    OnClientConnected?.Invoke(clientSession);
                }
                catch (OperationCanceledException)
                {
                    // 监听取消，退出循环
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"监听客户端连接异常：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 处理客户端会话的消息接收
        /// </summary>
        private void OnClientSessionMessageReceived(ClientSession session, IMessage msg)
        {
            // 透传消息到服务器的全局事件
            Console.WriteLine($"[{session.SessionId}] 接收消息：{msg.GetType().Name}/{msg}");
            OnClientMessageReceived?.Invoke(session, msg);
        }

        /// <summary>
        /// 处理客户端会话断开
        /// </summary>
        private void OnClientSessionDisconnected(ClientSession session)
        {
            // 从会话字典移除
            _clientSessions.TryRemove(session.SessionId, out _);
            Console.WriteLine($"客户端[{session.SessionId}]已断开，当前在线数：{_clientSessions.Count}");

            // 触发客户端断开事件
            OnClientDisconnected?.Invoke(session);

            // 释放会话资源
            session.Dispose();
        }
        #endregion 内部逻辑

        protected override void OnDispose()
        {
            base.OnDispose();
            _ = StopAsync();
        }
    }
}
