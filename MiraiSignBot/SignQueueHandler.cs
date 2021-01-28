using Mirai_CSharp;
using Mirai_CSharp.Models;
using MiraiSignBot.Struct;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static NJITSignHelper.SignMsgLib.SignObject;

namespace MiraiSignBot
{
    class SignQueueHandler
    {
        public static Dictionary<long, User> queue { get; private set; }
        public static MiraiHttpSession session;

        public static bool AddAccount(User user)
        {
            if (queue == null) queue = new Dictionary<long, User>();
            lock (queue)
            {
                if (queue.ContainsKey(user.qq))
                {
                    session.SendFriendMessageAsync(user.qq,
                        new PlainMessage("⚠一个QQ只能绑定一个学号，您已经有登录过的自动签到任务了。\n" +
                        "如有疑问请联系QQ:1250542735"));
                    return false;
                }
                user.cli = new NJITSignHelper.SignMsgLib.Client(new NJITSignHelper.SignMsgLib.Client.ClientInfo()
                {
                    SystemName = "Android",
                    SystemVersion = "10",
                    AppVersion = "8.2.17",
                    DeviceModel = "Peach X",
                    DeviceId = Guid.NewGuid(),//生成新的硬件ID
                }, user.account)
                {
                    //lastUpdate = Now()
                };
                queue.Add(user.qq, user);
                Save();
                return true;
            }
        }

        public static bool RemoveAccount(long qq)
        {
            if (queue == null) queue = new Dictionary<long, User>();
            lock (queue)
            {
                if (queue.ContainsKey(qq))
                {
                    queue.Remove(qq);
                    session.SendFriendMessageAsync(qq,
                        new PlainMessage("✔我已删除了你的NJIT账号，不会继续为您签到。"));
                    Save();
                    return true;
                }
                session.SendFriendMessageAsync(qq,
                        new PlainMessage("⚠我不知道你的NJIT账号。"));
                return false;
            }
        }

        public static void Save()
        {
            lock (queue)
                File.WriteAllBytes("./database.bin", ObjectSerilizer.SerializeToBinary(queue));
        }

        public static void Load()
        {
            if (queue == null) queue = new Dictionary<long, User>();
            lock (queue)
                queue = ObjectSerilizer.DeserializeFromBinary<Dictionary<long, User>>(File.ReadAllBytes("./database.bin"));
        }

        public static void __ProceedQueue()
        {
            if (queue == null) queue = new Dictionary<long, User>();
            lock (queue)
                foreach (User u in queue.Values)
                {
                    CheckUser(u);
                }
        }

        public static void ReCheckUser(long qq)
        {
            if (queue.ContainsKey(qq))
            {
                Console.WriteLine("[" + qq + "] 用户(" + queue[qq].account.StudentId + ")主动触发签到检查");
                queue[qq].cli.lastUpdate = 0;//刷新全部
                CheckUser(queue[qq], true);
            }
            else
            {
                Console.WriteLine("[" + qq + "] 用户主动触发签到检查，但没有绑定的StudentId可供查询。");
                session.SendFriendMessageAsync(qq,
                        new PlainMessage("⚠我不知道你的NJIT账号，所以没法给你签到。\n" +
                        "如果你想告诉我你的NJIT账号，请说“自动签到”"));
            }
        }

        public static void CheckUser(User u, bool noticeAnyway = false)
        {
            try
            {
                var client = u.cli;
                var han = u.account;
                var location = u.location;
                var list = client.getSignList();
                Console.WriteLine("[" + u.qq + "] 检测到" + list.Length + "个签到");
                int expires = 0;
                int handles = 0;
                foreach (var item in list)
                {
                    if (item.Handled)
                    {
                        handles++;
                        continue;
                    }
                    if (item.Expired)
                    {
                        expires++;
                        Console.WriteLine("\t<" + item.signWid + ">已过期，不处理：" + item.DeadLine.ToString());
                        continue;
                    }
                    Console.WriteLine("\t<" + item.signWid + ">开始处理");
                    try
                    {
                        try
                        {
                            item.FetchMore();//获取详细信息
                        }
                        catch (Exception err)
                        {
                            if (Regex.IsMatch(err.Message, ".*认证失败.*"))
                            {
                                Console.Write("\tCAS失效，试图更新...");
                                if (han.ReLogin())
                                {
                                    Console.WriteLine("OK.");
                                    item.FetchMore();
                                }
                                else
                                {
                                    Console.WriteLine("FAIL.\n\tCAS认证失败，无法获取签到信息。");
                                    throw new Exception("CAS已失效且无法更新");
                                }
                            }
                        }
                        List<FormSelection> selections = new List<FormSelection>();
                        foreach (var quest in item.form)
                        {
                            Console.WriteLine("\t\t题<" + quest.wid + ">：" + quest.title);
                            foreach (var sel in quest.selections)
                            {
                                if (!sel.abNormal)
                                {
                                    Console.WriteLine("\t\t\t选<" + sel.wid + ">" + sel.content);
                                    selections.Add(sel);
                                    break;//找到一个正常选项然后停止寻找
                                }
                            }
                        }
                        string selectionsstr = "";
                        int iii = 0;
                        foreach (FormSelection sel in selections)
                        {
                            iii++;
                            selectionsstr += "[" + iii + "]" + sel.content + "\n";
                        }
                        string sendMsg = item.Title + "\n" + selectionsstr;
                        try
                        {
                            session.SendFriendMessageAsync(u.qq,
                                new PlainMessage(sendMsg)).Wait();
                        }
                        catch (Exception err)
                        {
                            Console.WriteLine("\t<EXCEPTION>发送消息失败：" + err.Message);
                        }
                        var res = item.Sign(selections.ToArray(), location);
                        if (res.Value<long>("code") == 0)
                        {
                            Console.WriteLine("\t->已签到");
                            session.SendFriendMessageAsync(u.qq,
                                new PlainMessage("✔已签到\n回复TD取消自动签到服务"));
                        }
                        else
                        {
                            Console.WriteLine("\t<" + res.Value<long>("code") + ">无法签到：" + res.Value<string>("message"));
                            session.SendFriendMessageAsync(u.qq,
                                new PlainMessage("❌无法签到\n" + res.Value<string>("message") + "\n回复TD取消自动签到服务"));
                        }
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("\t<EXCEPTION>无法签到：" + err.Message);
                        session.SendFriendMessageAsync(u.qq,
                                new PlainMessage("❌无法签到\n" + err.Message + "\n回复TD取消自动签到服务\n" + err.StackTrace));
                    }
                }
                if (expires + handles >= list.Length - 1 && noticeAnyway)
                {
                    Console.WriteLine("[" + u.qq + "]" + expires + "个签到均已过期");
                    session.SendFriendMessageAsync(u.qq,
                    new PlainMessage("⚠您所有未签的签到(" + expires + ")均已过期"));
                }
            }
            catch (Exception err)
            {
                session.SendFriendMessageAsync(u.qq,
                    new PlainMessage("处理您的签到时发生意外的错误，请联系QQ:1250542735\n" +
                    "回复TD取消自动签到服务。\n\n" +
                    "错误信息：" + err.Message + "\n" +
                    "" + err.StackTrace));
            }
            Thread.Sleep(2 * 1000);//等待两秒
        }

        public static int Now()
        {
            return (int)(DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
