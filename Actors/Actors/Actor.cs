using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Actors
{
    public static class Actor
    {
        static readonly int cpuCount = Environment.ProcessorCount;

        public static Actor<TItem> Make<TItem>(Func<TItem, Task> itemHandler, int concurrency = 0, int maxQueueLength = 100)
        {
            if (concurrency == 0)
            {
                concurrency = cpuCount;
            }

            return new Actor<TItem>(itemHandler, concurrency, maxQueueLength);
        }
    }

    public class Actor<TItem> : IClosable
    {
        private ActorState state;
        private readonly Func<TItem, Task> itemHandler;
        private readonly int maxQueueLength;
        private readonly ConcurrentQueue<TItem> queue;
        private readonly Task[] tasks;

        public Actor(Func<TItem, Task> itemHandler, int concurrency, int maxQueueLength)
        {
            this.maxQueueLength = maxQueueLength;
            this.queue = new ConcurrentQueue<TItem>();
            this.tasks = Enumerable.Range(0, concurrency)
                .Select(i => Task.Run(WorkerLoop))
                .ToArray();
            this.itemHandler = itemHandler;
            this.state = ActorState.Running;
        }

        public async Task Close()
        {
            switch (state)
            {
                case ActorState.Running:
                    state = ActorState.Closing;
                    await Task.WhenAll(tasks);
                    state = ActorState.Closed;
                    return;

                case ActorState.Closing:
                case ActorState.Closed:
                    return;

                default:
                    throw new Exception("Invalid state");
            }
        }

        public async Task WaitForCompletion()
        {
            while (state != ActorState.Closed)
            {
                await Task.Yield();
            }
        }

        public async Task Enqueue(TItem item)
        {
            if (state != ActorState.Running)
            {
                throw new Exception($"Can not enqueue item, actor is in {state} state.");
            }

            while (queue.Count > maxQueueLength)
            {
                await Task.Yield();
            }

            queue.Enqueue(item);
        }

        private async Task WorkerLoop()
        {
            while (state == ActorState.Running)
            {
                TItem item;
                while (queue.TryDequeue(out item))
                {
                    await itemHandler(item);
                }

                await Task.Yield();
            }
        }
    }

    public enum ActorState
    {
        Running = 0,
        Closing,
        Closed,
    }
}
