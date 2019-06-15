using System;
using System.Diagnostics;
using System.Threading;

namespace MyLibrary.Threading
{
    /// <summary>
    /// Менеджер многопоточной обработки данных
    /// </summary>
    [DebuggerStepThrough]
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
        public bool Aborted => _aborted;

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
            Start((manager, index) => action(index));
        }
        /// <summary>
        /// Запуск обработки
        /// </summary>
        /// <param name="action">Делегат выполняемой операции. Action(manager, index), manager - экземпляр текущего класса ThreadManager, index - индекс выполняемой задачи</param>
        public void Start(Action<ThreadManager, int> action)
        {
            Started?.Invoke(this, EventArgs.Empty);

            var completedThreads = 0;
            var index = 0;
            for (var i = 0; i < _threads.Length; i++)
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        while (true)
                        {
                            if (_aborted)
                                return;

                            int current_index;
                            lock (_threads)
                            {
                                if (index >= _tasksCount)
                                {
                                    break;
                                }
                                current_index = index;
                                index++;
                            }
                            action(this, current_index);
                        }
                    }
                    finally
                    {
                        lock (_threads)
                        {
                            completedThreads++;
                            if (completedThreads == _threads.Length)
                            {
                                Completed?.Invoke(this, EventArgs.Empty);
                            }
                        }
                    }
                });
                thread.IsBackground = true;
                thread.Start();
                _threads[i] = thread;
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
                for (var i = 0; i < _threads.Length; i++)
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
            for (var i = 0; i < _threads.Length; i++)
            {
                _threads[i].Join();
            }
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
        private readonly Thread[] _threads;
        private readonly int _tasksCount;
        private volatile bool _aborted;
    }
}
