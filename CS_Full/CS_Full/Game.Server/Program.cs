using Game.Shared;
using Game.Shared.Proto;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Game.Server
{
    class Program
    {
        static readonly ConcurrentDictionary<string, ClientSession> Sessions = new ConcurrentDictionary<string, ClientSession>();

        static async Task Main()
        {
            GameFacade_Common.startTime = DateTime.Now;

            // 获取服务器单例
            var server = NetworkServer.Instance;

            // 订阅客户端连接事件
            server.OnClientConnected += (session) =>
            {
                Console.WriteLine($"业务层：客户端[{session.SessionId}]连接");

                Msg_PlayerConnect_Rsp msg = new();
                server.SendMessageToClient(session.SessionId, EProtoType.PlayerConnectRsp, msg);
            };

            // 订阅客户端断开事件
            server.OnClientDisconnected += (session) =>
            {
                Console.WriteLine($"业务层：客户端[{session.SessionId}]断开");
            };

            // 订阅客户端消息接收事件（处理具体业务）
            server.OnClientMessageReceived += async (session, msg) =>
            {
                //Console.WriteLine($"业务层：收到客户端[{session.SessionId}]消息：{msg.GetType().Name}");
                ServerMessageSystem.Instance.HandleMessage(session, msg);
            };


            try
            {
                // 启动服务器（监听9000端口）
                 _ = server.StartAsync("0.0.0.0", 9000);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"服务器启动异常：{ex.Message}");
            }

            // 定义各生命周期的逻辑
            LifeCycleSimulator.Instance.Prepare(
                LifeCirceMethod.Start, LifeCirceMethod.Update, LifeCirceMethod.FixedUpdate);

            while (true)
            {
                LifeCycleSimulator.Instance.Tick();
            }
        }
    }
}