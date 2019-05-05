using System;
using System.IO;
using System.Text;

namespace MyLibrary.Net
{
    public class HttpRequestPostData
    {
        public static HttpRequestPostData FromString(string value)
        {
            return FromString(value, Encoding.UTF8);
        }
        public static HttpRequestPostData FromString(string value, Encoding encoding)
        {
            var postData = new HttpRequestPostData();
            postData.SetStringContent(value, encoding);
            postData.ContentType = "application/x-www-form-urlencoded; charset=" + encoding.WebName;
            return postData;
        }
        public static HttpRequestPostData FromBytes(byte[] value)
        {
            var postData = new HttpRequestPostData();
            postData.SetBytesContent(value);
            postData.ContentType = "application/x-www-form-urlencoded";
            return postData;
        }
        public static HttpRequestPostData FromMultiPart(string contentDisposition, string contentType, byte[] value)
        {
            var postData = new HttpRequestPostData();
            postData.SetMultiPartContent(contentDisposition, contentType, value);
            postData.ContentType = "multipart/form-data; boundary=---------------------------" + (postData.Content as MultiPartPostDataContent).Boundary;
            return postData;
        }

        public string ContentType { get; set; }
        public IPostDataContent Content { get; private set; }

        public void SetStringContent(string value)
        {
            SetStringContent(value, Encoding.UTF8);
        }
        public void SetStringContent(string value, Encoding encoding)
        {
            Content = new StringPostDataContent()
            {
                Text = value,
                Encoding = encoding,
            };
        }
        public void SetBytesContent(byte[] value)
        {
            Content = new BytesPostDataContent()
            {
                Data = value,
            };
        }
        public void SetMultiPartContent(string contentDisposition, string contentType, byte[] value)
        {
            var boundary = new StringBuilder();
            for (int i = 0; i < 14; i++)
            {
                boundary.Append(_rnd.Next(10));
            }

            Content = new MultiPartPostDataContent()
            {
                Boundary = boundary.ToString(),
                ContentDisposition = contentDisposition,
                ContentType = contentType,
                Content = value,
            };
        }
        public byte[] GetContent()
        {
            if (Content is StringPostDataContent)
            {
                var content = Content as StringPostDataContent;
                return content.Encoding.GetBytes(content.Text);
            }
            if (Content is BytesPostDataContent)
            {
                var content = Content as BytesPostDataContent;
                return content.Data;
            }
            if (Content is MultiPartPostDataContent)
            {
                var content = Content as MultiPartPostDataContent;

                using (var memoryStream = new MemoryStream())
                {
                    using (var streamWriter = new StreamWriter(memoryStream))
                    {
                        streamWriter.WriteLine("-----------------------------" + content.Boundary);
                        if (content.ContentDisposition != null)
                        {
                            streamWriter.Write("Content-Disposition: ");
                            streamWriter.WriteLine(content.ContentDisposition);
                        }
                        if (content.ContentType != null)
                        {
                            streamWriter.Write("Content-Type: ");
                            streamWriter.WriteLine(content.ContentType);
                        }
                        streamWriter.WriteLine();
                        streamWriter.Flush();

                        memoryStream.Write(content.Content, 0, content.Content.Length);

                        streamWriter.WriteLine();
                        streamWriter.Write("-----------------------------" + content.Boundary + "--");
                        streamWriter.Flush();
                    }
                    return memoryStream.ToArray();
                }
            }
            throw new NotImplementedException();
        }

        private static Random _rnd = new Random();
    }

    public interface IPostDataContent { }

    public class StringPostDataContent : IPostDataContent
    {
        public string Text { get; set; }
        public Encoding Encoding { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }

    public class BytesPostDataContent : IPostDataContent
    {
        public byte[] Data { get; set; }
    }

    public class MultiPartPostDataContent : IPostDataContent
    {
        public string Boundary { get; set; }
        public string ContentDisposition { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
    }
}
