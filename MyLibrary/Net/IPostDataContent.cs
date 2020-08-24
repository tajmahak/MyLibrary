namespace MyLibrary.Net
{
    public interface IPostDataContent
    {
        byte[] GetContent();

        string GetContentType();
    }
}
