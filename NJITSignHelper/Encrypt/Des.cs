using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NJITSignHelper.Encrypt
{
    class Des
    {
        public Des()
        {
        }

        public static string Encrypt(string stringToEncrypt, string sKey)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            byte[] inputByteArray = Encoding.UTF8.GetBytes(stringToEncrypt);
            des.Key = ASCIIEncoding.UTF8.GetBytes(sKey);
            des.Mode = CipherMode.CBC;
            //des.Padding = PaddingMode.None;
            des.Padding = PaddingMode.PKCS7;
            des.IV = Encoding.UTF8.GetBytes("\x01\x02\x03\x04\x05\x06\x07\x08");
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            return Base64.Encrypt(ms.ToArray());
        }


        public static string Decrypt(string stringToDecrypt, string sKey)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            byte[] inputByteArray = Base64.Decrypt(stringToDecrypt);
            des.Key = Encoding.UTF8.GetBytes(sKey);
            des.Mode = CipherMode.CBC;
            //des.Padding = PaddingMode.None;
            des.Padding = PaddingMode.PKCS7;
            des.IV = Encoding.UTF8.GetBytes("\x01\x02\x03\x04\x05\x06\x07\x08");
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            byte[] bytearr = ms.ToArray();
            return Encoding.UTF8.GetString(bytearr);
        }
    }
}
