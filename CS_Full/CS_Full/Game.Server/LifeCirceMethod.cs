using Game.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Server
{
    public static class LifeCirceMethod
    {
        public static void Start()
        {
            Console.WriteLine($"[Start] 程序启动，Server");

            foreach (var system in GameFacade_Server.serverSystems)
            {
                system.Start();
            }
        }

        public static void Update()
        {
            //Console.WriteLine($"[Update]");
            foreach (var system in GameFacade_Server.serverSystems)
            {
                system.Update();
            }
        }

        public static void FixedUpdate()
        {
            //Console.WriteLine($"[FixedUpdate]");
            foreach (var system in GameFacade_Server.serverSystems)
            {
                system.LogicUpdate();
            }
        }
    }
}
