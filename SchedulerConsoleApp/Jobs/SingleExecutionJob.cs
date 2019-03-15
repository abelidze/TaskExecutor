using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SchedulerManager.Executor;

namespace SchedulerBenchmark.Jobs
{
    /// <summary>
    /// A simple job which is executed only once.
    /// </summary>
    class SingleExecutionJob : IJob
    {
        public override string GetName()
        {
            return this.GetType().Name;
        }

        public override void DoJob()
        {
            System.Console.WriteLine(String.Format("The Job \"{0}\" was executed.", this.GetName()));
        }

        public override bool IsRepeatable()
        {
            return false;
        }
    }
}
