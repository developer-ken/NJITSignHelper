using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace MiraiSignBot.Struct
{
    class ObjectSerilizer
    {
        public static byte[] SerializeToBinary(object obj)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(stream, obj);

            byte[] data = stream.ToArray();
            stream.Close();

            return data;
        }
        public static object DeserializeFromBinary(byte[] data)
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(data, 0, data.Length);
            stream.Position = 0;
            BinaryFormatter bf = new BinaryFormatter();
            object obj = bf.Deserialize(stream);

            stream.Close();

            return obj;
        }
        public static T DeserializeFromBinary<T>(byte[] data)
        {
            return (T)DeserializeFromBinary(data);
        }
    }
}
