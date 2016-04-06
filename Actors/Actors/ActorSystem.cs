using System;
using System.Threading.Tasks;

namespace Actors
{
    public static class ActorSystem
    {
        public static Func<Func<TOut, Task>, Task> Enter<TOut>(Func<Func<TOut, Task>, Task> parentGenerator) => consumer => parentGenerator(consumer);

        public static Func<Func<TOut, Task>, Task> Then<TIn, TOut>(this Func<Func<TIn, Task>, Task> parentGenerator, Func<Func<TOut, Task>, TIn, Task> itemHandler)
        {
            return async consumer =>
            {
                var actor = Actor.Make<TIn>(item => itemHandler(consumer, item));
                await parentGenerator(actor.Enqueue);
                await actor.Close();
                await actor.WaitForCompletion();
            };
        }

        public static Func<Func<TOut, Task>, Task> Aggregate<TIn, TOut>(this Func<Func<TIn, Task>, Task> parentGenerator, TOut initialValue, Func<TIn, TOut, TOut> itemHandler)
        {
            return async consumer =>
            {
                var acc = initialValue;
                var actor = Actor.Make<TIn>(async item =>
                {
                    acc = itemHandler(item, acc);
                    await Task.Yield();
                }, concurrency: 1);

                await parentGenerator(actor.Enqueue);
                await actor.Close();
                await actor.WaitForCompletion();
                await consumer(acc);
            };
        }

        public static Task Exit<TIn>(this Func<Func<TIn, Task>, Task> parentGenerator, Func<TIn, Task> itemHandler) => parentGenerator(itemHandler);
    }
}
