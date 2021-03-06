﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace NJITSignHelper.SignMsgLib
{
    [Serializable]
    public class LoginHandler
    {
        public CookieCollection cookies { get; private set; }
        //public string MOD_AUTH_CAS { get; private set; }
        private Dictionary<string, string> _secondaryCasCache;
        public int StudentId { get; private set; }
        private string defaultService;
        private string Passwd;
        private string EncodeKey;
        public struct WebResult
        {
            public string Payload;
            public string ContentType;
            public HttpStatusCode StatusCode;
            public string Location;
        }

        public LoginHandler()
        {
            cookies = new CookieCollection();
            _secondaryCasCache = new Dictionary<string, string>();
        }

        public WebResult HTTP_POST(string url, Dictionary<string, string> payload)
        {
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.AllowAutoRedirect = false;
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.85 Safari/537.36";
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.CookieContainer = new CookieContainer();
            if (cookies != null)
            {
                req.CookieContainer = new CookieContainer();
                foreach (Cookie cookieItem in cookies)
                {
                    req.CookieContainer.Add(cookieItem);
                }
            }
            string payloadstr = "";
            foreach (KeyValuePair<string, string> kvp in payload)
            {
                payloadstr += HttpUtility.UrlEncode(kvp.Key) + "=" + HttpUtility.UrlEncode(kvp.Value) + "&";
            }
            payloadstr = payloadstr[0..^1];//Substring(0, payloadstr.Length - 1)

            byte[] data = Encoding.UTF8.GetBytes(payloadstr);
            req.ContentLength = data.Length;
            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(data, 0, data.Length);
                reqStream.Close();
            }
            try
            {
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                cookies.Add(resp.Cookies);
                Stream stream = resp.GetResponseStream();
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
                return new WebResult()
                {
                    Payload = result,
                    ContentType = resp.ContentType,
                    StatusCode = resp.StatusCode,
                    Location = new List<string>(resp.Headers.AllKeys).Contains("Location") ? resp.Headers["Location"] : ""
                };
            }
            catch (WebException exp)
            {
                var resp = (HttpWebResponse)exp.Response;
                cookies.Add(resp.Cookies);
                return new WebResult()
                {
                    Payload = result,
                    ContentType = resp.ContentType,
                    StatusCode = resp.StatusCode,
                    Location = new List<string>(resp.Headers.AllKeys).Contains("Location") ? resp.Headers["Location"] : ""
                };
            }
        }

        public WebResult HTTP_GET(string url)
        {
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.AllowAutoRedirect = false;
            req.Method = "GET";
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.85 Safari/537.36";
            try
            {
                if (cookies != null)
                {
                    req.CookieContainer = new CookieContainer();
                    foreach (Cookie cookieItem in cookies)
                    {
                        req.CookieContainer.Add(cookieItem);
                    }
                }
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                Stream stream = resp.GetResponseStream();
                cookies.Add(resp.Cookies);
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    result = reader.ReadToEnd();
                }
                return new WebResult()
                {
                    Payload = result,
                    ContentType = resp.ContentType,
                    StatusCode = resp.StatusCode,
                    Location = new List<string>(resp.Headers.AllKeys).Contains("Location") ? resp.Headers["Location"] : ""
                };
            }
            catch (WebException exp)
            {
                var resp = (HttpWebResponse)exp.Response;
                
                
                cookies.Add(resp.Cookies);
                return new WebResult()
                {
                    Payload = result,
                    ContentType = resp.ContentType,
                    StatusCode = resp.StatusCode,
                    Location = new List<string>(resp.Headers.AllKeys).Contains("Location") ? resp.Headers["Location"] : ""
                };
            }
        }

        private Dictionary<string, string> loginForm = new Dictionary<string, string>();

        public WebResult GetLoginForm(string serviceurl)
        {
            var webpage = HTTP_GET(
                "http://authserver.njit.edu.cn/authserver/login?service=" + HttpUtility.UrlEncode(serviceurl)
                );
            Dictionary<string, string> formcontents = new Dictionary<string, string>();
            EncodeKey = Regex.Match(webpage.Payload, "input *.*id=\"pwdDefaultEncryptSalt\".*value=\"(.*)\"").Groups[1].Value;
            var match = Regex.Match(webpage.Payload, "<input *.*name=\"(\\w*)\".*value=\"(.*)\"");
            do
            {
                formcontents.Add(match.Groups[1].Value, match.Groups[2].Value);
                match = match.NextMatch();
            } while (match.Success);
            loginForm = formcontents;
            return webpage;
        }

        public string EncodePassword(string password)
        {
            return Encrypt.Aes.Encrypt(Encrypt.Random.String(64) + password, EncodeKey, Encrypt.Random.String(16));
        }

        public string MOD_AUTH_CAS(string serviceUrl,bool nocache = false)
        {
            if (_secondaryCasCache == null) _secondaryCasCache = new Dictionary<string, string>();//兼容已序列化的数据
            if ((!nocache) && _secondaryCasCache.ContainsKey(serviceUrl))
            {
                return _secondaryCasCache[serviceUrl];
            }
            else if (ReLogin(serviceUrl))
            {
                return _secondaryCasCache[serviceUrl];
            }
            else
            {
                return "";
            }
        }

        public string MOD_AUTH_CAS(bool nocache = false)
        {
            return MOD_AUTH_CAS(defaultService, nocache);
        }

        /// <summary>
        /// 尝试使用已有Cookie直接登录，成功则返回True。
        /// </summary>
        /// <param name="serviceurl">要登录的服务的回调。必须正确，否则产生的MOD_AUTH_CAS无效</param>
        /// <returns>代表是否成功的bool值</returns>
        public bool Login(string serviceurl)
        {
            if (defaultService == null) defaultService = serviceurl;
            var getForm = GetLoginForm(serviceurl);

            Match ticket = Regex.Match(getForm.Location, "\\?ticket=([\\w-]{1,})");
            if (ticket.Success)
            {
                _RegCas(serviceurl, ticket.Groups[1].Value);
                ActivateCas(serviceurl, ticket.Groups[1].Value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 使用用户名密码登录，成功则返回True。仍然会先尝试Cookie登录，失败则发送密码。
        /// </summary>
        /// <param name="studentid">学号</param>
        /// <param name="passwd">密码</param>
        /// <param name="serviceurl">要登录的服务的回调。必须正确，否则产生的MOD_AUTH_CAS无效</param>
        /// <returns>代表是否成功的bool值</returns>
        public bool Login(int studentid, string passwd, string serviceurl)
        {
            if (defaultService == null) defaultService = serviceurl;
            StudentId = studentid;
            Passwd = passwd;
            if (Login(serviceurl)) return true;
            loginForm["username"] = studentid.ToString();
            loginForm["password"] = EncodePassword(passwd);
            var loginResult = HTTP_POST(
                "http://authserver.njit.edu.cn/authserver/login?service=" + HttpUtility.UrlEncode(serviceurl),
                loginForm
                );
            Match ticket = Regex.Match(loginResult.Location, "\\?ticket=([\\w-]{1,})");
            if (ticket.Success)
            {
                _RegCas(serviceurl, ticket.Groups[1].Value);
                ActivateCas(serviceurl, ticket.Groups[1].Value);
                return true;
            }
            return false;
        }

        public HttpStatusCode ActivateCas(string service, string mod_auth_cas)
        {
            return HTTP_GET(service + "?ticket=" + mod_auth_cas).StatusCode;
        }

        private void _RegCas(string service, string cas)
        {
            if (_secondaryCasCache.ContainsKey(service))
                _secondaryCasCache[service] = cas;
            else
                _secondaryCasCache.Add(service, cas);
        }

        /// <summary>
        /// 使用上一次登录的信息尝试重新登录
        /// </summary>
        /// <returns>代表是否成功的bool值</returns>
        public bool ReLogin(string serviceUrl)
        {
            return Login(StudentId, Passwd, serviceUrl);
        }
    }
}
