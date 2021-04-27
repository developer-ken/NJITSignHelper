using Mirai_CSharp.Models;
using NJITSignHelper.PhyLocation;
using NJITSignHelper.SignMsgLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiraiSignBot.Procedure
{
    class SetupAccount : Procedure
    {
        public SetupAccount(long qq, Mirai_CSharp.MiraiHttpSession session) : base(qq, session)
        {

        }

        public override void Abort()
        {
            Console.WriteLine("[" + qq + "]SetupAccount 操作已取消");
            IsFinished = true;
            session.SendFriendMessageAsync(qq, new PlainMessage("ℹ好的，再见。"));
        }

        public override void Main()
        {
            try
            {
                Console.WriteLine("[" + qq + "]SetupAccount 流程开始");
                int studentid = -1;
                string passwd = "";
                session.SendFriendMessageAsync(qq,
                    new PlainMessage("⚠我只能处理NJIT的体温签到，其它学校的同学我帮不上忙。\n" +
                    "ℹ我会记住你的NJIT账密，如果你不同意可以说/abort。\n" +
                    "在设置过程的任何阶段，你都可以发/abort让我停下(注意大小写！！)")).Wait();
                session.SendFriendMessageAsync(qq, new PlainMessage("ℹ请告诉我你的学号")).Wait();
                while (true)
                {
                    studentid = ReadInt();
                    if (studentid.ToString().Length != 9)
                    {
                        session.SendFriendMessageAsync(qq, new PlainMessage("⚠请发送正确的学号")).Wait();
                    }
                    else break;
                }
                Console.WriteLine("[" + qq + "]SetupAccount-学号已获取");
                session.SendFriendMessageAsync(qq, new PlainMessage("我需要你<NJIT统一认证系统>的密码。\n" +
                    "⚠一定要先确认密码是对的，因为只有一次机会。\n" +
                    "ℹ请告诉我你的密码")).Wait();
                passwd = ReadLine();
                LoginHandler login = new LoginHandler();
                bool succeed = login.Login(studentid, passwd, NJITSignHelper.Program.SERVICE);
                if (!succeed)
                {
                    session.SendFriendMessageAsync(qq, new PlainMessage("⚠对不起，我没办法获得你的NJIT账号的CAS授权。密码可能不对，也有可能是我的打开方式不对。")).Wait();
                    Console.WriteLine("[" + qq + "]SetupAccount-登录失败");
                    IsFinished = true;
                    return;
                }
                Console.WriteLine("[" + qq + "]SetupAccount-登录成功");
                session.SendFriendMessageAsync(qq, new PlainMessage("✔很棒，我拿到你的CAS授权了。\n")).Wait();
                Random r = new Random();
                session.SendFriendMessageAsync(qq, new PlainMessage("我要知道你在哪里。\n" +
                        "我需要你的WGS84坐标(Lat,Lng)，比如31." + r.Next(100000, 999999) + ",118." + r.Next(100000, 999999) + "\n南京工程学院坐标约为<31.931,118.876>\n" +
                        "ℹ请发送你的坐标")).Wait();
                if (SignQueueHandler.AddAccount(new Struct.User()
                {
                    qq = qq,
                    account = login,
                    location = ReadLocation()
                }))
                {
                    Console.WriteLine("[" + qq + "]SetupAccount 流程完成");
                    session.SendFriendMessageAsync(qq, new PlainMessage("✔大功告成啦！以后我会帮你留意签到。\n" +
                        "ℹ我不保证总能成功签到，所以你自己也要注意签到情况哦。\n⚠每次签到成功我都会给你发消息，所以千万不要删我好友，不然我就把你拉黑>_<")).Wait();
                    session.SendFriendMessageAsync(qq,
                        new PlainMessage("⚠疫情防控，人人有责。如果您出现疑似感染症状或与感染者接触，请及时、主动向校方负责人和辅导员汇报。\n" +
                        "【瞒报疫情信息是违法行为，请尊重自己和他人的健康】")).Wait();
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("[" + qq + "]SetupAccount Exception:" + err.Message + "\n" + err.StackTrace);
                session.SendFriendMessageAsync(qq,
                        new PlainMessage("⚠致命错误：" + err.Message + "\n请联系QQ:1250542735\n" + err.StackTrace)).Wait();
            }
            IsFinished = true;
        }

        public int ReadInt()
        {
            while (true)
            {
                try
                {
                    int result = int.Parse(ReadLine());
                    return result;
                }
                catch
                {
                    session.SendFriendMessageAsync(qq, new PlainMessage("⚠请发送纯数字")).Wait();
                }
            }
        }

        public Location ReadLocation()
        {
            while (true)
            {
                try
                {
                    string[] data = ReadLine().Replace("，", ",").Split(',');
                    Location wgs84 = new Location(double.Parse(data[0]), double.Parse(data[1]));
                    wgs84.locName = Location.getLocName(wgs84.ToBD09());
                    session.SendFriendMessageAsync(qq,
                        new PlainMessage("ℹ我发现你在" + wgs84.locName)).Wait();
                    return wgs84;
                }
                catch (Exception e)
                {
                    Console.WriteLine("[" + qq + "]SetupAccount-ReadLocation-无法获取坐标信息");
                    session.SendFriendMessageAsync(qq,
                        new PlainMessage("⚠我没法验证你的地址，可能是你的地址错了，或者系统出了问题。请重试。\n调试信息:" + e.Message + "\n" + 
                        e.StackTrace + "\n" +
                        "如有疑问请联系QQ:1250542735")).Wait();
                }
            }
        }
    }
}
