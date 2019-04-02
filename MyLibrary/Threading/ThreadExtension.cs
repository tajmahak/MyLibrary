using System.Threading;

namespace MyLibrary.MyLibrary.Threading
{
    public static class ThreadExtension
    {
        public static Thread StartThread(ThreadStart start)
        {
            var thread = new Thread(start);
            thread.IsBackground = false;
            thread.Start();
            return thread;
        }
        public static Thread StartBackgroundThread(ThreadStart start)
        {
            var thread = new Thread(start);
            thread.IsBackground = true;
            thread.Start();
            return thread;
        }
    }
}
