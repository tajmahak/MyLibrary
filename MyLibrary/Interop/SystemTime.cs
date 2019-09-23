using System;
using static MyLibrary.Interop.NativeMethods;

namespace MyLibrary.Interop
{
    public static class SystemTime
    {
        public static bool SetSystemTime(DateTime time)
        {
            var systemTime = new SYSTEMTIME
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
