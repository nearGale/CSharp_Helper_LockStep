using Game.Shared;
using Game.Shared.Proto;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Client
{
    class Program
    {
        static async Task Main()
        {
            GameFacade_Common.startTime = DateTime.Now;

            GameFacade_Client.PlayerName = "Client" + new Random().Next(1000, 9999); // 随机玩家名

            // 获取单例实例
            var networkClient = NetworkClient.Instance;

            // 订阅消息接收事件（业务层处理消息）
            networkClient.OnMessageReceived += GameHelper_Client.OnReceiveProtoMsg;

            // 订阅连接状态事件
            networkClient.OnConnectionStateChanged += (isConnected) =>
            {
                Console.WriteLine(isConnected ? "已连接" : "已断开");
                foreach (var system in GameFacade_Client.clientSystems)
                {
                    if(isConnected)
                        system.OnClientConnect();
                    else
                        system.OnClientDisconnect();
                }
            };


            // 连接服务器并处理消息的主逻辑
            try
            {
                // 1. 连接服务器
                _ = networkClient.ConnectAsync("127.0.0.1", 9000);

                //// 2. 示例：发送登录消息（入队）
                //var loginMsg = new C2SLogin
                //{
                //    Username = "player123",
                //    Password = "123456" // 实际项目需加密
                //};
                //networkClient.EnqueueMessage(EProtoType.ProtoC2SLogin, loginMsg);

                //// 3. 手动输入发送聊天消息
                //Console.WriteLine("输入 'chat:内容' 发送聊天，输入 'quit' 退出");
                //while (networkClient.IsConnected)
                //{
                //    var input = Console.ReadLine();
                //    if (string.IsNullOrEmpty(input)) continue;

                //    if (input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                //    {
                //        await networkClient.DisconnectAsync();
                //        break;
                //    }

                //    if (input.StartsWith("chat:", StringComparison.OrdinalIgnoreCase))
                //    {
                //        var content = input.Substring(5).Trim();
                //        var chatMsg = new C2SChat
                //        {
                //            From = "player123",
                //            Text = content
                //        };
                //        // 聊天消息入队，由发送循环处理
                //        networkClient.EnqueueMessage(EProtoType.ProtoC2Schat, chatMsg);
                //        Console.WriteLine($"聊天消息已入队：{content}");
                //    }
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"程序异常：{ex.Message}");
            }
            //finally
            //{
            //    // 释放单例资源
            //    networkClient.Dispose();
            //}

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