using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static NJITSignHelper.SignMsgLib.SignObject;

namespace NJITSignHelper
{
    class Program
    {
        static int studentid;
        static string token;
        static string cpe;
        private static PhyLocation.Location location;


        static void Main(string[] args)
        {
            try
            {
                string[] authInfos = File.ReadAllText("account.private").Replace("\r", "").Split('\n');
                studentid = int.Parse(authInfos[0]);
                token = authInfos[1];
                cpe = authInfos[2];
                location = new PhyLocation.Location(double.Parse(authInfos[3].Split(',')[0]),
                    double.Parse(authInfos[3].Split(',')[1]))//小前大后
                { locName = authInfos[3].Split(',')[2] };//地址描述文本
            }
            catch
            {
                Console.WriteLine("无法加载用户认证信息，无法继续执行。");
                while (true) Thread.Sleep(int.MaxValue);
            }
            SignMsgLib.Client client = new SignMsgLib.Client(cpe, token, studentid);
            int last = 0;
            while (true)
            {
                var list = client.getSignList(last);
                last = Now();
                Console.WriteLine("[SignList] 检测到" + list.Length + "个未签的签到");
                foreach (var item in list)
                {
                    if (item.expired)
                    {
                        Console.WriteLine("\t<" + item.signWid + ">已过期，不处理：" + item.deadLine.ToString());
                        continue;
                    }
                    Console.WriteLine("\t<" + item.signWid + ">开始处理");
                    try
                    {
                        item.FetchMore();//获取详细信息
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
