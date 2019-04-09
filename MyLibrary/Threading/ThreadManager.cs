using System;
using System.Threading;

namespace MyLibrary.Threading
{
    /// <summary>
    /// Менеджер многопоточной обработки данных
    /// </summary>
    [System.Diagnostics.DebuggerNonUserCode]
    public class ThreadManager
    {
        // Статические методы
        /// <summary>
        /// Запуск обработки
        /// </summary>
        /// <param name="threadsCount">Количество создаваемых потоков для выполнения задач</param>
        /// <param name="tasksCount">Количество выполняемых задач</param>
        /// <param name="action">Делегат выполняемой операции. Action(index), index - индекс выполняемой задачи</param>
        /// <returns></returns>
        public static ThreadManager Start(int threadsCount, int tasksCount, Action<int> action)
        {
            var threadManager = new ThreadManager(threadsCount, tasksCount);
            threadManager.Start(action);
            return threadManager;
        }
        /// <summary>
        /// Запуск обработки
        /// </summary>
        /// <param name="threadsCount">Количество создаваемых потоков для выполнения задач</param>
        /// <param name="tasksCount">Количество выполняемых задач</param>
        /// <param name="action">Делегат выполняемой операции. Action(manager, index), manager - экземпляр текущего класса ThreadManager, index - индекс выполняемой задачи</param>
        /// <returns></returns>
        public static ThreadManager Start(int threadsCount, int tasksCount, Action<ThreadManager, int> action)
        {
            var threadManager = new ThreadManager(threadsCount, tasksCount);
            threadManager.Start(action);
            return threadManager;
        }

        // Свойства
        public Exception ThreadException { get; private set; }
        public bool Aborted
        {
            get
            {
                return _aborted;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="threadsCount">Количество создаваемых потоков для выполнения задач</param>
        /// <param name="tasksCount">Количество выполняемых задач</param>
        public ThreadManager(int threadsCount, int tasksCount)
        {
            if (threadsCount > tasksCount)
            {
                threadsCount = tasksCount;
            }

            _threads = new Thread[threadsCount];
            _tasksCount = tasksCount;
        }
        /// <summary>
        /// Запуск обработки
        /// </summary>
        /// <param name="action">Делегат выполняемой операции. Action(index), index - индекс выполняемой задачи</param>
        public void Start(Action<int> action)
        {
            Start((manager, index) =>
            {
                action(index);
            });
        }
        /// <summary>
        /// Запуск обработки
        /// </summary>
        /// <param name="action">Делегат выполняемой операции. Action(manager, index), manager - экземпляр текущего класса ThreadManager, index - индекс выполняемой задачи</param>
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
                            if (_aborted)
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
                                {
                                    Completed(this, EventArgs.Empty);
                                }
                            }
                        }
                    }
                });
                thread.IsBackground = true;
                _threads[i] = thread;
            }
            for (int i = 0; i < _threads.Length; i++)
            {
                _threads[i].Start();
            }
        }
        /// <summary>
        /// Принудительно останавливает выполнение операции для всех потоков вызовом метода Thread.Abort()
        /// </summary>
        public void Abort()
        {
            _aborted = true;
            lock (_threads)
            {
                for (int i = 0; i < _threads.Length; i++)
                {
                    _threads[i].Abort();
                }
            }
        }
        /// <summary>
        /// Безопасная остановка выполнения операции с использованием свойства Abort
        /// </summary>
        public void SafeAbort()
        {
            _aborted = true;
        }
        /// <summary>
        /// Блокирует вызывающий поток до завершения всех потоков
        /// </summary>
        public void Join()
        {
            for (int i = 0; i < _threads.Length; i++)
            {
                _threads[i].Join();
            }

            if (ThreadException != null)
                throw ThreadException;
        }

        // События
        /// <summary>
        /// Происходит перед началом операций
        /// </summary>
        public event EventHandler<EventArgs> Started;
        /// <summary>
        /// Происходит после завершения/остановки всех потоковых операций
        /// </summary>
        public event EventHandler<EventArgs> Completed;

        // Закрытые сущности
        private Thread[] _threads;
        private volatile int _tasksCount;
        private volatile int _index;
        private volatile int _completedThreads;
        private volatile bool _aborted;
    }
}
