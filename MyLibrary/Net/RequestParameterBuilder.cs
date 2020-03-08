using System.Text;

namespace MyLibrary.Net
{
    public class RequestParameterBuilder
    {
        public RequestParameterBuilder Add(string name, object value)
        {
            if (_str.Length > 0)
            {
                _str.Append("&");
            }
            _str.Append(name);
            _str.Append("=");
            _str.Append(value);

            return this;
        }

        public override string ToString()
        {
            return _str.ToString();
        }

        private readonly StringBuilder _str = new StringBuilder();
    }
}
