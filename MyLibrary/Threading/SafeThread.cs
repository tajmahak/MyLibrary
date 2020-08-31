using System;
using System.Threading;

namespace MyLibrary.Threading
{
    /// <summary>
    /// Поток с безопасным завершением операции через управляемое поле Aborted
    /// </summary>
    public class SafeThread
    {
        public SafeThread(Action<SafeThread> action)
        {
            CurrentThread = new Thread(() => action(this));
            CurrentThread.IsBackground = true;
        }

        public static SafeThread Start(Action<SafeThread> action)
        {
            SafeThread safeThread = new SafeThread(action);
            safeThread.CurrentThread.Start();
            return safeThread;
        }

        public Thread CurrentThread { get; private set; }
        public bool Aborted => aborted;
        private volatile bool aborted;

        public void Start()
        {
            CurrentThread.Start();
        }

        public void Join()
        {
            CurrentThread.Join();
        }

        public void SafeAbort()
        {
            aborted = true;
        }
    }
}
