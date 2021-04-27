using NJITSignHelper.SignMsgLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using static NJITSignHelper.SignMsgLib.SignObject;

namespace NJITSignHelper
{
    public class Program
    {
        private static PhyLocation.Location location;
        public const string SERVICE = "https://njit.campusphere.net/wec-counselor-sign-apps/stu/sign/submitSign";


        static void Main(string[] args)
        {
            LoginHandler han = new LoginHandler();
            Console.WriteLine("====NJIT体温签到自动程序====\n" +
                "请登录您的【统一认证系统】账号(用来登录信息门户和教务系统那个账号)\n\n----NJIT统一认证系统----");
            do
            {
                int number = -1;
                try
                {
                    Console.Write("学号>");
                    number = int.Parse(Console.ReadLine());
                }
                catch
                {
                    Console.WriteLine("请输入正确的学号！");
                }
                Console.Write("密码>");
                string password = Console.ReadLine();
                Console.Write("获取CAS认证...");
                bool succ = han.Login(number, password, SERVICE);
                if (!succ)
                {
                    Console.WriteLine("FAIL\n登录失败，请重试。");
                    continue;
                }
                break;
            } while (true);
            Console.WriteLine("OK.\n登录成功。\n-----------------------\n\n提供您的经纬度和地址。");
            while (true)
            {
                try
                {
                    Console.Write("Lat>");
                    double lat = double.Parse(Console.ReadLine());
                    Console.Write("Lng>");
                    double lng = double.Parse(Console.ReadLine());
                    Console.Write("地址>");
                    string loc = Console.ReadLine();
                    location = new PhyLocation.Location(lat, lng) { locName = loc };
                    break;
                }
                catch
                {
                    Console.WriteLine("Location生成失败，请确认输入格式正确");
                }
            }

            SignMsgLib.Client client = new SignMsgLib.Client(new Client.ClientInfo()
            {
                SystemName = "Android",
                SystemVersion = "10",
                AppVersion = "8.2.17",
                DeviceModel = "Peach X",
                DeviceId = Guid.NewGuid()//生成新的硬件ID
            }, han);
            int last = 0;
            while (true)
            {
                var list = client.getSignList(last);
                /*list = (from n in list
                        where !n.Handled
                        select n
                       ).ToArray();*/
                last = Now();
                Console.WriteLine("[SignList] 检测到" + list.Length + "个未签的签到");
                foreach (var item in list)
                {
                    if (item.Expired)
                    {
                        Console.WriteLine("\t<" + item.signWid + ">已过期，不处理：" + item.DeadLine.ToString());
                        continue;
                    }
                    if (item.Handled)
                    {
                        Console.WriteLine("\t<" + item.signWid + ">已被签过");
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
                                Console.WriteLine("FAIL.\n\tCAS认证失败，无法获取签到信息。");
                                throw new Exception("CAS已失效且无法更新");
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
                        var res = item.Sign(selections.ToArray(), location);
                        if (res.Value<long>("code") == 0)
                        {
                            Console.WriteLine("\t->已签到");
                        }
                        else
                        {
                            Console.WriteLine("\t<" + res.Value<long>("code") + ">无法签到：" + res.Value<string>("message"));
                        }
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine("\t<EXCEPTION>无法签到：" + err.Message);
                    }
                }
                Console.WriteLine("[SignList] 10分钟后再次抓取");
                Thread.Sleep(10 * 60 * 1000);//十分钟执行一次
            }
        }

        public static int Now()
        {
            return (int)(DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
