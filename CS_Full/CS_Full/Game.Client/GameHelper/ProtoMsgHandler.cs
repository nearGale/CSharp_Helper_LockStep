using Game.Shared;
using Game.Shared.Proto;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Client
{
    public static partial class GameHelper_Client
    {
        public static void OnReceiveProtoMsg(IMessage msg)
        {
            //GameHelper_Common.LogDebug($"OnReceiveProtoMsg:{msg}/{msg.GetType()}");
            switch (msg)
            {
                case C2SChat c:
                    Console.WriteLine($"[Server] {c.From}: {c.Text}");
                    break;
                case S2CPong p:
                    Console.WriteLine($"Pong received at {p.TimeStamp:o}");
                    break;
                default:
                    ClientMessageSystem.Instance.HandleMessage(msg);
                    break;
            }
        }
    }
}
