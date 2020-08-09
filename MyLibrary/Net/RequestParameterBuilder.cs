using System.Text;

namespace MyLibrary.Net
{
    public class RequestParameterBuilder
    {
        private readonly StringBuilder str = new StringBuilder();


        public RequestParameterBuilder Add(string name, object value)
        {
            if (str.Length > 0)
            {
                str.Append("&");
            }
            str.Append(name);
            str.Append("=");
            str.Append(value);

            return this;
        }


        public override string ToString()
        {
            return str.ToString();
        }
    }
}
