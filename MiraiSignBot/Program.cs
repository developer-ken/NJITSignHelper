using System;
using Mirai_CSharp.Models;
using Mirai_CSharp;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using MiraiSignBot.Struct;
using Newtonsoft.Json.Linq;

namespace MiraiSignBot
{
    class Program
    {
        public static MiraiHttpSession session;
        private static Dictionary<long, Procedure.Procedure> procedures = new Dictionary<long, Procedure.Procedure>();
        private static MiraiHttpSessionOptions options;
        private static long qq = 2997309496;
        static void Main(string[] args)
        {
            Console.WriteLine("[QQ]配置初始化...");
            session = new MiraiHttpSession();
            if (!File.Exists("mirai.conf"))
            {
                JObject jb = new JObject();
                jb.Add("host", "127.0.0.1");
                jb.Add("port", 1234);
                jb.Add("auth", "passw0rd");
                File.WriteAllText("mirai.conf", jb.ToString());
            }
            string config = File.ReadAllText("mirai.conf");
            JObject conf = JObject.Parse(config);
            options = new MiraiHttpSessionOptions(conf.Value<string>("host"), conf.Value<int>("port"), conf.Value<string>("auth"));
            Console.WriteLine("[QQ]等待Mirai...");
            session.ConnectAsync(options, qq).Wait();
            session.DisconnectedEvt += Session_DisconnectedEvt;
            session.FriendMessageEvt += Session_FriendMessageEvt;
            session.NewFriendApplyEvt += Session_NewFriendApplyEvt;
            Console.WriteLine("[QQ]已连接");
            SignQueueHandler.session = session;
            if (File.Exists("./database.bin"))
            {
                Console.WriteLine("[Queue]加载保存的队列...");
                try
                {
                    SignQueueHandler.Load();
                    Console.WriteLine("[Queue]已加载" + SignQueueHandler.queue.Count + "个用户数据");
                }
                catch (Exception err)
                {
                    Console.WriteLine("[Queue]加载失败：" + err.Message + "\n" + err.StackTrace);
                }
            }
            while (true)
            {
                try
                {
                    DateTime start = DateTime.Now;
                    Console.WriteLine("[Timer] 计时器开始:" + start);
                    SignQueueHandler.__ProceedQueue();
                    SignQueueHandler.Save();
                    Console.WriteLine("[Timer] 计时器结束:" + DateTime.Now + ", 用时" + (DateTime.Now - start).Seconds + "秒");
                    Thread.Sleep(10 * 60 * 1000);
                }
                catch (Exception err)
                {
                    Console.WriteLine("[EXCEPTION] 消息循环内出现意外错误");
                    DumpError(err);
                }
            }
        }

        private static void DumpError(Exception err)
        {
            Console.WriteLine("[EXCEPTION] " + err.Message);
            Console.WriteLine("[EXCEPTION] Stack:" + err.StackTrace);
            if (err.InnerException != null)
            {
                Console.WriteLine("[EXCEPTION] ->Inner Exception");
                DumpError(err.InnerException);
            }
        }
        private static async System.Threading.Tasks.Task<bool> Session_DisconnectedEvt(MiraiHttpSession sender, Exception e)
        {
            while (true)
                try
                {
                    Console.WriteLine("[Connection]连接意外断开：" + e.Message + "\n" +
                        "[Connection]断线重连...");
                    await sender.ConnectAsync(options, qq);
                    break;
                }
                catch (Exception err)
                {
                    Console.WriteLine("[Connection]失败：" + err.Message + "\n" +
                        "[Connection]断线重连...");
                    await Task.Delay(500);
                }
            return false;
        }

        private static async System.Threading.Tasks.Task<bool> Session_NewFriendApplyEvt(MiraiHttpSession sender, INewFriendApplyEventArgs e)
        {
            await session.HandleNewFriendApplyAsync(e, FriendApplyAction.Allow);
            return false;
        }

        private static async System.Threading.Tasks.Task<bool> Session_FriendMessageEvt(MiraiHttpSession sender, IFriendMessageEventArgs e)
        {
            string msg = GetStringMessage(e.Chain);
            bool regNewProcedure = true;
            if (procedures.ContainsKey(e.Sender.Id))
            {
                if (procedures[e.Sender.Id].IsFinished)
                {
                    procedures.Remove(e.Sender.Id);
                }
                else
                {//Procedure仍在继续
                    regNewProcedure = false;
                    Procedure.Procedure.WriteLine(e.Sender.Id, msg);
                }
            }
            if (regNewProcedure)
            {
                switch (msg)
                {
                    case "RESET":
                        {
                            lock (SignQueueHandler.queue)
                            {
                                foreach (User u in SignQueueHandler.queue.Values)
                                {
                                    u.cli.lastUpdate = 0;
                                }
                            }
                        }
                        break;
                    case "自动签到":
                        Procedure.SetupAccount proc = new Procedure.SetupAccount(e.Sender.Id, session);
                        procedures.Add(e.Sender.Id, proc);
                        new Thread(new ThreadStart(proc.Main)).Start();
                        break;
                    case "TD":
                        SignQueueHandler.RemoveAccount(e.Sender.Id);
                        break;
                    case "CHECK":
                    case "检查":
                    case "复核":
                    case "复查":
                        await session.SendFriendMessageAsync(e.Sender.Id,
                        new PlainMessage("好的，我会再次检查您的签到列表。"));
                        SignQueueHandler.ReCheckUser(e.Sender.Id);
                        break;
                    case "谁没签到":
                        try
                        {
                            Console.WriteLine("[" + e.Sender.Id + "] 正在执行签到查询...");
                            await session.SendFriendMessageAsync(e.Sender.Id, new PlainMessage(
                                "稍等，我查一下..."
                                ));
                            List<int> unsignList = new List<int>();
                            string unsignedStr = "";
                            for (int i = 208200601; i <= 208200641; i++)
                            {
                                var data = AnonymousData.AnalyzeSignListFor(i, e.Sender.Id);
                                if (data.AviUnsigned > 0)
                                {
                                    unsignedStr += i.ToString() + ",";
                                    unsignList.Add(i);
                                }
                                Thread.Sleep(new Random().Next(500, 1000));
                            }
                            Console.Write("[" + e.Sender.Id + "] 查好了，");
                            if (unsignList.Count == 0)
                            {

                                Console.WriteLine("所有人都签了");
                                await session.SendFriendMessageAsync(e.Sender.Id, new PlainMessage(
                                    "除过期签到外，所有人都签到了"
                                    ));
                            }
                            else
                            {
                                unsignedStr = unsignedStr[0..^1];
                                Console.WriteLine("这些人没签：" + unsignedStr);
                                await session.SendFriendMessageAsync(e.Sender.Id, new PlainMessage(
                                    "这些人没有签到：\n" + unsignedStr
                                    ));
                            }
                        }
                        catch (Exception err)
                        {
                            await session.SendFriendMessageAsync(e.Sender.Id, new PlainMessage(
                                "额...我这边好像出问题了。如果你需要技术帮助，可以提供下面的信息:\n" +
                                err.Message + "\n" +
                                err.StackTrace
                                ));
                        }
                        break;
                    default:

                        break;
                }
            }
            return false;
        }

        private static string GetStringMessage(IMessageBase[] chain)
        {
            StringBuilder sb = new StringBuilder();
            foreach (IMessageBase msg in chain)
            {
                if (msg.Type == "Plain")
                {
                    PlainMessage plmsg = (PlainMessage)msg;
                    sb.Append(plmsg.Message);
                }
            }
            return sb.ToString();
        }
    }
}
