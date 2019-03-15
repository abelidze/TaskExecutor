using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Numerics;
using System.Threading.Tasks;
using SchedulerTests;
using SchedulerManager.Executor;

namespace SchedulerBenchmark
{
    class Benchmarker
    {
        private static JobManager jobManager = new JobManager();
        private static BigInteger minFactRecursive = 1000;
        private static BigInteger maxFactRecursive = 4000;
        private static BigInteger minFactIterative = 10000;
        private static BigInteger maxFactIterative = 50000;
        private const int sortSize = 10000000;

        //[Benchmark]
        //public static void NoThreadFactorialRecursive()
        //{
        //    for (BigInteger i = minFactRecursive; i < maxFactRecursive; ++i) {
        //        Helpers.FactorialRecursive(i);
        //    }
        //}

        //[Benchmark]
        //public static void ThreadFactorialRecursive()
        //{
        //    var list = new List<Task>();
        //    for (BigInteger i = minFactRecursive; i < maxFactRecursive; ++i) {
        //        list.Add(
        //          jobManager.Execute(() => Helpers.FactorialRecursive(i))
        //        );
        //    }
        //    Task.WaitAll(list.ToArray());
        //}

        [Benchmark]
        public static void NoThreadFactorialIterative()
        {
            for (BigInteger i = minFactRecursive; i < maxFactRecursive; ++i) {
                Helpers.FactorialIterative(i);
            }
        }

        [Benchmark]
        public static void ThreadFactorialIterative()
        {
            var list = new List<Task>();
            for (BigInteger i = minFactRecursive; i < maxFactRecursive; ++i) {
                list.Add(
                  jobManager.Execute(() => Helpers.FactorialIterative(i))
                );
            }
            Task.WaitAll(list.ToArray());
        }

        [Benchmark]
        public static void NoThreadQSort()
        {
            var list = new List<int>();
            var generator = new Random();
            for (int i = 0; i < sortSize; ++i) {
                list.Add(generator.Next(sortSize));
            }
            jobManager.Execute(() => Helpers.QSort(list, 0, list.Count)).Wait();
        }

        [Benchmark]
        public static void ThreadQSort()
        {
            var list = new List<int>();
            var generator = new Random();
            for (int i = 0; i < sortSize; ++i) {
                list.Add(generator.Next(sortSize));
            }
            jobManager.Execute(() => Helpers.QSort(list, 0, list.Count, jobManager)).Wait();
        }
    }
}
