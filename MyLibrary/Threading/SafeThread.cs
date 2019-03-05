using System;
using System.Threading;

namespace MyLibrary.Threading
{
    public class SafeThread
    {
        public SafeThread(Action<SafeThread> action)
        {
            CurrentThread = new Thread(() =>
            {
                action(this);
            });
            CurrentThread.IsBackground = true;
        }
        public static SafeThread Start(Action<SafeThread> action)
        {
            var safeThread = new SafeThread(action);
            safeThread.CurrentThread.Start();
            return safeThread;
        }

        public Thread CurrentThread { get; private set; }
        public bool Aborted
        {
            get
            {
                return _aborted;
            }
        }
        public void Join()
        {
            CurrentThread.Join();
        }
        public void SafeAbort()
        {
            _aborted = true;
        }

        private volatile bool _aborted;
    }
}
