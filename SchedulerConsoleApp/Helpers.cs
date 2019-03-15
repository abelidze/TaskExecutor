using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using SchedulerTests;
using SchedulerManager.Executor;

namespace SchedulerBenchmark
{
    class Helpers
    {
        public static BigInteger FactorialRecursive(BigInteger n)
        {
            if (n <= 1)
                return 1;
            return n * FactorialRecursive(n - 1);
        }

        public static BigInteger FactorialIterative(BigInteger n)
        {
            if (n <= 1)
                return 1;
            while (n > 1) {
                n *= --n;
            }
            return n;
        }

        public static void QSort<T>(List<T> list, int first, int last, JobManager jm = null) where T : IComparable
        {
            var pivot = Partition(list, first, last);

            Task left = null;
            Task right = null;

            if (pivot - first > 1) {
                if (jm != null) {
                    jm.Execute(() => QSort<T>(list, first, pivot, jm));
                } else {
                    QSort<T>(list, first, pivot);
                }
            }

            if (last - pivot > 2) {
                if (jm != null) {
                    jm.Execute(() => QSort<T>(list, pivot + 1, last, jm));
                } else {
                    QSort<T>(list, pivot + 1, last);
                }
            }

            if (left != null) {
                left.Wait();
            }

            if (right != null) {
                right.Wait();
            }
        }

        private static int Partition<T>(List<T> list, int first, int last) where T : IComparable
        {
            var left = first;
            var right = last - 1;
            var pivot = first + (last - first) / 2;

            while (left != pivot || right != pivot) {
                while (list[left].CompareTo(list[pivot]) <= 0 && left < pivot) {
                    ++left;
                }

                while (list[right].CompareTo(list[pivot]) >= 0 && right > pivot) {
                    --right;
                }

                if (left == pivot) {
                    pivot = right;
                } else if (right == pivot) {
                    pivot = left;
                }

                var c = list[left];
                list[left] = list[right];
                list[right] = c;
            }
            return pivot;
        }

        public static void Tests()
        {
            //JobManager jobManager = new JobManager();
            //var factory = jobManager.GetFactory();
            //var task = factory.StartNew(() => {
            //    System.Console.WriteLine("Hello World");
            //});
            //System.Console.WriteLine(task.Result);

            //List<Task<int>> tasks = new List<Task<int>>();
            //for (int i = 0; i < 10; ++i) {
            //    int num = i;
            //    Task<int> task = new Task<int>(() => {
            //        System.Console.WriteLine("Hello World" + num.ToString());
            //        Thread.Sleep(3000);
            //        return num;
            //    });
            //    jobManager.AddTask(task);
            //    tasks.Add(task);
            //}
            //foreach (var task in tasks)
            //    System.Console.WriteLine(task.Result);
            //Thread.Sleep(2000);


            //var task = jobManager.ExecuteAction(() => {
            //    System.Console.WriteLine("HelloWorld");
            //    Thread.Sleep(3000);
            //});
            //task.Wait();


            //var tasks = jobManager.ExecuteAllJobs();
            //Task.WaitAll(tasks.ToArray());
            //Thread.Sleep(80000);
        }
    }
}
