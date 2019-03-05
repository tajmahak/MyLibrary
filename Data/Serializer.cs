using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace MyLibrary.Data
{
    public static class Serializer
    {
        static Serializer()
        {
            _xmlSerializers = new Dictionary<Type, XmlSerializer>();
        }

        public static void InitializeType(Type type)
        {
            lock (_xmlSerializers)
                if (!_xmlSerializers.ContainsKey(type))
                {
                    var xmlSerializer = new XmlSerializer(type);
                    _xmlSerializers.Add(type, xmlSerializer);
                }
        }
        public static string Serialize(object obj)
        {
            var xmlSerializer = GetXmlSerializer(obj.GetType());

            var mem = new MemoryStream();
            xmlSerializer.Serialize(mem, obj);
            var data = mem.ToArray();
            return Convert.ToBase64String(data);
        }
        public static T Deserialize<T>(string data)
        {
            if (data == null)
                return Activator.CreateInstance<T>();

            var xmlSerializer = GetXmlSerializer(typeof(T));

            var mem = new MemoryStream(Convert.FromBase64String(data));
            var obj = xmlSerializer.Deserialize(mem);
            mem.Dispose();
            return (T)obj;
        }

        private static Dictionary<Type, XmlSerializer> _xmlSerializers;
        private static XmlSerializer GetXmlSerializer(Type type)
        {
            InitializeType(type);
            lock (_xmlSerializers)
                return _xmlSerializers[type];
        }
    }
}
