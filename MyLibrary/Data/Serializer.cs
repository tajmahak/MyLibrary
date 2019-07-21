using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace MyLibrary.Data
{
    public static class Serializer
    {
        public static void InitializeType(Type type)
        {
            lock (_xmlSerializers)
            {
                if (!_xmlSerializers.ContainsKey(type))
                {
                    var xmlSerializer = new XmlSerializer(type);
                    _xmlSerializers.Add(type, xmlSerializer);
                }
            }
        }
        public static string SerializeToXml(object obj)
        {
            var xmlSerializer = GetXmlSerializer(obj.GetType());
            using (var textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, obj);
                return textWriter.ToString();
            }
        }
        public static T DeserializeFromXml<T>(string xml)
        {
            var xmlSerializer = new XmlSerializer(typeof(T));
            using (var textReader = new StringReader(xml))
            {
                var obj = (T)xmlSerializer.Deserialize(textReader);
                CorrectObject(obj);
                return obj;
            }
        }

        private static readonly Dictionary<Type, XmlSerializer> _xmlSerializers = new Dictionary<Type, XmlSerializer>();
        private static XmlSerializer GetXmlSerializer(Type type)
        {
            InitializeType(type);
            lock (_xmlSerializers)
            {
                return _xmlSerializers[type];
            }
        }
        private static void CorrectObject(object obj)
        {
            //  исправление многострочного string после десериализации
            foreach (var property in obj.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    var value = (string)property.GetValue(obj, null);
                    if (value != null)
                    {
                        value = value.Replace("\n", "\r\n");
                        property.SetValue(obj, value, null);
                    }
                }
            }
        }
    }
}
