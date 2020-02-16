using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MyLibrary.Net
{
    public interface IPostDataContent
    {
        byte[] GetContent();
        string GetContentType();
    }

    public class PostDataContent : IPostDataContent
    {
        public PostDataContent()
        {

        }
        public PostDataContent(byte[] content, string contentType) : this()
        {
            Content = content;
            ContentType = contentType;
        }

        public byte[] Content { get; set; }
        public string ContentType { get; set; }

        public byte[] GetContent()
        {
            return Content;
        }
        public string GetContentType()
        {
            return ContentType;
        }
    }

    public class PostDataStringContent : IPostDataContent
    {
        public PostDataStringContent(string text)
        {
            Text = text;
        }
        public PostDataStringContent(string text, Encoding encoding) : this(text)
        {
            Encoding = encoding;
        }
        public PostDataStringContent(string text, Encoding encoding, string contentType) : this(text, encoding)
        {
            ContentType = contentType;
        }

        public string Text { get; set; }
        public Encoding Encoding { get; set; } = Encoding.UTF8;
        public string ContentType { get; set; } = "application/x-www-form-urlencoded";

        public byte[] GetContent()
        {
            return Encoding.GetBytes(Text);
        }
        public string GetContentType()
        {
            return $"{ContentType}; charset={Encoding.WebName}";
        }

        public override string ToString()
        {
            return Text;
        }
    }

    public class PostDataBytesContent : IPostDataContent
    {
        public PostDataBytesContent(byte[] content)
        {
            Content = content;
        }

        public byte[] Content { get; set; }

        public byte[] GetContent()
        {
            return Content;
        }
        public string GetContentType()
        {
            return "application/x-www-form-urlencoded";
        }
    }

    public class PostDataMultiPartContent : IPostDataContent
    {
        private PostDataMultiPartContent()
        {
            var boundary = new StringBuilder();
            for (var i = 0; i < 14; i++)
            {
                boundary.Append(_rnd.Next(10));
            }
            Boundary = boundary.ToString();
        }
        public PostDataMultiPartContent(byte[] content, string contentDisposition = null, string contentType = null) : this()
        {
            Content = content;
            ContentDisposition = contentDisposition;
            ContentType = contentType;
        }

        public string Boundary { get; private set; }
        public string ContentDisposition { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }

        public byte[] GetContent()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(memoryStream))
                {
                    streamWriter.WriteLine($"-----------------------------{Boundary}");
                    if (ContentDisposition != null)
                    {
                        streamWriter.Write("Content-Disposition: ");
                        streamWriter.WriteLine(ContentDisposition);
                    }
                    if (ContentType != null)
                    {
                        streamWriter.Write("Content-Type: ");
                        streamWriter.WriteLine(ContentType);
                    }
                    streamWriter.WriteLine();
                    streamWriter.Flush();

                    memoryStream.Write(Content, 0, Content.Length);

                    streamWriter.WriteLine();
                    streamWriter.Write($"-----------------------------{Boundary}--");
                    streamWriter.Flush();
                }
                return memoryStream.ToArray();
            }
        }
        public string GetContentType()
        {
            return $"multipart/form-data; boundary=---------------------------{Boundary}";
        }

        private static readonly Random _rnd = new Random();
    }

    public class PostDataMultiPartListContent : IPostDataContent
    {
        public PostDataMultiPartListContent()
        {
            var boundary = new StringBuilder();
            for (var i = 0; i < 14; i++)
            {
                boundary.Append(_rnd.Next(10));
            }
            Boundary = boundary.ToString();
        }

        public string Boundary { get; private set; }
        public List<string[]> Items { get; private set; } = new List<string[]>();

        public void AddItem(string name, string value)
        {
            AddItem(name, value, "form-data");
        }
        public void AddItem(string name, string value, string contentDisposition)
        {
            Items.Add(new string[]
            {
                contentDisposition,
                name,
                value,
            });
        }
        public byte[] GetContent()
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var streamWriter = new StreamWriter(memoryStream))
                {
                    foreach (var item in Items)
                    {
                        streamWriter.WriteLine($"--MU--{Boundary}--");
                        streamWriter.WriteLine($"Content-Disposition: {item[0]}; name=\"{item[1]}\"");
                        streamWriter.WriteLine();
                        streamWriter.WriteLine(item[2]);
                    }
                    streamWriter.WriteLine($"--MU--{Boundary}----");
                    streamWriter.WriteLine();
                    streamWriter.Flush();
                }
                return memoryStream.ToArray();
            }
        }
        public string GetContentType()
        {
            return $"multipart/form-data; boundary=MU--{Boundary}--";
        }

        private static readonly Random _rnd = new Random();
    }
}
