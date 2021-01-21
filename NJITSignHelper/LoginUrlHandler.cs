using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NJITSignHelper
{
    class LoginUrlHandler
    {
        /// <summary>
        /// 获取MOD_AUTH_CAS的正确值
        /// </summary>
        /// <param name="url">标准URL</param>
        /// <returns></returns>
        public static string GetTicket(string url)
        {
            Match match = Regex.Match(url, "\\?ticket=([\\w-]{1,})");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
