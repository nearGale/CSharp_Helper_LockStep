using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    public static class GameFacade_Common
    {
        public static bool isServer = false;

        public static DateTime startTime;

        /// <summary>
        /// 是否启用指令执行快照（记录指令执行前、后，整个战斗场景的状态）
        /// 记录到的文件路径：PersistentDataPath/commandSnapshotLogName
        /// </summary>
        public static bool enableCommandSnapshot = true;

        /// <summary>
        /// 指令快照文件，存储路径
        /// 基于 PersistentDataPath
        /// </summary>
        public static string commandSnapshotLogName = "commandSnapshot";

        /// <summary>
        /// 报错内容文件，存储路径
        /// </summary>
        public static string exceptionLogName = "exception";

        /// <summary>
        /// log文件，存储路径
        /// </summary>
        public static string normalLogName = "normal";

        /// <summary>
        /// 生命周期退出监测
        /// </summary>
        public static bool lifeCircleStopTrigger = false;
    }
}
