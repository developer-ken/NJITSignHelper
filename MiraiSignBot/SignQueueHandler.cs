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
                },user.account)
                {
                    lastUpdate = Now()
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
                        new PlainMessage("✔已移除您绑定的NJIT账号，系统将不会继续为您签到。\n" +
                        "如有疑问请联系QQ:1250542735"));
                    Save();
                    return true;
                }
                session.SendFriendMessageAsync(qq,
                        new PlainMessage("⚠您没有绑定过任何学号。\n" +
                        "如有疑问请联系QQ:1250542735"));
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
                    try
                    {
                        var client = u.cli;
                        var han = u.account;
                        var location = u.location;

                        var list = client.getSignList();
                        Console.WriteLine("[" + u.qq + "] 检测到" + list.Length + "个未签的签到");
                        foreach (var item in list)
                        {
                            if (item.expired)
                            {
                                Console.WriteLine("\t<" + item.signWid + ">已过期，不处理：" + item.deadLine.ToString());
                                //continue;
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
                                session.SendFriendMessageAsync(u.qq,
                                        new PlainMessage(sendMsg)).Wait();
                                var res = item.Sign(selections.ToArray(), location);
                                if (res.Value<long>("code") == 0)
                                {
                                    Console.WriteLine("\t->已签到");
                                    session.SendFriendMessageAsync(u.qq,
                                        new PlainMessage("✔已签到\n回复TD取消自动签到服务")).Wait();

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
                            }
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
        }

        public static int Now()
        {
            return (int)(DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
