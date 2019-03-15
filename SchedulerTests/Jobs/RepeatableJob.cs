using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SchedulerManager.Executor;

namespace SchedulerTests.Jobs
{
    /// <summary>
    /// A simple repeatable Job.
    /// </summary>
    class RepeatableJob : IJob
    {
        private int _counter = 0;
        private int _maxCounter = 5;

        public int MaxCounter
        {
            get { return _maxCounter; }
        }

        public int Counter
        {
            get { return _counter; }
        }
        
        public override string GetName()
        {
            return this.GetType().Name;
        }

        public override void DoJob()
        {
            System.Console.WriteLine(
                    String.Format(
                            "This is the execution number \"{0}\" of the Job \"{1}\".",
                            _counter.ToString(),
                            this.GetName()
                        )
                );
            ++_counter;
        }

        public override bool IsRepeatable()
        {
            return (_counter < _maxCounter);
        }

        public override int GetDelayInMilliseconds()
        {
            return 200;
        }
    }
}
