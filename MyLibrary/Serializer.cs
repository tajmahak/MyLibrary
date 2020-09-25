using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MyLibrary
{
    public static class Serializer
    {
        private static readonly Dictionary<Type, XmlSerializer> xmlSerializers = new Dictionary<Type, XmlSerializer>();


        public static string SerializeToXml(object obj)
        {
            XmlSerializer xmlSerializer = GetXmlSerializer(obj.GetType());
            using (StringWriter textWriter = new StringWriter())
            {
                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
                xmlWriterSettings.OmitXmlDeclaration = true;
                xmlWriterSettings.Indent = true;
                using (XmlWriter xmlWriter = XmlWriter.Create(textWriter, xmlWriterSettings))
                {
                    xmlSerializer.Serialize(xmlWriter, obj);
                }
                return textWriter.ToString();
            }
        }

        public static T DeserializeFromXml<T>(string xml)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringReader textReader = new StringReader(xml))
            {
                T obj = (T)xmlSerializer.Deserialize(textReader);
                CorrectObject(obj);
                return obj;
            }
        }

        public static string SerializeToText(object obj)
        {
            Type type = obj.GetType();
            System.Reflection.PropertyInfo[] properties = type.GetProperties();

            StringBuilder str = new StringBuilder();
            foreach (System.Reflection.PropertyInfo property in properties)
            {
                object value = property.GetValue(obj, null);
                str.AppendLine($"{property.Name} = {value}");
            }
            return str.ToString();
        }

        public static T DeserializeFromText<T>(string text)
        {
            T config = Activator.CreateInstance<T>();

            string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            System.Reflection.PropertyInfo[] properties = typeof(T).GetProperties();
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                line = line.Trim();

                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                int charIndex = line.IndexOf('=');
                string parameterName = line.Substring(0, charIndex).TrimEnd();
                string parameterValue = line.Remove(0, charIndex + 1).TrimStart();

                parameterName = parameterName.ToUpperInvariant();
                System.Reflection.PropertyInfo property = Array.Find(properties, x => x.Name.ToUpper() == parameterName);
                if (property != null)
                {
                    try
                    {
                        object value = Convert.ChangeType(parameterValue, property.PropertyType);
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


        private static XmlSerializer GetXmlSerializer(Type type)
        {
            InitializeXmlType(type);
            lock (xmlSerializers)
            {
                return xmlSerializers[type];
            }
        }

        private static void InitializeXmlType(Type type)
        {
            lock (xmlSerializers)
            {
                if (!xmlSerializers.ContainsKey(type))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(type);
                    xmlSerializers.Add(type, xmlSerializer);
                }
            }
        }

        private static void CorrectObject(object obj)
        {
            //  исправление многострочного string после десериализации XML
            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (property.PropertyType == typeof(string))
                {
                    string value = (string)property.GetValue(obj, null);
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
