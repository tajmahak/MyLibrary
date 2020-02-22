namespace MyLibrary.Net
{
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
}
