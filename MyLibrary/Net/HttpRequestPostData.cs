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
            postData.ContentType = "multipart/form-data; boundary=" + postData.MultiPartContent.Boundary;
            return postData;
        }

        public string ContentType { get; set; }
        public string StringContent { get; private set; }
        public Encoding StringContentEncoding { get; private set; }
        public byte[] BytesContent { get; private set; }
        public MultiPartContentInfo MultiPartContent { get; private set; }

        public void SetStringContent(string value)
        {
            SetStringContent(value, Encoding.UTF8);
        }
        public void SetStringContent(string value, Encoding encoding)
        {
            ClearContent();
            StringContent = value;
            StringContentEncoding = encoding;
        }
        public void SetBytesContent(byte[] value)
        {
            ClearContent();
            BytesContent = value;
        }
        public void SetMultiPartContent(string contentDisposition, string contentType, byte[] value)
        {
            ClearContent();

            var boundary = new StringBuilder();
            boundary.Append("-----------------------------");
            for (int i = 0; i < 14; i++)
            {
                boundary.Append(_rnd.Next(10));
            }

            MultiPartContent = new MultiPartContentInfo()
            {
                Boundary = boundary.ToString(),
                ContentDisposition = contentDisposition,
                ContentType = contentType,
                Content = value,
            };
        }
        public byte[] GetContent()
        {
            if (StringContent != null)
            {
                return StringContentEncoding.GetBytes(StringContent);
            }

            if (BytesContent != null)
            {
                return BytesContent;
            }

            if (MultiPartContent != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
                    {
                        streamWriter.WriteLine(MultiPartContent.Boundary);
                        if (MultiPartContent.ContentDisposition != null)
                        {
                            streamWriter.Write("Content-Disposition: ");
                            streamWriter.WriteLine(MultiPartContent.ContentDisposition);
                        }
                        if (MultiPartContent.ContentType != null)
                        {
                            streamWriter.Write("Content-Type: ");
                            streamWriter.WriteLine(MultiPartContent.ContentType);
                        }
                        streamWriter.WriteLine();
                        streamWriter.Flush();

                        memoryStream.Write(MultiPartContent.Content, 0, MultiPartContent.Content.Length);

                        streamWriter.WriteLine();
                        streamWriter.Write(MultiPartContent.Boundary);
                        streamWriter.WriteLine("--");
                        streamWriter.Flush();
                    }
                    return memoryStream.ToArray();
                }
            }

            throw new NotImplementedException();
        }

        private void ClearContent()
        {
            StringContent = null;
            StringContentEncoding = null;
            BytesContent = null;
            MultiPartContent = null;
        }
        private static Random _rnd = new Random();
    }

    public class MultiPartContentInfo
    {
        public string Boundary { get; set; }
        public string ContentDisposition { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
    }
}
