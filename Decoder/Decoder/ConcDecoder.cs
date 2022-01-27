using System;
using System.Threading;
using Decoder;

namespace ConcDecoder
{
    /// <summary>
    /// A concurrent version of the class Buffer
    /// Note: For the final solution this class MUST be implemented.
    /// </summary>
    public class ConcurrentTaskBuffer : TaskBuffer
    {
        //todo: add required fields such that satisfies a thread safe shared buffer.
        
        object lockObject = new object();

        public ConcurrentTaskBuffer() : base()
        {
        }

        /// <summary>
        /// Adds the given task to the queue. The implementation must support concurrent accesses.
        /// </summary>
        /// <param name="task">A task to wait in the queue for the execution</param>
        public override void AddTask(TaskDecryption task)
        {
            lock (lockObject)
            {
                this.taskBuffer.Enqueue(task);
                this.maxBuffSize = this.taskBuffer.Count > this.maxBuffSize ? this.taskBuffer.Count  : this.maxBuffSize;
                this.numOfTasks++;
            }

            this.LogVisualisation();
            this.PrintBufferSize();
        }

        /// <summary>
        /// Picks the next task to be executed. The implementation must support concurrent accesses.
        /// </summary>
        /// <returns>Next task from the list to be executed. Null if there is no task.</returns>
        public override TaskDecryption GetNextTask()
        {
            TaskDecryption t = null;

            lock (lockObject)
            {
                if (this.taskBuffer.Count > 0)
                {
                    t = this.taskBuffer.Dequeue();
                    // check if the task is the last ending task: put the task back.
                    // It is an indication to terminate processors
                    if (t.id < 0)
                        this.taskBuffer.Enqueue(t);
                }
            }

            return t;
        }

        /// <summary>
        /// Prints the number of elements available in the buffer.
        /// </summary>
        public override void PrintBufferSize()
        {
            lock (lockObject)
            {
                Console.WriteLine("Buffer#{0}", this.taskBuffer.Count);
            }
        }
    }

    class ConcLaunch : Launch
    {
        public ConcLaunch() : base(){  }

        /// <summary>
        /// This method implements the concurrent version of the decryption of provided challenges.
        /// </summary>
        /// <param name="numOfProviders">Number of providers</param>
        /// <param name="numOfWorkers">Number of workers</param>
        /// <returns>Information logged during the execution.</returns>
        public string ConcurrentTaskExecution(int numOfProviders, int numOfWorkers)
        {
            ConcurrentTaskBuffer tasks = new ConcurrentTaskBuffer();

            for (int i = 0; i < numOfProviders; i++)
            {
                Thread t = new Thread(() => {
                    Provider provider = new Provider(tasks, this.challenges);
                    provider.SendTasks();
                });        

                t.Start();
            }

            for (int i = 0; i < numOfWorkers; i++)
            {
                Thread t = new Thread(() => {
                    Worker worker = new Worker(tasks);
                    worker.ExecuteTasks();
                });

                t.Start();
            }

            return tasks.GetLogs();
        }
    }
}
