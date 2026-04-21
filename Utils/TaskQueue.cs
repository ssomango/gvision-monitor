using GVisionWpf.UIs.UiUpdaters;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GVisionWpf.Utils
{
    public class TaskQueue : IDisposable
    {
        public CancellationTokenSource cts;
        private List<Task> workers;

        private BlockingCollection<Func<Task>> taskQueue;

        public TaskQueue(int nWorkers)
        {
            this.taskQueue = new BlockingCollection<Func<Task>>();
            this.workers = new List<Task>();
            this.cts = new CancellationTokenSource();

            CreateAndStartWorkers(nWorkers);
        }

        public void CreateAndStartWorkers(int nWorkers)
        {
            workers = new List<Task>();

            for (int i = 0; i < nWorkers; i++)
            {
                this.workers.Add(Task.Factory.StartNew(() => worker(i), this.cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap());
            }
        }

        public void Dispose()
        {
            this.cts.Cancel();
            this.taskQueue.CompleteAdding();
          
            try
            {
                Task.WaitAll(this.workers.ToArray());
            }
            catch(AggregateException) 
            {

            }

            this.taskQueue.Dispose();
            this.cts.Dispose();

            this.workers.Clear();
        }

        public Task EnqueueTask(Func<CancellationToken, Task> task)
        {
            var tcs = new TaskCompletionSource();

            if (cts.IsCancellationRequested || taskQueue.IsAddingCompleted)
            {
                tcs.SetCanceled();
                return tcs.Task;
            }

            taskQueue.Add(async () =>
            {
                try
                {
                    await task(cts.Token);
                    tcs.TrySetResult();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }

        private async Task worker(int id)
        {
            foreach (Func<Task> task in this.taskQueue.GetConsumingEnumerable(this.cts.Token))
            {
                try
                {
                    await task();
                }
                catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    GlobalErrorHandler.HandleException(ex);
                }
            }
        }
    }
}