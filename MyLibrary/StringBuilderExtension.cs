﻿using System.Text;

namespace MyLibrary
{
    public static class StringBuilderExtension
    {
        public static StringBuilder Concat(this StringBuilder builder, params object[] values)
        {
            foreach (object value in values)
            {
                builder.Append(value);
            }
            return builder;
        }
    }
}
