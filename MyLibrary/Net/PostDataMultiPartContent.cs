using System.IO;

namespace MyLibrary.Net
{
    public class PostDataMultiPartContent : IPostDataContent
    {
        public PostDataMultiPartContent(byte[] content, string contentDisposition = null, string contentType = null)
        {
            Content = content;
            ContentDisposition = contentDisposition;
            ContentType = contentType;
        }

        public string Boundary { get; private set; } = "85692478526984";
        public string ContentDisposition { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }

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
    }
}
