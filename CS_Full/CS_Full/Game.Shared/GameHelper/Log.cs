using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Shared
{
    public static partial class GameHelper_Common
    {
        public static void LogError(string str)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERR:" + str);
            Console.ForegroundColor = originalColor;
            FileLog(GameFacade_Common.exceptionLogName, str);
            throw new Exception(str);
        }

        public static void LogDebug(string str)
        {
            Console.WriteLine("Debug:" + str);
            FileLog(GameFacade_Common.normalLogName, str);
        }

        // TODO: 避免频繁写入，文件频繁IO影响程序性能，缓存Append日志数据，根据数量策略定时存储
        public static void FileLog(string filePath, string str)
        {
            var path = GetProjectRootDirectory() + "//" + $"{filePath}-{GameFacade_Common.startTime.ToString("yyyy-MM-dd-hh-mm-ss")}.log";

            StreamWriter sw;
            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                //CreateText方法限定了文本文件，只能创建文本文件
                //Create应该可以创建任何类型的文件
                sw = fileInfo.CreateText();
            }
            else
            {
                sw = fileInfo.AppendText();
            }

            // 写入一行
            sw.WriteLine(str);

            // 关闭流
            sw.Close();

            // 销毁
            sw.Dispose();

        }
    }
}
