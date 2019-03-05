using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace MyLibrary.Data
{
    public class JsonNode : IEnumerable<JsonNode>
    {
        public static JsonNode Parse(string json)
        {
            var token = JToken.Parse(json);

            var node = new JsonNode();
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
                    return _childs;
                return new JsonNodeCollection(0); // чтобы не было исключения при использовании foreach
            }
            set
            {
                _childs = value;
            }
        }
        public bool HasChilds
        {
            get
            {
                return (_childs != null && _childs.Count > 0);
            }
        }

        public JsonNode this[int index]
        {
            get
            {
                return Childs[index];
            }
        }
        public JsonNode this[string name]
        {
            get
            {
                return Childs[name];
            }
        }

        public override string ToString()
        {
            string str = Name;
            if (Value != null)
                str += (" = '" + Value + "'");
            if (HasChilds)
                str += (" [" + Childs.Count + "]");
            return str.TrimStart();
        }

        #region Скрытые сущности

        private JsonNodeCollection _childs; // сделано для уменьшения потребления ОЗУ
        private static void ParseNode(JsonNode node, JToken token)
        {
            #region JProperty
            if (token is JProperty)
            {
                var jProperty = token as JProperty;
                node.Name = jProperty.Name;
                ParseNode(node, jProperty.Value);
            }

            #endregion
            #region JValue
            else if (token is JValue)
            {
                var jValue = token as JValue;
                node.Value = jValue.ToString();
            }
            #endregion
            #region JContainer
            else if (token is JContainer)
            {
                var jContainer = token as JContainer;
                node.Childs = new JsonNodeCollection();
                foreach (var cToken in jContainer)
                {
                    var cNode = new JsonNode();
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
        public JsonNode this[string name]
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    var node = this[i];
                    if (node.Name == name)
                        return node;
                }
                return null;
            }
        }

        #region Конструктор
        
        public JsonNodeCollection()
        {
        }
        public JsonNodeCollection(int capacity)
            : base(capacity)
        {
        }

        #endregion
    }
}
