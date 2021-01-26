using System;
using Mirai_CSharp.Models;
using Mirai_CSharp;
using System.Threading;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace MiraiSignBot
{
    class Program
    {
        public static MiraiHttpSession session;
        private static Dictionary<long, Procedure.Procedure> procedures = new Dictionary<long, Procedure.Procedure>();
        private static MiraiHttpSessionOptions options = new MiraiHttpSessionOptions("192.168.1.204", 1234, "Ken1250542735");
        private static long qq = 2997309496;
        static void Main(string[] args)
        {
            Console.WriteLine("[QQ]初始化...");
            session = new MiraiHttpSession();
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
                SignQueueHandler.__ProceedQueue();
                SignQueueHandler.Save();
                Thread.Sleep(10 * 60 * 1000);
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
                catch(Exception err){
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
                    case "自动签到":
                        Procedure.SetupAccount proc = new Procedure.SetupAccount(e.Sender.Id, session);
                        procedures.Add(e.Sender.Id, proc);
                        new Thread(new ThreadStart(proc.Main)).Start();
                        break;
                    case "TD":
                        SignQueueHandler.RemoveAccount(e.Sender.Id);
                        break;
                    default:
                        session.SendFriendMessageAsync(e.Sender.Id, new PlainMessage("对不起，我听不懂。有疑问请联系QQ:1250542735"));
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
