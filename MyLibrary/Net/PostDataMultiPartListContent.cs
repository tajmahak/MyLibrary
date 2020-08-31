using System.Collections.Generic;
using System.IO;

namespace MyLibrary.Net
{
    public class PostDataMultiPartListContent : IPostDataContent
    {
        public string Boundary { get; private set; } = "18637495287369";
        public List<string[]> Items { get; private set; } = new List<string[]>();

        public void Add(string name, string value)
        {
            Add(name, value, "form-data");
        }

        public void Add(string name, string value, string contentDisposition)
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
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (StreamWriter streamWriter = new StreamWriter(memoryStream))
                {
                    foreach (string[] item in Items)
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
    }
}
