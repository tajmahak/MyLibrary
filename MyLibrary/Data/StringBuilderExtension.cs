using System.Text;

namespace MyLibrary.Data
{
    public static class StringBuilderExtension
    {
        public static StringBuilder Concat(this StringBuilder builder, params object[] values)
        {
            foreach (var value in values)
            {
                builder.Append(value);
            }
            return builder;
        }
    }
}
