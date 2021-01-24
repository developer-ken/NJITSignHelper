using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NJITSignHelper.Encrypt
{
    class Aes
    {
        public static string Encrypt(string str, string key,string iv=null)
        {
            if (string.IsNullOrEmpty(str)) return null;
            byte[] toEncryptArray = Encoding.UTF8.GetBytes(str);

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
            if (iv != null) rm.IV = Encoding.UTF8.GetBytes(iv);

            ICryptoTransform cTransform = rm.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Base64.Encrypt(resultArray);
        }

        public static string Decrypt(string str, string key, string iv = null)
        {
            if (string.IsNullOrEmpty(str)) return null;
            byte[] toEncryptArray = Base64.Decrypt(str);

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7
            };
            if (iv != null) rm.IV = Encoding.UTF8.GetBytes(iv);

            ICryptoTransform cTransform = rm.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Encoding.UTF8.GetString(resultArray);
        }
    }
}
