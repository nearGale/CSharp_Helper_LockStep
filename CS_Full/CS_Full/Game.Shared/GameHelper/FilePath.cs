using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    public static partial class GameHelper_Common
    {
        /// <summary>
        /// 获取工程/程序运行的根目录
        /// </summary>
        /// <returns>工程根目录的绝对路径</returns>
        public static string GetProjectRootDirectory()
        {
            // 获取当前程序集的所在目录
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            // 获取程序集所在文件夹（即工程输出目录，如 bin/Debug/net6.0）
            string assemblyDir = Path.GetDirectoryName(assemblyPath);

            // 如果是开发环境，可向上回溯到工程根目录（跳过 bin/Debug/net6.0 层级）
            // 注意：发布后需注释此逻辑，或根据实际情况调整回溯层级
            string projectRoot = Directory.GetParent(assemblyDir).Parent.Parent.FullName;

            return projectRoot; // 最终返回工程根目录
        }
    }
}
