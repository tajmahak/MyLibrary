using System;
using System.Threading;

namespace MyLibrary.Threading
{
    public static class ThreadExtension
    {
        public static Thread StartThread(ThreadStart start)
        {
            Thread thread = new Thread(start);
            thread.IsBackground = false;
            thread.Start();
            return thread;
        }

        public static Thread StartBackgroundThread(ThreadStart start)
        {
            Thread thread = new Thread(start);
            thread.IsBackground = true;
            thread.Start();
            return thread;
        }

        public static SafeThread StartSafeThread(Action<SafeThread> start)
        {
            SafeThread thread = new SafeThread(start);
            thread.Start();
            return thread;
        }
    }
}
