using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;

namespace MyLibrary.Json
{
    public class JsonNode : IEnumerable<JsonNode>
    {
        public static JsonNode Parse(string json, bool useDecodeString = false)
        {
            if (useDecodeString)
            {
                json = DecodeJSString(json);
            }

            JToken token = JToken.Parse(json);
            JsonNode node = new JsonNode();
            ParseNode(node, token);

            return node;
        }

        public string Name { get; set; }
        public string Value { get; set; }

        public JsonNode Parent { get; set; }
        public JsonNodeCollection Childs
        {
            get
            {
                if (_childs != null)
                {
                    return _childs;
                }

                return new JsonNodeCollection(0); // чтобы не было исключения при использовании foreach
            }
            set => _childs = value;
        }
        public bool HasChilds => (_childs != null && _childs.Count > 0);

        public JsonNode this[int index] => Childs[index];
        public JsonNode this[string name] => Childs[name];

        public override string ToString()
        {
            string str = Name;
            if (Value != null)
            {
                str += (" = '" + Value + "'");
            }

            if (HasChilds)
            {
                str += (" [" + Childs.Count + "]");
            }

            return str.TrimStart();
        }

        #region Скрытые сущности

        // Преобразование JSON перед парсингом для избежания ошибки "Bad JSON escape sequence..."
        // Источник: https://github.com/JamesNK/Newtonsoft.Json/issues/980
        private static string DecodeJSString(string s)
        {
            StringBuilder builder;
            char ch, ch2;
            int num, num2, num3, num4, num5, num6, num7, num8;
            if (string.IsNullOrEmpty(s) || !s.Contains(@"\"))
            {
                return s;
            }
            builder = new StringBuilder();
            num = s.Length;
            num2 = 0;
            while (num2 < num)
            {
                ch = s[num2];
                if (ch != 0x5c)
                {
                    builder.Append(ch);
                }
                else if (num2 < (num - 5) && s[num2 + 1] == 0x75)
                {
                    num3 = HexToInt(s[num2 + 2]);
                    num4 = HexToInt(s[num2 + 3]);
                    num5 = HexToInt(s[num2 + 4]);
                    num6 = HexToInt(s[num2 + 5]);
                    if (num3 < 0 || num4 < 0 | num5 < 0 || num6 < 0)
                    {
                        builder.Append(ch);
                    }
                    else
                    {
                        ch = (char)((((num3 << 12) | (num4 << 8)) | (num5 << 4)) | num6);
                        num2 += 5;
                        builder.Append(ch);
                    }
                }
                else if (num2 < (num - 3) && s[num2 + 1] == 0x78)
                {
                    num7 = HexToInt(s[num2 + 2]);
                    num8 = HexToInt(s[num2 + 3]);
                    if (num7 < 0 || num8 < 0)
                    {
                        builder.Append(ch);
                    }
                    else
                    {
                        ch = (char)((num7 << 4) | num8);
                        num2 += 3;
                        builder.Append(ch);
                    }
                }
                else
                {
                    if (num2 < (num - 1))
                    {
                        ch2 = s[num2 + 1];
                        if (ch2 == 0x5c)
                        {
                            builder.Append(@"\");
                            num2 += 1;
                        }
                        else if (ch2 == 110)
                        {
                            builder.Append("\n");
                            num2 += 1;
                        }
                        else if (ch2 == 0x74)
                        {
                            builder.Append("\t");
                            num2 += 1;
                        }
                    }
                    builder.Append(ch);
                }
                num2 += 1;
            }
            return builder.ToString();
        }
        private static string EncodeJSString(string sInput)
        {
            StringBuilder builder;
            string str;
            char ch;
            int num;
            builder = new StringBuilder(sInput);
            builder.Replace(@"\", @"\\");
            builder.Replace("\r", @"\r");
            builder.Replace("\n", @"\n");
            builder.Replace("\"", "\\\"");
            str = builder.ToString();
            builder = new StringBuilder();
            num = 0;
            while (num < str.Length)
            {
                ch = str[num];
                if (0x7f >= ch)
                {
                    builder.Append(ch);
                }
                else
                {
                    builder.AppendFormat(@"\u{0:X4}", (int)ch);
                }
                num += 1;
            }
            return builder.ToString();
        }
        private static int HexToInt(char h)
        {
            if (h < 0x30 || h > 0x39)
            {
                if (h < 0x61 || h > 0x66)
                {
                    if (h < 0x41 || h > 0x46)
                    {
                        return -1;
                    }
                    return ((h - 0x41) + 10);
                }
                return ((h - 0x61) + 10);
            }
            return (h - 0x30);
        }

        private JsonNodeCollection _childs; // сделано для уменьшения потребления ОЗУ
        private static void ParseNode(JsonNode node, JToken token)
        {
            #region JProperty
            if (token is JProperty)
            {
                JProperty jProperty = token as JProperty;
                node.Name = jProperty.Name;
                ParseNode(node, jProperty.Value);
            }

            #endregion
            #region JValue
            else if (token is JValue)
            {
                JValue jValue = token as JValue;
                node.Value = jValue.ToString();
            }
            #endregion
            #region JContainer
            else if (token is JContainer)
            {
                JContainer jContainer = token as JContainer;
                node.Childs = new JsonNodeCollection();
                foreach (JToken cToken in jContainer)
                {
                    JsonNode cNode = new JsonNode();
                    cNode.Parent = node;
                    ParseNode(cNode, cToken);
                    node.Childs.Add(cNode);
                }
            }
            #endregion
        }

        public IEnumerator<JsonNode> GetEnumerator()
        {
            return Childs.GetEnumerator();
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Childs.GetEnumerator();
        }

        #endregion
    }
    public class JsonNodeCollection : List<JsonNode>
    {
        public JsonNodeCollection()
        {
        }
        public JsonNodeCollection(int capacity) : base(capacity)
        {
        }

        public JsonNode this[string name]
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    JsonNode node = this[i];
                    if (node.Name == name)
                    {
                        return node;
                    }
                }
                return null;
            }
        }
    }
}
