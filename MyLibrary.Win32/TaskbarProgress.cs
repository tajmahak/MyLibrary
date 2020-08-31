using System;
using static MyLibrary.Win32.Interop.Native;

namespace MyLibrary.Win32
{
    public static class TaskbarProgress
    {
        private static readonly ITaskbarList3 taskbarInstance = (ITaskbarList3)new TaskbarInstance();
        private static readonly bool taskbarSupported = Environment.OSVersion.Version >= new Version(6, 1);

        public static void SetState(IntPtr windowHandle, TaskbarStates taskbarState)
        {
            if (taskbarSupported)
            {
                taskbarInstance.SetProgressState(windowHandle, taskbarState);
            }
        }

        public static void SetValue(IntPtr windowHandle, double progressValue, double progressMax)
        {
            if (taskbarSupported)
            {
                taskbarInstance.SetProgressValue(windowHandle, (ulong)progressValue, (ulong)progressMax);
            }
        }
    }

    public enum TaskbarStates
    {
        NoProgress = 0,
        Indeterminate = 0x1,
        Normal = 0x2,
        Error = 0x4,
        Paused = 0x8
    }
}
