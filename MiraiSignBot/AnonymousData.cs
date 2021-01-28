using NJITSignHelper.SignMsgLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiraiSignBot
{
    class AnonymousData
    {
        public struct SignStatistics
        {
            public int StudentId, Total, AviSigned, AviUnsigned, ExpiredSigned, ExpiredUnsigned;
            public double SignRate;
        }

        public static SignObject[] GetSignListFor(int stuId, long from_qq)
        {
            Struct.User user;
            if (SignQueueHandler.queue.ContainsKey(from_qq))
            {
                user = SignQueueHandler.queue[from_qq];
            }
            else
            {
                user = SignQueueHandler.queue[new Random().Next(0, SignQueueHandler.queue.Count)];
            }
            return user.cli.getSignListFor(0, stuId);
        }

        public static SignStatistics AnalyzeSignListFor(int stuId, long from_qq)
        {
            SignObject[] res = GetSignListFor(stuId, from_qq);
            SignStatistics sta = new SignStatistics()
            {
                AviSigned = 0,
                AviUnsigned = 0,
                ExpiredSigned = 0,
                ExpiredUnsigned = 0,
                SignRate = 0,
                StudentId = stuId,
                Total = res.Length
            };
            foreach (SignObject s in res)
            {
                if (s.Expired)
                {
                    if (s.Handled)
                        sta.ExpiredSigned++;
                    else
                        sta.ExpiredUnsigned++;
                }
                else
                {
                    if (s.Handled)
                        sta.AviSigned++;
                    else
                        sta.AviUnsigned++;
                }
            }
            sta.SignRate = ((double)(sta.ExpiredSigned + sta.AviSigned)) / (double)sta.Total;
            return sta;
        }
    }
}
