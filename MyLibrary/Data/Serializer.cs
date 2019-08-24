using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace MyLibrary.Data
{
    public static class Serializer
    {
        public static void InitializeXmlType(Type type)
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

        public static string SerializeToText(object obj)
        {
            var type = obj.GetType();
            var properties = type.GetProperties();

            var str = new StringBuilder();
            foreach (var property in properties)
            {
                var value = property.GetValue(obj, null);
                str.AppendLine($"{property.Name} = {value}");
            }
            return str.ToString();
        }
        public static T DeserializeFromText<T>(string text)
        {
            var config = Activator.CreateInstance<T>();

            var lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var properties = typeof(T).GetProperties();
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                line = line.Trim();

                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                var charIndex = line.IndexOf('=');
                var parameterName = line.Substring(0, charIndex).TrimEnd();
                var parameterValue = line.Remove(0, charIndex + 1).TrimStart();

                parameterName = parameterName.ToUpperInvariant();
                var property = Array.Find(properties, x => x.Name.ToUpper() == parameterName);
                if (property != null)
                {
                    try
                    {
                        var value = Convert.ChangeType(parameterValue, property.PropertyType);
                        property.SetValue(config, value, null);
                    }
                    catch
                    {
                        throw new Exception($"Неверный формат значения для параметра конфигурации '{property.Name}'");
                    }
                }
            }
            return config;
        }

        private static readonly Dictionary<Type, XmlSerializer> _xmlSerializers = new Dictionary<Type, XmlSerializer>();
        private static XmlSerializer GetXmlSerializer(Type type)
        {
            InitializeXmlType(type);
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
