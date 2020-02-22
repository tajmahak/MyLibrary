namespace MyLibrary.Net
{
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
}
