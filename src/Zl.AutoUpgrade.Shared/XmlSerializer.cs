using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Zl.AutoUpgrade.Shared
{
    static class XmlSerializer
    {
        public static byte[] ToBinary(object obj)
        {
            using (MemoryStream fs = new MemoryStream())
            {
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(obj.GetType());
                x.Serialize(fs, obj);
                fs.Flush();
                return fs.ToArray();
            }
        }


        public static T ToObject<T>(byte[] data)
        {
            using (MemoryStream sr = new MemoryStream(data))
            {
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(T));
                return (T)(x.Deserialize(sr));
            }
        }

        public static string ToString(object obj)
        {
            StringBuilder sb = new StringBuilder();
            using (StringWriter writer = new StringWriter(sb))
            {
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(obj.GetType());
                x.Serialize(writer, obj);
                writer.Flush();
                return sb.ToString();
            }
        }

        public static T ToObject<T>(string dataString)
        {
            using (StringReader reader = new StringReader(dataString))
            {
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(T));
                return (T)(x.Deserialize(reader));
            }
        }

        public static T Copy<T>(T obj)
        {
            return ToObject<T>(ToBinary(obj));
        }
        /// <summary>
        /// 从文件中读取 对应类型数据
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="serializer">序列化实例</param>
        /// <param name="filePath">文件全路径</param>
        /// <returns>数据</returns>
        public static T LoadFromFile<T>(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(T));
                return (T)(x.Deserialize(stream));
            }
        }

        /// <summary>
        /// 将数据保存到指定的路径下
        /// </summary>
        /// <param name="serializer">序列化实例</param>
        /// <param name="obj">数据</param>
        /// <param name="filePath">文件全路径</param>
        public static void SaveToFile(object obj, string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(obj.GetType());
                x.Serialize(stream, obj);
                stream.Flush();
            }
        }
    }
}
