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
            Console.WriteLine("["+qq+"]SetupAccount 操作已取消");
            IsFinished = true;
            session.SendFriendMessageAsync(qq, new PlainMessage("ℹ操作已取消\n" +
                    "如有疑问，请联系QQ:1250542735")).Wait();
        }

        public override void Main()
        {
            try
            {
                Console.WriteLine("[" + qq + "]SetupAccount 流程开始");
                int studentid = -1;
                string passwd = "";
                session.SendFriendMessageAsync(qq,
                    new PlainMessage("⚠请注意，我只能处理NJIT的体温签到。其它学校的同学无法使用本服务。\n" +
                    "ℹ继续使用即默认您同意我们记录您的学号和密码，以便在必要时为您重新登录。\n" +
                    "在设置过程的任何阶段，发送/abort即可终止设置。(注意大小写！！)")).Wait();
                session.SendFriendMessageAsync(qq, new PlainMessage("ℹ现在，请发送您的学号。")).Wait();
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
                session.SendFriendMessageAsync(qq, new PlainMessage("接下来，您需要提供您<NJIT统一认证系统>的密码。\n" +
                    "⚠只有一次机会，如果登录失败就会被拉黑。请先确认您的密码正确。\n" +
                    "ℹ现在，请发送您的密码。")).Wait();
                passwd = ReadLine();
                LoginHandler login = new LoginHandler();
                bool succeed = login.Login(studentid, passwd, NJITSignHelper.Program.SERVICE);
                if (!succeed)
                {
                    session.SendFriendMessageAsync(qq, new PlainMessage("⚠登录失败，您无法继续使用本服务。\n" +
                        "如有疑问，请联系QQ:1250542735")).Wait();
                    Console.WriteLine("[" + qq + "]SetupAccount-登录失败");
                    IsFinished = true;
                    return;
                }
                Console.WriteLine("[" + qq + "]SetupAccount-登录成功");
                session.SendFriendMessageAsync(qq, new PlainMessage("✔登录成功。\n" +
                        "我们需要您的GPS坐标来帮您上报信息。\n" +
                        "正确的格式是Lat,Lng，且必须使用WGS84标准，例如31.928509,118.887844。请【务必使用英文半角逗号】。\n" +
                        "ℹ现在，请发送您的坐标。")).Wait();
                if (SignQueueHandler.AddAccount(new Struct.User()
                {
                    qq = qq,
                    account = login,
                    location = ReadLocation()
                }))
                {
                    Console.WriteLine("[" + qq + "]SetupAccount 流程完成");
                    session.SendFriendMessageAsync(qq, new PlainMessage("✔您的NJIT账号已经绑定，系统会为您自动签到。\n" +
                        "ℹ我们不保证总能为您成功签到，请自行定期检查您的签到情况。我们不为您使用本服务造成的任何后果负责。")).Wait();
                    session.SendFriendMessageAsync(qq,
                        new PlainMessage("⚠疫情防控，人人有责。如果您出现疑似感染症状或与感染者接触，请及时、主动向校方负责人和辅导员汇报。\n" +
                        "【瞒报疫情信息是违法行为，请尊重自己和他人的健康】")).Wait();
                }
            }
            catch(Exception err)
            {
                Console.WriteLine("[" + qq + "]SetupAccount Exception:"+err.Message+"\n"+err.StackTrace);
                session.SendFriendMessageAsync(qq,
                        new PlainMessage("⚠致命错误："+err.Message+"\n请联系QQ:1250542735\n"+err.StackTrace)).Wait();
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
                    string[] data = ReadLine().Split(',');
                    Location wgs84 = new Location(double.Parse(data[0]), double.Parse(data[1]));
                    wgs84.locName = Location.getLocName(wgs84);
                    session.SendFriendMessageAsync(qq,
                        new PlainMessage("ℹ您的地址是：" + wgs84.locName)).Wait();
                    return wgs84;
                }
                catch (Exception e)
                {
                    Console.WriteLine("[" + qq + "]SetupAccount-ReadLocation-无法获取坐标信息");
                    session.SendFriendMessageAsync(qq,
                        new PlainMessage("⚠无法获取地址信息：坐标无效或系统故障，请重试。\n调试信息:" + e.Message + "\n" +
                        "如有疑问请联系QQ:1250542735")).Wait();
                }
            }
        }
    }
}
