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
        /// <summary>
        /// Запуск обработки
        /// </summary>
        /// <param name="threadsCount">Количество создаваемых потоков для выполнения задач</param>
        /// <param name="tasksCount">Количество выполняемых задач</param>
        /// <param name="action">Делегат выполняемой операции. Action(index), index - индекс выполняемой задачи</param>
        /// <returns></returns>
        public static ThreadManager Start(int threadsCount, int tasksCount, Action<int> action)
        {
            ThreadManager threadManager = new ThreadManager(threadsCount, tasksCount);
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
            ThreadManager threadManager = new ThreadManager(threadsCount, tasksCount);
            threadManager.Start(action);
            return threadManager;
        }


        public bool Aborted => aborted;
        private readonly Thread[] threads;
        private readonly int tasksCount;
        private volatile bool aborted;

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

            threads = new Thread[threadsCount];
            this.tasksCount = tasksCount;
        }


        /// <summary>
        /// Происходит перед началом операций
        /// </summary>
        public event EventHandler<EventArgs> Started;

        /// <summary>
        /// Происходит после завершения/остановки всех потоковых операций
        /// </summary>
        public event EventHandler<EventArgs> Completed;


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

            int completedThreads = 0;
            int index = 0;
            for (int i = 0; i < threads.Length; i++)
            {
                Thread thread = new Thread(() =>
                {
                    try
                    {
                        while (true)
                        {
                            if (aborted)
                            {
                                return;
                            }

                            int current_index;
                            lock (threads)
                            {
                                if (index >= tasksCount)
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
                        lock (threads)
                        {
                            completedThreads++;
                            if (completedThreads == threads.Length)
                            {
                                Completed?.Invoke(this, EventArgs.Empty);
                            }
                        }
                    }
                });
                thread.IsBackground = true;
                thread.Start();
                threads[i] = thread;
            }
        }

        /// <summary>
        /// Принудительно останавливает выполнение операции для всех потоков вызовом метода Thread.Abort()
        /// </summary>
        public void Abort()
        {
            aborted = true;
            lock (threads)
            {
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i].Abort();
                }
            }
        }

        /// <summary>
        /// Безопасная остановка выполнения операции с использованием свойства Abort
        /// </summary>
        public void SafeAbort()
        {
            aborted = true;
        }

        /// <summary>
        /// Блокирует вызывающий поток до завершения всех потоков
        /// </summary>
        public void Join()
        {
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }
        }
    }
}
