using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using log4net;

namespace SchedulerManager.Executor
{
    /// <summary>
    /// Jobs manager (executor)
    /// </summary>
    public class JobManager : TaskScheduler
    {
        private ILog log = LogManager.GetLogger("SchedulerLogger");

        [ThreadStatic]
        private static bool _isProcessingQueue;

        private readonly LinkedList<Task> _tasks = new LinkedList<Task>();
        private readonly int _workersCount;
        private int _activeWorkers = 0;
        private static TaskFactory _factory;
        private static object _syncRoot = new Object();

        /// <summary>
        /// Creates a new instance with the specified workers count.
        /// </summary>
        /// <param name="workersCount">Workers count, must be >= 0. Equals ProcessoCount if zero.</param>
        public JobManager(int workersCount = 0)
        {
            if (workersCount < 0) {
                log.Error("Attempt to set workers count less then zero");
                throw new ArgumentOutOfRangeException("workersCount < 0");
            }
            if (workersCount == 0) {
                workersCount = Environment.ProcessorCount;
            }
            _workersCount = workersCount;
        }

        /// <summary>
        /// Get factory for executing tasks that returns nothing (actions). 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TaskFactory GetFactory()
        {
            if (_factory == null) {
                lock (_syncRoot) {
                    _factory = new TaskFactory(this);
                }
            }
            return _factory;
        }

        /// <summary>
        /// Get factory for executing tasks that returns value. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public TaskFactory<T> GetFactory<T>()
        {
            return Factory<T>.GetInstance(this);
        }

        /// <summary>
        /// Nested class for factories.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class Factory<T>
        {
            private static TaskFactory<T> factory;
            private static object syncRoot = new Object();

            internal static TaskFactory<T> GetInstance(TaskScheduler scheduler)
            {
                if (factory == null) {
                    lock (syncRoot) {
                        factory = new TaskFactory<T>(scheduler);
                    }
                }
                return factory;
            }
        }     

        /// <summary>
        /// Execute all job implementations.
        /// </summary>
        public List<IJob> ExecuteAllJobs()
        {
            try {
                IEnumerable<Type> jobs = GetImplementations<IJob>();

                if (jobs != null && jobs.Count() > 0) {
                    List<IJob> jobsList = new List<IJob>(jobs.Count());
                    foreach (Type jobType in jobs) {
                        IJob job = AddJob(jobType);
                        if (job != null) {
                            jobsList.Add(job);
                        }
                    }
                    return jobsList;
                }
            } catch (Exception ex) {
                log.Error("An error has occured while instantiating or executing Jobs for the Scheduler.", ex);
            }
            return null;
        }

        /// <summary>
        /// Adding new job to queue.
        /// </summary>
        public IJob AddJob<T>()
        {
            return AddJob(typeof(T));
        }

        /// <summary>
        /// Adding new job to queue.
        /// </summary>
        /// <param name="job">IJob implementation type.</param>
        public IJob AddJob(Type job)
        {
            IJob jobInstance = null;
            if (IsRealClass(job)) {
                try {
                    jobInstance = Activator.CreateInstance(job) as IJob;
                    if (jobInstance == null) {
                        throw new NotSupportedException("Unsupported job type");
                    }
                    log.Debug(String.Format("The Job \"{0}\" has been instantiated successfully.", jobInstance.GetName()));

                    jobInstance.BuildTask(this);

                    log.Debug(String.Format("The Job \"{0}\" has been executed successfully.", jobInstance.GetName()));
                } catch (Exception ex) {
                    log.Error(String.Format("The Job \"{0}\" could not be instantiated or executed.", job.Name), ex);
                }
            } else {
                log.Error(String.Format("The Job \"{0}\" cannot be instantiated.", job.FullName));
            }
            return jobInstance;
        }

        /// <summary>
        /// Adding new Task to queue.
        /// </summary>
        /// <param name="task"></param>
        public void AddTask(Task task)
        {
            task.Start(this);
        }

        /// <summary>
        /// Adding new Task&lt;TResult&gt; to queue.
        /// </summary>
        /// <param name="task"></param>
        public void AddTask<T>(Task<T> task)
        {
            task.Start(this);
        }

        /// <summary>
        /// Executing Action.
        /// </summary>
        public Task Execute(Action f)
        {
            return GetFactory().StartNew(f);
        }

        /// <summary>
        /// Executing Func&lt;TResult&gt;.
        /// </summary>
        public Task<T> Execute<T>(Func<T> f)
        {
            return GetFactory<T>().StartNew(f);
        }

        /// <summary>
        /// Clear tasks' queue.
        /// </summary>
        public void Clear()
        {
            lock (_tasks) {
                _tasks.Clear();
            }
        }

        /// <summary>
        /// Queues a task to the scheduler. 
        /// </summary>
        /// <param name="task">Specified task</param>
        protected sealed override void QueueTask(Task task)
        {
            if (task == null) {
                log.Error("QueueTask: ArgumentNullException.");
                throw new ArgumentNullException();
            }

            lock (_tasks) {
                _tasks.AddLast(task);
                if (_activeWorkers < _workersCount) {
                    ++_activeWorkers;
                    NotifyThreadPool();
                }
            }
        }

        /// <summary>
        /// Inform the ThreadPool that there's work to be executed for this scheduler.
        /// </summary>
        private void NotifyThreadPool()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ => {
                _isProcessingQueue = true;
                try {
                    while (true) {
                        Task item;
                        lock (_tasks) {
                            if (_tasks.Count == 0) {
                                --_activeWorkers;
                                break;
                            }
                            item = _tasks.First.Value;
                            _tasks.RemoveFirst();
                        }

                        base.TryExecuteTask(item);
                    }
                } finally {
                    _isProcessingQueue = false;
                }
            }, null);
        }

        /// <summary>
        /// Attempts to execute the specified task on the current thread.
        /// </summary>
        /// <param name="task">Task to execute.</param>
        /// <param name="taskWasPreviouslyQueued">If true task would be started immediately.</param>
        /// <returns>True in case task is executed, false otherwise.</returns>
        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (!_isProcessingQueue) {
                return false;
            }

            if (taskWasPreviouslyQueued) {
                if (TryDequeue(task)) {
                    return base.TryExecuteTask(task);
                } else {
                    return false;
                }
            } else {
                return base.TryExecuteTask(task);
            }
        }

        /// <summary>
        /// Attempt to remove a previously scheduled task from the scheduler.
        /// </summary>
        /// <param name="task">Removed task.</param>
        /// <returns>bool</returns>
        protected sealed override bool TryDequeue(Task task)
        {
            lock (_tasks) {
                return _tasks.Remove(task);
            }
        }

        /// <summary>
        /// Gets the maximum concurrency level supported by this scheduler.
        /// </summary>
        public sealed override int MaximumConcurrencyLevel { get { return _workersCount; } }

        /// <summary>
        /// Gets an enumerable of the tasks currently scheduled on this scheduler.
        /// </summary>
        /// <returns></returns>
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try {
                Monitor.TryEnter(_tasks, ref lockTaken);
                if (lockTaken) {
                    return _tasks;
                } else {
                    log.Error("GetScheduledTasks was failed");
                    throw new NotSupportedException();
                }
            } finally {
                if (lockTaken) {
                    Monitor.Exit(_tasks);
                }
            }
        }

        /// <summary>
        /// Returns all types in the current AppDomain implementing the interface or inheriting the type. 
        /// </summary>
        private IEnumerable<Type> GetImplementations<T>()
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(T).IsAssignableFrom(type));
        }

        /// <summary>
        /// Determine whether the object is real - non-abstract, non-generic-needed, non-interface class.
        /// </summary>
        /// <param name="testType">Type to be verified.</param>
        /// <returns>True in case the class is real, false otherwise.</returns>
        public static bool IsRealClass(Type testType)
        {
            return testType.IsAbstract == false
                && testType.IsGenericTypeDefinition == false
                && testType.IsInterface == false;
        }

    }
}