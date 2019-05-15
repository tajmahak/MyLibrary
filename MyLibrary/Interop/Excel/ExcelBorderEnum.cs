using System;

namespace MyLibrary.Interop.Excel
{
    [Flags]
    public enum ExcelBorderEnum
    {
        All = Top | Bottom | Left | Right,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
    }
}
