using HtmlAgilityPack;
using System;

namespace MyLibrary.Data.Formats
{
    public static class HtmlAgilityPackExtension
    {
        public static HtmlDocument Parse(string html)
        {
            var document = new HtmlDocument();
            document.LoadHtml(html);
            return document;
        }

        public static HtmlNodeCollection Filter(this HtmlDocument document, Predicate<HtmlNode> pattern)
        {
            return Filter(document.DocumentNode, pattern);
        }
        public static HtmlNodeCollection Filter(this HtmlNode node, Predicate<HtmlNode> pattern)
        {
            return Filter(node.ChildNodes, pattern);
        }
        public static HtmlNodeCollection Filter(this HtmlNodeCollection collection, Predicate<HtmlNode> pattern)
        {
            if (collection.Count == 0)
            {
                return collection;
            }

            var newCollection = new HtmlNodeCollection(collection[0].ParentNode);
            for (var i = 0; i < collection.Count; i++)
            {
                if (pattern(collection[i]))
                {
                    newCollection.Append(collection[i]);
                }
            }
            return newCollection;
        }
        public static HtmlNodeCollection Find(this HtmlDocument document, Predicate<HtmlNode> pattern)
        {
            return Find(document.DocumentNode, pattern);
        }
        public static HtmlNodeCollection Find(this HtmlNode node, Predicate<HtmlNode> pattern)
        {
            return Find(node.ChildNodes, pattern);
        }
        public static HtmlNodeCollection Find(this HtmlNodeCollection collection, Predicate<HtmlNode> pattern)
        {
            var newCollection = new HtmlNodeCollection(null);
            foreach (var node in collection)
            {
                if (pattern(node))
                {
                    newCollection.Add(node);
                }

                if (node.ChildNodes.Count > 0)
                {
                    var child_collection = Find(node.ChildNodes, pattern);
                    foreach (var childNode in child_collection)
                    {
                        newCollection.Add(childNode);
                    }
                }
            }
            return newCollection;
        }

        public static bool HasAttribute(this HtmlNode node, string name)
        {
            return node.Attributes.Contains(name);
        }
        public static bool HasAttribute(this HtmlNode node, string name, string value)
        {
            if (!node.Attributes.Contains(name))
            {
                return false;
            }

            return node.Attributes[name].Value == value;
        }
        public static bool HasClass(this HtmlNode node, string value)
        {
            return HasAttribute(node, "class", value);
        }
        public static string GetAttributeValue(this HtmlNode node, string name)
        {
            return node.Attributes[name].Value;
        }
    }
}
