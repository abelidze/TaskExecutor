using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SchedulerManager.Executor
{
    /// <summary>
    /// Classes which extend this abstract class are Jobs which can be fetched and executed by JobManager.
    /// </summary>
    public abstract class IJob
    {
        protected bool _isFinished = false;
        protected Task _task = null;

        /// <sumary>
        /// Job is finished.
        /// </sumary>
        public virtual bool IsFinished { get { return _isFinished; } }

        /// <sumary>
        /// Get / Set attached task
        /// </sumary>
        public Task AttachedTask
        {
            get { return _task; }
        }

        /// <sumary>
        /// Building task using JobManager.
        /// </sumary>
        public virtual void BuildTask(JobManager jm)
        {
            _task = jm.GetFactory().StartNew(() => ExecuteJob(jm));
        }

        /// <sumary>
        /// Wait for task completion
        /// </sumary>
        public virtual void Wait()
        {
            while (!IsFinished) {
                try {
                    _task.Wait();
                    Thread.Sleep(20);
                } catch {
                    break;
                }
            }
        }

        /// <summary>
        /// Execute Job with its strategy.
        /// </summary>
        public virtual void ExecuteJob(JobManager jm)
        {
            _isFinished = false;
            DoJob();
            if (IsRepeatable()) {
                _task = jm.Execute(async () => {
                    await Task.Delay(GetDelayInMilliseconds());
                    ExecuteJob(jm);
                });
            } else {
                _isFinished = true;
            }
        }

        /// <summary>
        /// Job's parameters. It is optional.
        /// </summary>
        /// <returns>Parameters to be used in the job.</returns>
        public virtual Object GetParameters()
        {
            return null;
        }

        /// <summary>
        /// The amount of time, in milliseconds, which the Job has to wait until it is started over. It is optional.
        /// </summary>
        /// <returns>Interval time between this job executions.</returns>
        public virtual int GetDelayInMilliseconds()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the job's name.
        /// </summary>
        /// <returns>Job's name.</returns>
        public abstract String GetName();

        /// <summary>
        /// The job to be executed.
        /// </summary>
        public abstract void DoJob();

        /// <summary>
        /// Determines whether a job is to be repeated after a certain amount of time.
        /// </summary>
        /// <returns>True in case the job is to be repeated, false otherwise.</returns>
        public abstract bool IsRepeatable();
    }
}