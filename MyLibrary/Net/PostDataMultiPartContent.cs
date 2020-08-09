using System;
using System.IO;
using System.Text;

namespace MyLibrary.Net
{
    public class PostDataMultiPartContent : IPostDataContent
    {
        private static readonly Random rnd = new Random();

        public string Boundary { get; private set; }
        public string ContentDisposition { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }


        public PostDataMultiPartContent(byte[] content, string contentDisposition = null, string contentType = null) : this()
        {
            Content = content;
            ContentDisposition = contentDisposition;
            ContentType = contentType;
        }


        public byte[] GetContent()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (StreamWriter streamWriter = new StreamWriter(memoryStream))
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


        private PostDataMultiPartContent()
        {
            StringBuilder boundary = new StringBuilder();
            for (int i = 0; i < 14; i++)
            {
                boundary.Append(rnd.Next(10));
            }
            Boundary = boundary.ToString();
        }
    }
}
