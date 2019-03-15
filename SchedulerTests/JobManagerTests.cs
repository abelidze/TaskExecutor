using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SchedulerManager.Executor;

namespace SchedulerTests
{
    [TestClass]
    public class JobManagerTests
    {
        private static JobManager jobManager = new JobManager();

        [TestMethod]
        public void ExecuteActionTest()
        {
            bool isExecuted = false;

            var task = jobManager.Execute(() => {
                Thread.Sleep(200);
                isExecuted = true;
            });
            task.Wait();

            Assert.IsTrue(isExecuted);
        }

        [TestMethod]
        public void ExecuteFunctionTest()
        {
            int expected = 5;

            var task = jobManager.Execute<int>(() => {
                return 5;
            });

            Assert.AreEqual(task.Result, expected);
        }

        [TestMethod]
        public void WhenAllTest()
        {
            int count = 100;
            int expected = count * (count - 1) / 2;

            var tasks = new List<Task<int>>();
            for (int i = 0; i < count; ++i) {
                int num = i;
                tasks.Add(
                    jobManager.Execute<int>(() => {
                        return num;
                    })
                );
            }

            var task = Task.WhenAll(tasks).ContinueWith(async _ => {
                int[] results = await _;
                int sum = 0;
                foreach (var t in results) {
                    sum += t;
                }
                return sum;
            }).Result;

            Assert.AreEqual(expected, task.Result);
        }

        [TestMethod]
        public void WhenAnyTest()
        {
            int count = 20;
            string expected = "Complited";
            string result = "";

            var generator = new Random();
            var tasks = new List<Task>();
            for (int i = 0; i < count; ++i) {
                int num = i;
                tasks.Add(
                    jobManager.Execute(() => {
                        Thread.Sleep(generator.Next(count) * 100);
                    })
                );
            }

            Task.WhenAny(tasks).ContinueWith(_ => {
                result = "Complited";
            }).Wait();

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void FactoryAndQueueTest()
        {
            int count = 100;
            int[] expected = new int[count];

            var factory = jobManager.GetFactory<int>();
            var tasks = new List<Task<int>>();
            for (int i = 0; i < count; ++i) {
                int num = i;
                expected[i] = num;
                tasks.Add(
                    factory.StartNew(() => {
                        return num;
                    })
                );
            }

            for (int i = 0; i < count; ++i) {
                Assert.AreEqual(expected[i], tasks[i].Result);
            }
        }

        [TestMethod]
        public void AddJobTest()
        {
            Jobs.RepeatableJob job = jobManager.AddJob<Jobs.RepeatableJob>() as Jobs.RepeatableJob;

            Assert.IsNotNull(job);

            int expected = job.MaxCounter;
            
            job.Wait();

            Assert.AreEqual(expected, job.Counter);
        }

        [TestMethod]
        public void ExecuteAllJobsTest()
        {
            bool expected = true;

            var jobs = jobManager.ExecuteAllJobs();

            Assert.IsNotNull(jobs);
            Assert.AreEqual(jobs.Count, 2);

            foreach (var job in jobs) {
                job.Wait();
                Assert.AreEqual(expected, job.IsFinished);
            }
        }
    }
}
