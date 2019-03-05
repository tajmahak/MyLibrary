using System;
using System.Threading;

namespace MyLibrary.Threading
{
    [System.Diagnostics.DebuggerNonUserCode]
    public class ThreadManager
    {
        public Exception ThreadException { get; private set; }
        public bool Aborted
        {
            get
            {
                return _aborted;
            }
        }

        public ThreadManager(int threadsCount, int tasksCount)
        {
            _threads = new Thread[threadsCount];
            _tasksCount = tasksCount;
        }
        public void Start(Action<int> action)
        {
            Start((manager, index) =>
                action(index));
        }
        public void Start(Action<ThreadManager, int> action)
        {
            if (Started != null)
                Started(this, EventArgs.Empty);

            _index = 0;
            for (int i = 0; i < _threads.Length; i++)
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        while (true)
                        {
                            if (Aborted)
                                return;

                            int index;
                            lock (_threads)
                            {
                                if (_index >= _tasksCount)
                                    break;
                                index = _index;
                                _index++;
                            }
                            action(this, index);
                        }
                    }
                    catch (ThreadAbortException)
                    {
                    }
                    catch (Exception ex)
                    {
                        lock (_threads)
                        {
                            if (ThreadException == null)
                            {
                                ThreadException = ex;
                                Abort();
                            }
                        }
                    }
                    finally
                    {
                        lock (_threads)
                        {
                            _completedThreads++;
                            if (_completedThreads == _threads.Length)
                            {
                                if (Completed != null)
                                    Completed(this, EventArgs.Empty);
                            }
                        }
                    }
                });
                thread.IsBackground = true;
                _threads[i] = thread;
            }
            for (int i = 0; i < _threads.Length; i++)
                _threads[i].Start();
        }
        public void Abort()
        {
            _aborted = true;
            lock (_threads)
            {
                for (int i = 0; i < _threads.Length; i++)
                    _threads[i].Abort();
            }
        }
        public void SafeAbort()
        {
            _aborted = true;
        }
        public Thread AbortAsync()
        {
            var thread = new Thread(Abort);
            thread.IsBackground = true;
            thread.Start();
            return thread;
        }
        public void Join()
        {
            for (int i = 0; i < _threads.Length; i++)
                _threads[i].Join();
            if (ThreadException != null)
                throw ThreadException;
        }

        public event EventHandler<EventArgs> Started;
        public event EventHandler<EventArgs> Completed;

        private Thread[] _threads;
        private volatile int _tasksCount;
        private volatile int _index;
        private volatile int _completedThreads;
        private volatile bool _aborted;
    }
}
