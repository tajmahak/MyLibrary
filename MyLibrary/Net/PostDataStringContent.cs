using System.Text;

namespace MyLibrary.Net
{
    public class PostDataStringContent : IPostDataContent
    {
        public PostDataStringContent(string data)
        {
            Text = data;
        }
        public PostDataStringContent(string data, Encoding encoding) : this(data)
        {
            Encoding = encoding;
        }
        public PostDataStringContent(string data, Encoding encoding, string contentType) : this(data, encoding)
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
}
