using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;

namespace MiraiSignBot.Struct
{
    class ObjectSerilizer
    {
        public static byte[] SerializeToJson(object obj)
        {
            //MemoryStream stream = new MemoryStream();
            // bf.Serialize(stream, obj);
            byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(obj, typeof(Dictionary<long, User>)));
            //byte[] data = stream.ToArray();
            //stream.Close();

            return data;
        }
        public static object DeserializeFromJson(byte[] data)
        {
            //MemoryStream stream = new MemoryStream();
            //stream.Write(data, 0, data.Length);
            //stream.Position = 0;
            //BinaryFormatter bf = new BinaryFormatter();
            //object obj = bf.Deserialize(stream);

            //stream.Close();
            return null;
        }
        public static T DeserializeFromBinary<T>(byte[] data)
        {
            //return (T)DeserializeFromJson(data);
            string json = Encoding.UTF8.GetString(data);
            return (T)JsonSerializer.Deserialize(json, typeof(T));
        }
    }
}
