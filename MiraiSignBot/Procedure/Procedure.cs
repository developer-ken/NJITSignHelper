using Mirai_CSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MiraiSignBot.Procedure
{
    abstract class Procedure
    {
        public bool IsFinished { get; protected set; }
        /// <summary>
        /// 消息队列
        /// </summary>
        private static Dictionary<long, List<string>> queue = new Dictionary<long, List<string>>();
        public MiraiHttpSession session { get; private set; }
        public long qq { get; private set; }

        public Procedure(long qq, MiraiHttpSession session)
        {
            this.qq = qq;
            this.session = session;
        }

        public abstract void Main();
        public abstract void Abort();

        public static void WriteLine(long qq, string item)
        {
            if (queue == null) queue = new Dictionary<long, List<string>>();
            if (!queue.ContainsKey(qq)) queue.Add(qq, new List<string>());
            if (queue[qq] == null) queue[qq] = new List<string>();
            queue[qq].Add(item);
        }

        public string ReadLine()
        {
            string line = ReadLine(qq);
            if (line == "/abort")
            {
                IsFinished = true;
                Abort();
                throw new Exception("Procedure aborted by user.");
            }
            return line;
        }

        public static string ReadLine(long qq)
        {
            if (queue == null) queue = new Dictionary<long, List<string>>();
            string line = "";
                try
                {
                    while (!queue.ContainsKey(qq) || queue[qq] == null || queue[qq].Count == 0)
                        Thread.Sleep(0);
                }
                catch { }
                line = queue[qq][0];
                queue[qq].RemoveAt(0);
            return line;
        }
    }
}
