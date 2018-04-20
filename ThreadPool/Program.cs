using System;
using System.Threading.Tasks;
using System.Threading;

namespace ThreadPool
{
    class Program
    {
        public static void Main(String[] args)
        {
            // Count of tests.
            const int count = 10;

            // Per event is used for each Fibonacci object.
            ManualResetEvent[] doneEvents = new ManualResetEvent[count];
            Fibonacci[] array = new Fibonacci[count];

            // Random number generator.
            Random rand = new Random();

            // Work with threads with MyThreadPool.
            Console.WriteLine("Starting {0} tasks.", count);

            for (int i = 0; i < count; i++)
            {
                doneEvents[i] = new ManualResetEvent(false);
                array[i] = new Fibonacci(rand.Next(25, 40), doneEvents[i]);
                MyThreadPool.QueueUserWorkItem(array[i].ThreadPoolCallback);
            }

            Console.WriteLine("Main thread does some work than sleep.");
            Thread.Sleep(7000);

            //Wait for all threads in pool to calculation.
            WaitHandle.WaitAll(doneEvents);
            Console.WriteLine("All calculations are complete.");

            // Results.
            for (int i = 0; i < count; i++)
            {
                Console.WriteLine("Fibonacci[{0}] = {1}", array[i].Number, array[i].Result);
            }
        }
    }
}
