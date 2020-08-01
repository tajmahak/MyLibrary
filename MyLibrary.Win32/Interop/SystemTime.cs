using System;
using static MyLibrary.Win32.Interop.NativeMethods;

namespace MyLibrary.Win32.Interop
{
    public static class SystemTime
    {
        public static bool SetSystemTime(DateTime time)
        {
            SYSTEMTIME systemTime = new SYSTEMTIME
            {
                wDay = (short)time.Day,
                wDayOfWeek = (short)time.DayOfWeek,
                wHour = (short)time.Hour,
                wMilliseconds = (short)time.Millisecond,
                wMinute = (short)time.Minute,
                wMonth = (short)time.Month,
                wSecond = (short)time.Second,
                wYear = (short)time.Year
            };
            return NativeMethods.SetSystemTime(ref systemTime);
        }
    }
}
