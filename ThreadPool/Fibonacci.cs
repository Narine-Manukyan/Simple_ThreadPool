using System;
using System.Threading;

namespace ThreadPool
{
    /// <summary>
    /// Class that represents the Fibonacci number and calculation.
    /// </summary>
    public class Fibonacci
    {
        // Fibonacci Number.
        public long Number { private set; get; }
        private ManualResetEvent myEvent;
        // Result of Fibonacci calculating.
        public long Result { private set; get; }

        // Parameterfull constructor.
        public Fibonacci(long number,ManualResetEvent e)
        {
            this.Number = number;
            this.myEvent = e;
        }

        /// <summary>
        /// Recursive method that calculates the Nth Fibonacci number.
        /// </summary>
        public long CalculateFibonacci(long number)
        {
            if (number <= 1)
                return number;
            return CalculateFibonacci(number - 1) + CalculateFibonacci(number - 2);
        }

        /// <summary>
        /// Wrapper method for use with thread pool.
        /// </summary>
        public void ThreadPoolCallback(Object threadContext)
        {
            Console.WriteLine("Thread started.");
            this.Result = CalculateFibonacci(this.Number);
            Console.WriteLine("Thread result calculated.");
            myEvent.Set();
        }
    }
}
