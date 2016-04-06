using System;
using System.Threading.Tasks;

namespace Actors
{
    class Program
    {
        static void Main(string[] args)
        {
            var task = ActorSystem
                .Enter<int>(async cb =>
                {
                    for (int i = 0; i <= 5000; i++)
                    {
                        await cb(i);
                    }
                })
                .Then<int, int>((cb, item) => cb(item * item))
                .Aggregate(0, (acc, i) => acc ^ i)
                .Then<int, int>(async (cb, item) =>
                {
                    for (int i = 0; i < item; i++)
                    {
                        await cb(i);
                    }
                })
                .Exit(async item =>
                {
                    if (item % 1000000 == 0)
                        Console.WriteLine(item);

                    await Task.FromResult(0);
                });

            Task.WaitAll(task);
        }
    }
}
