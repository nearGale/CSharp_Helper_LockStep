using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    public interface ISystem
    {
        void Start();
        void Update();
        void LogicUpdate();
    }

    public interface IClientSystem : ISystem
    {
        void OnClientConnect();
        void OnClientDisconnect();
    }

    public interface IServerSystem : ISystem
    {
        void OnStartServer();
        void OnStopServer();
    }
}
