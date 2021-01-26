using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Security.Cryptography;

namespace NJITSignHelper.SignMsgLib
{
    [Serializable]
    public class Client
    {
        public const int SCHOOL_CODE = 11276;
        public const string ENCODE_KEY = "b3L26XNL";
        public const string ACCESS_TOKEN = "5e5b7d74e7b43fc22a851e615fd2792f";
        public const string APPID = "amp-ios-11276";
        public const string UA = "Mozilla/5.0 (Linux; Android 10; Mi 10 Build/QKQ1.191117.002; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/83.0.4103.101 Mobile Safari/537.36  cpdaily/8.2.17 wisedu/8.2.17 okhttp/3.12.4";

        public ClientInfo Info { get; private set; }
        public LoginHandler Login { get; private set; }

        [Serializable]
        public struct ClientInfo
        {
            public string SystemName, SystemVersion;
            public string DeviceModel;
            public Guid DeviceId;
            public string AppVersion;
        }

        public JObject HTTP_POST(string url, JObject payload, string CpdCrypt = "")
        {
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/json";

            req.Headers.Add("accessToken", ACCESS_TOKEN);
            req.Headers.Add("appId", APPID);
            req.Headers.Add("Cpdaily-Extension", CpdCrypt);
            req.CookieContainer = new CookieContainer();
            req.CookieContainer.Add(new Cookie("MOD_AUTH_CAS", Login.MOD_AUTH_CAS, "/", ".njit.campusphere.net"));

            req.UserAgent = UA;

            byte[] data = Encoding.UTF8.GetBytes(payload.ToString());
            req.ContentLength = data.Length;
            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();
            }

            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            if (resp.ContentType.IndexOf("json") >= 0)
            {
                Stream stream = resp.GetResponseStream();
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
                return (JObject)JsonConvert.DeserializeObject(result);
            }
            else return null;
        }

        /// <summary>
        /// 创建客户端
        /// </summary>
        /// <param name="cpe">Cpdaily-Extension的值</param>
        /// <param name="ticket">MOD_AUTH_CAS的值</param>
        public Client(ClientInfo inf, LoginHandler login)
        {
            Login = login;
            Info = inf;
        }

        public int lastUpdate = 0;

        public SignObject[] getSignList()
        {
            var result = getSignList(lastUpdate);
            lastUpdate = Now();
            return result;
        }

        public SignObject[] getSignList(int lastupdate)
        {
            /*
{
    "userId": "208200611",
    "schoolCode": "11276",
    "sign": "95c143160bcb0a3c65e0f2811cd3b2fb",
    "timestamp": "0",
    "page": {
        "start": "0",
        "size": "200",
        "total": ""
    }
}
             */
            List<SignObject> reslist = new List<SignObject>();
            int start = 0;
            int step = 200;
            do
            {
                var payload = new JObject();
                payload.Add("userId", Login.StudentId.ToString());
                payload.Add("schoolCode", SCHOOL_CODE.ToString());
                payload.Add("sign", MD5Encrypt(ACCESS_TOKEN + SCHOOL_CODE.ToString() + Login.StudentId.ToString()));
                payload.Add("timestamp", lastupdate.ToString());

                var page = new JObject();
                page.Add("start", start);
                page.Add("size", step);
                page.Add("total", "");
                payload.Add("page", page);

                var result = HTTP_POST(
                    "http://messageapi.campusphere.net/message_pocket_web/V2/mp/restful/mobile/message/extend/get",
                    payload);
                if (result.Value<int>("status") != 200) throw new Exception(result.Value<string>("msg"));
                try
                {
                    if (result["page"] == null || ((JArray)result["msgsNew"]).Count == 0) break;
                }
                catch
                {
                    break;
                }
                foreach (JObject jb in (JArray)result["msgsNew"])
                {
                    if (!jb.Value<bool>("isHandled"))//仅处理没有签过的到
                        reslist.Add(new SignObject(jb, this));
                }
                if (result["page"].Value<int>("size") < step) break;
            } while (true);
            return reslist.ToArray();
        }

        public static string MD5Encrypt(string password)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] hashedDataBytes;
            hashedDataBytes = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder tmp = new StringBuilder();
            foreach (byte i in hashedDataBytes)
            {
                tmp.Append(i.ToString("x2"));
            }
            return tmp.ToString();
        }
        public static int Now()
        {
            return (int)(DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
