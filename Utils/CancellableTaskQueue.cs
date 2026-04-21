using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GVisionWpf.Utils
{
    public class CancellableTaskQueue : IDisposable
    {
        private readonly object lockObject = new();

        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> taskTokens = new();

        private BlockingCollection<Func<Task>> taskQueue;
        private List<Task> workers;
        private CancellationTokenSource globalCts;

        private int runningTaskCount = 0;

        public int RunningTaskCount => runningTaskCount;


        public CancellableTaskQueue(int workerCount)
        {
            this.taskQueue = new BlockingCollection<Func<Task>>();
            this.globalCts = new CancellationTokenSource();
            this.workers = new List<Task>();

            createAndStartWorkers(workerCount);
        }

        private void createAndStartWorkers(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var task = Task.Factory.StartNew(() => workerLoop(i),
                    this.globalCts.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default).Unwrap();

                this.workers.Add(task);
            }
        }

        private async Task workerLoop(int workerId)
        {
            foreach (var task in this.taskQueue.GetConsumingEnumerable(this.globalCts.Token))
            {
                try
                {
                    await task();
                }
                catch (OperationCanceledException)
                {
                    // ignore - expected on cancel
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TaskQueue] Task error: {ex.Message}");
                }
            }
        }

        public Guid EnqueueCancelableTask(Func<CancellationToken, Task> task)
        {
            var id = Guid.NewGuid();

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(this.globalCts.Token);
            this.taskTokens[id] = linkedCts;

            taskQueue.Add(async () =>
            {
                try
                {
                    Interlocked.Increment(ref runningTaskCount);

                    await task(linkedCts.Token);
                }
                finally
                {
                    if (taskTokens.TryRemove(id, out var cts))
                        cts.Dispose();

                    Interlocked.Decrement(ref runningTaskCount);
                }
            });

            return id;
        }

        public void CancelTask(Guid taskId)
        {
            if (taskTokens.TryRemove(taskId, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }
        }

        public void Dispose()
        {
            lock (lockObject)
            {
                globalCts.Cancel();
                taskQueue.CompleteAdding();

                try
                {
                    Task.WaitAll(workers.ToArray());
                }
                catch (AggregateException) { }

                foreach (var token in taskTokens.Values)
                    token.Cancel();

                taskTokens.Clear();

                globalCts.Dispose();
                taskQueue.Dispose();

                workers.Clear();
            }
        }

        public void ResetWorkers(int workerCount = 1)
        {
            lock (lockObject)
            {
                Dispose();
                this.taskQueue = new BlockingCollection<Func<Task>>();
                this.globalCts = new CancellationTokenSource();
                this.workers = new List<Task>();

                createAndStartWorkers(workerCount);
            }
        }
    }
}
