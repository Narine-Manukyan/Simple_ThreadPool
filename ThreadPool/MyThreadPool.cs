using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace ThreadPool
{
    /// <summary>
    /// Delegete for working with threads.
    /// </summary>
    /// <param name="state">An object that contains data to be used by the delegate.</param>
    public delegate void WaitCallback(Object state);

    /// <summary>
    /// MyThreadPool implements a simple thread pool, that allows for dynamic change of number
    /// of working threads.
    /// </summary>
    public static class MyThreadPool
    {
        /// <summary>
        /// Maximum number of Threads in pool.
        /// </summary>
        public static int MaxThreads { set; get; } = 10;

        /// <summary>
        /// Minimum numebr of Threads in pool.
        /// </summary>
        public static int MinThreads { set; get; }

        /// <summary>
        /// Count od queued threads.
        /// </summary>
        public static int CountQueued = 0;

        /// <summary>
        /// Count of finished Threads.
        /// </summary>
        public static int finishedThreads = 0;

        /// <summary>
        /// Queue for WaitCallBack functions.
        /// </summary>
        public static ConcurrentQueue<WaitCallback> jobs = new ConcurrentQueue<WaitCallback>();

        /// <summary>
        /// List for executed Threads;
        /// </summary>
        public static List<Thread> threads = new List<Thread>();

        /// <summary>
        /// The Background Thread.
        /// </summary>
        public static Thread backgroundThread = new Thread(() =>
          {
              while (true)
              {
                  Dequeuer();
                  Thread.Sleep(1000);
              }
          }
        );

        /// <summary>
        /// Queues a method for execution. 
        /// The method executes when a thread pool thread becomes available.
        /// </summary>
        /// <param name="callback">A WaitCallback that represents the method to be executed.</param>
        /// <returns>true if the method is successfully queued; 
        /// NotSupportedException is thrown if the work item could not be queued.</returns>
        public static bool QueueUserWorkItem(WaitCallback callback)
        {
            if (callback == null)
                throw new NotSupportedException("The Callback method cannot be null");
            lock (jobs)
            {
                jobs.Enqueue(callback);
                CountQueued++;
            }
            lock (backgroundThread)
            {
                if(backgroundThread.ThreadState==ThreadState.Unstarted)
                {
                    backgroundThread.IsBackground = true;
                    backgroundThread.Start();
                }
            }
            return true;
        }

        /// <summary>
        /// For Dequeuing from ConcurrentQueue.
        /// </summary>
        public static void Dequeuer()
        {
            WaitCallback job = null;
            Thread stopped = null;
            lock (threads)
            {
                if (finishedThreads > 0)
                {
                    for (int i = 0; i < threads.Count; i++)
                    {
                        if (threads[i].ThreadState == ThreadState.Stopped)
                            // Get the reference to a stopped thread.
                            stopped = threads[i];
                    }
                }

                if (threads.Count > MaxThreads)
                    // Reassigning the masimum threads used.
                    MaxThreads = threads.Count;

                lock (jobs)
                {
                    // When there's still work, for dequeuing something.
                    if (jobs.Count > 0 && MaxThreads > threads.Count)
                        jobs.TryDequeue(out job);
                }
                // Release everything while preparing a new independant thread.
            }

            if (job == null && stopped != null)
            {
                // Dequeued nothing and there is a stopped thread.
                bool isEmpty = false;
                
                // Find the empty queue.
                lock (jobs)
                    isEmpty = (jobs.Count == 0);

                if (isEmpty)
                {
                    lock (threads)
                    {
                        if (!threads.Remove(stopped))
                            throw new SystemException("Couldn't remove a thread from list.");
                        else
                            Interlocked.Decrement(ref finishedThreads);
                    }
                }
            }

            else if (job != null)
            {
                Thread thread = new Thread(new ParameterizedThreadStart(ThreadFunc));
                thread.IsBackground = true;

                lock (threads)
                {
                    if (MaxThreads > threads.Count)
                    {
                        // Replacing the stopped thread with a new thread.
                        if (stopped != null)
                            threads[threads.IndexOf(stopped)] = thread;
                        else
                            threads.Add(thread);
                    }
                    else
                    {
                        // Letting other active threads do the work.
                        lock (jobs)
                        {
                            jobs.Enqueue(job);
                            job = null;
                        }
                    }
                }

                if (job != null)
                    thread.Start(job);
            }
        }

        /// <summary>
        /// Function for Dequeuing from pool.
        /// </summary>
        public static void ThreadFunc(Object obj)
        {
            // When the thread is started, it already has a job assigned to it.
            WaitCallback job = obj as WaitCallback;
            if (job == null)
                throw new ArgumentNullException("Must be recieved WaitCerallBack as a paramet");
            job.Invoke(null);
            // Dequeuing jobs from the queue.
            while (true)
            {
                job = null;
                lock (threads)
                    lock (jobs)
                        if (jobs.Count > 0)
                            jobs.TryDequeue(out job);
                if (job != null)
                    job.Invoke(null);
                // Couldn't dequeue from queue, terminate the current thread.
                else
                {
                    Interlocked.Increment(ref finishedThreads);
                    return;
                }
            }
        }
    }
}
